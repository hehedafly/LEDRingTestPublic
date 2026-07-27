using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO.MemoryMappedFiles;
using UnityEngine;

// using System.Diagnostics;

namespace SharedMMF
{
/*use example

sharedmm = new Sharedmm("unity", "");
try{
    sharedmm.Init("UnityShareMemoryTest", 32+5*16*1024);
}
catch (Exception e){
    sharedmm = null;
    Debug.Log(e.Message);
    Quit();
}

if(sharedmm.CheckServerOnlineStatus()){
    string tempStr = $"From Unity-- Now Time:{Time.time}";
    if(sharedmm != null){
        sharedmm.WriteContent(tempStr, true);
        foreach(string msg in sharedmm.ReadMsg(0, "all")){
            if(msg.Split(";").Length == 4){
                List<int> pos =  new List<int>((from num in msg.Split(';') select int.Parse(num)).ToList());
                square.transform.position = new Vector3((pos[0] +pos[2])*0.05f, square.transform.position.y, (pos[1] +pos[3])*0.05f);
            }
        }
    }
}else{
    Quit();
}
*/


/**********************************************************************************************
    initialization: 0..HEADER_SIZE-1 0x00, HEADER_SIZE.. 0xFF   (HEADER_SIZE=128, 布局见下方常量处注释)
    name = server/<custom>
    index = 0(server) or 1-4
    care = certain name("UnityProject")/"", only one allowed in this version, if "": ignore all write/read Mark, else update marks base on cared one
    careindex = index/-1, if cared one online and applied, update to index of cared one, if multiple have same name, take the frist one applied, until its offline, then wait for another apply.

    0:              server Online status(0/-255)                                           $write by Server
    1:              max client number(4 max)                                            $write by Server
    2:              now client number(0-4)                                              $write by client, read then check online status
    3-6:            clients online status(0-255)                                          $write by client, check from 0 to 3, frist zero value as client's own index
    7:              client index applied                                                $write by client, check if careindex == -1
    8:              name length of client applied                                       $write by client
    9 .. HEADER_SIZE-4   name of client applied                                          $write by client
    HEADER_SIZE-3        header size byte(=HEADER_SIZE), for cross-end version check       $write by Server
    HEADER_SIZE-2        protocol version                                                  $write by Server
    HEADER_SIZE-1        xor check(size ^ version ^ MAGIC)                                 $write by Server

    HEADER_SIZE - HEADER_SIZE+16*1024:Server write buffer
    .. - ..+16:1024:client0 write buffer
    ......

    In every write buffer:
    0:              writing/finish(0/1)                                                 $write by self
    (1-2)*4:        read mark for others(0-4096<0x0F,0x00>), i                          $write by others, if larger than write mark, back to zero
    9-10:           written mark(0-4096),                                               $write by self, if equal to cared one's read mark, back to zero, else if careindex = -1, back to zero when everyone read or ran out of write buffer.
                                                                                                        if ran out of write buffer, back to (writemark - max(readmark)/cared one readmark)< 16 ? ~ : 0
    11-12           newest message start index                                          $write by self
    13-14           newest message end index                                            $write by self
    15..            messages                                                            $write by self

    In every message:
    0-1:            length(0<0x00, 0x00>-15360<0xE0, 0x00>)
    2-..:           content
    between every message: 0xFF, 0xFF

**********************************************************************************************/
    public class Sharedmm
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);
 
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr CreateFileMapping(int hFile, IntPtr lpAttributes, uint flProtect, uint dwMaxSizeHi, uint dwMaxSizeLow, string lpName);
 
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr OpenFileMapping(int dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, string lpName);
 
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr MapViewOfFile(IntPtr hFileMapping, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);
 
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool UnmapViewOfFile(IntPtr pvBaseAddress);
 
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool CloseHandle(IntPtr handle);
 
        [DllImport("kernel32", EntryPoint = "GetLastError")]
        public static extern int GetLastError();
 
        const int ERROR_ALREADY_EXISTS = 183;
        const int FILE_MAP_ALL_ACCESS = 0x001F01FF;
 
        const int INVALID_HANDLE_VALUE = -1;

        // ---- 协议元数据（头部大小/版本/异或校验）：C# 与 Python 两端必须完全一致，改动需同步重新部署 ----
        public const int HEADER_SIZE = 128;                     // 头部元数据字节数（原为32），其后写缓冲区顺延
        public const int MAX_CLIENT_COUNT = 4;                       // 头部魔数
        public const int TOTAL_SIZE = HEADER_SIZE + (MAX_CLIENT_COUNT + 1) * 16 * 1024;
        const byte PROTOCOL_VERSION = 2;                       // 协议版本，破坏性改动时+1
        const byte VERSION_MAGIC = 0xA5;                       // 异或校验魔数
        // ---- 头部字节布局(偏移固定进规则, 不单独变量化) ----
        // 0:server在线 1:maxClientNum 2:当前client数 3-14:client在线状态(12B,保留冗余)
        // 15:头部大小(=HEADER_SIZE) 16:协议版本 17:异或校验(buf15^buf16^MAGIC)
        // 18..HEADER_SIZE-1:名称登记区, 每槽20B=[uid:1][len:1][name:16(0x00填充)][0xFF][0xFF], 先到先得; server不登记
 
        IntPtr m_hSharedMemoryFile = IntPtr.Zero;
        IntPtr m_pwData = IntPtr.Zero;
        // bool m_bAlreadyExist = false;
        /// <summary>
        /// m_hSharedMemoryFile, m_hSharedMemoryFile = CreateFileMapping != null;  CloseHandle(m_hSharedMemoryFile);
        /// </summary>
        bool shmCreated = false;
        /// <summary>
        /// m_pwData, m_pwData = MapViewOfFile != IntPtr.Zero;  UnmapViewOfFile(m_pwData);
        /// </summary> <summary>
        /// 
        /// </summary>
        bool shmInitiled = false;
        long m_MemSize = TOTAL_SIZE;

        string shareMemoryName;
        string userName;
        long lngSize;
        string care;
        int UID;
        int careIndex;//write时关注对应用户读取自己内容情况
        int mySlotOffset = -1;//本实例占用的名称槽偏移(18..), 退出时清理
        // int contentBegin;
        unsafe byte* ShmBuffer;
        int maxClientNum;

        /// <summary>
        /// +0: writting tof; +1 + projected_id*2: read mark; +9:writtenmark; +11/13: newest starrt/end number
        /// </summary>
        int writeBufferStartPos;
        int writeBufferLength;
        List<int> writeBufferStartPosAll;
        bool closed = false;

        byte[] splitCondon = new byte[]{0xFF, 0xFF};
        int writtenmark = -1;
        /// <summary>
        /// number from own writebuffer start to new msg start
        /// </summary>
        int newestStartPos = -1;
        /// <summary>
        /// mubner from own writebuffer start to new msg end(include splitCondon)
        /// </summary>
        int newestEndPos = -1;
        List<int> messageStartPosLs = new List<int>();
        List<int> messageLengthLs = new List<int>();

        /// <summary>
        /// update every 1s
        /// </summary>
        bool heartbeat = false;
        int maxOfflineTick = 5;
        int offlineTick = 0;
        // int careOfflineTick = 0;
        List<int> careOnlineStatus = new List<int>{};//for client: index: 0-server, 1-care; value: updateTick.   for server:index: 0~maxclientnum-clients; value: same
        List<int> clientOfflineTick = new List<int>{};//only for server:index: 0~maxclientnum-clients; value: tick
 
        /// <summary>
        /// name: "server" or other client name, not the name of the shared memory(claim in Init func)
        /// maxClientNum: 4 in default
        /// </summary>
        /// <param name="_shareMemoryName"></param>
        /// <param name="_care"></param> <summary>
        /// 
        /// </summary>
        /// <param name="_shareMemoryName"></param>
        /// <param name="_care"></param>
        public Sharedmm(string _shareMemoryName, string _care, long _lngSize = -1, bool _heartbeat = false)
        {
            if (_lngSize <= 0 || _lngSize > TOTAL_SIZE){_lngSize = TOTAL_SIZE;}

            shareMemoryName = _shareMemoryName;
            lngSize = _lngSize;
            care = _care;
            UID = -1;
            careIndex = -1;
            // contentBegin = 0;
            writeBufferLength = 16 * 1024;
            writeBufferStartPosAll = new List<int>();
            heartbeat = _heartbeat;
        }

        // 注意：已删除 ~Sharedmm() 析构函数。
        // 原析构函数在 GC finalizer 线程调用 CloseSharedmm → UnmapViewOfFile，会与主线程 FixedUpdate
        // 对同一虚拟地址的并发访问争用（finalizer 线程 unmap 复用地址的新实例视图 → access violation）。
        // 原生资源改由 CloseSharedmm(manually:true) + IPCClient.OnDestroy 显式管理；
        // 进程退出时 OS 自动回收 file-mapping，可接受。Init 的 throw 路径也已 closed=true 防脏实例。

        /// <summary>
        /// init, throw exception if failed to create
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        unsafe public int Init(string userName)
        {
            this.userName = userName;

            // m_MemSize = lngSize;
            if (userName.Length > 0){
                if (userName == "server"){
                    m_hSharedMemoryFile = CreateFileMapping(INVALID_HANDLE_VALUE, IntPtr.Zero, 0x04, 0, (uint)lngSize, shareMemoryName);
                }else{
                    m_hSharedMemoryFile = CreateFileMapping(INVALID_HANDLE_VALUE, IntPtr.Zero, 0x04, 0, (uint)lngSize, shareMemoryName);
                    // m_hSharedMemoryFile = OpenFileMapping(FILE_MAP_ALL_ACCESS, true, strName);
                }

                // if (m_hSharedMemoryFile == IntPtr.Zero)
                if (m_hSharedMemoryFile == IntPtr.Zero)
                {
                    // m_bAlreadyExist = false;
                    int errorCode = GetLastError();
                    // shmInitiled = false;
                    throw new Exception("failed to create and map");
                }
                else
                {   
                    shmCreated = true;
                    int errorCode = GetLastError();
                    if (errorCode == ERROR_ALREADY_EXISTS)  //已经创建
                    {
                        if(userName == "server"){
                            throw new Exception("To map to a existing shm, name shouldn't be \"server\"");
                        }
                        // m_bAlreadyExist = true;
                    }
                    else                                   //创建成功
                    {
                        // if(name != "server"){
                        //     CloseHandle(m_hSharedMemoryFile);
                        //     throw new Exception("To create a new shm, name should be \"server\"");
                        // }
                        // m_bAlreadyExist = false;
                    }
                }
                //---------------------------------------
                //创建内存映射
                m_pwData = MapViewOfFile(m_hSharedMemoryFile, 0x0002, 0, 0, (uint)lngSize);
                if (m_pwData == IntPtr.Zero)
                {
                    // shmInitiled = false;
                    CloseHandle(m_hSharedMemoryFile);
                    shmCreated = false;

                    throw new Exception("failed to map to" + m_hSharedMemoryFile.ToString());
                }
                else
                {
                    shmInitiled = true;
                    // if (m_bAlreadyExist == true){
                    // int size = Marshal.SizeOf(typeof(byte));
                    ShmBuffer = (byte*)m_pwData.ToPointer();

                    if(Encoding.UTF8.GetByteCount(userName) > 16){
                        ReleaseNative();
                        closed = true;
                        throw new Exception($"user name too long (>16 bytes): {userName}");
                    }

                    if(userName != "server"){
                        List<byte> nowServerStatus = ReadShmHead().ToList();
                        maxClientNum = nowServerStatus[1];
                        if(nowServerStatus[2] >= maxClientNum){
                            ReleaseNative();
                            closed = true;                 // 让该实例即使被遗弃也不会被 finalizer 误处理（防脏实例）

                            if(maxClientNum == 0){throw new Exception("server offline");}
                            else{throw new Exception($"already {nowServerStatus[2]} clients on server");}
                        }else{
                            // 仅在确认服务端在线（maxClientNum>0，头部已初始化）后再校验协议版本；
                            // 否则服务端离线时 Unity 会 CreateFileMapping 新建全零映射，全零版本字节会被误报为版本/大小不匹配
                            if(!ValidateVersionInfo(out string versionReason)){
                                ReleaseNative();
                                closed = true;             // 防脏实例
                                throw new Exception($"protocol/version mismatch: {versionReason}");
                            }
                            UID = nowServerStatus.FindIndex(3, x => x == 0) - 2;
                            if(UID < 0 || UID > maxClientNum){
                                ReleaseNative();
                                closed = true;             // 防脏实例

                                throw new Exception("wrong status record, failed to get index");
                            }
                            WriteByte(2, (byte)(nowServerStatus[2] % 255 +1));
                            WriteByte(3 + UID - 1, 1);
                            RegisterSelf();
                            
                            Debug.Log($"UID: {UID}");
                        }

                        for (int i = HEADER_SIZE; i < HEADER_SIZE + (maxClientNum + 1) * writeBufferLength; i += writeBufferLength)
                        {
                            writeBufferStartPosAll.Add(i);
                        }
                        
                        ResolveCare();                     // care=""→-1; "server"→0; 其它→扫名称区匹配
                        careOnlineStatus.Add(nowServerStatus[0]);
                        careOnlineStatus.Add(-1);

                        writeBufferStartPos = writeBufferStartPosAll[UID];
                        newestStartPos = 15;
                        newestEndPos = newestStartPos;
                        WriteBytes(writeBufferStartPos, 0x00, 15);
                        writtenmark = 0;
                    }else{
                        UID = 0;
                        maxClientNum = 4;
                        // 容量检查: maxClientNum 个 client 各需一个名称槽(20B, 从18起), server 不占槽; 资源须小于 header
                        if(18 + maxClientNum * 20 > HEADER_SIZE){
                            ReleaseNative();
                            closed = true;
                            throw new Exception($"maxClientNum {maxClientNum} exceeds header capacity (HEADER_SIZE={HEADER_SIZE})");
                        }
                        writeBufferStartPosAll.Clear();
                        for (int i = HEADER_SIZE; i < HEADER_SIZE + (maxClientNum + 1) * writeBufferLength; i += writeBufferLength)
                        {
                            writeBufferStartPosAll.Add(i);
                        }
                        WriteBytes(0, 0x00, HEADER_SIZE);
                        WriteBytes(HEADER_SIZE, 0xFF, (maxClientNum + 1) * writeBufferLength);
                        WriteByte(0, 1);
                        WriteByte(1, (byte)maxClientNum);
                        // 在线区(3-14)与名称区(18..)已被上面的整头清零覆盖; 版本区(15-17)随后由 WriteVersionInfo 写入
                        writeBufferStartPos = writeBufferStartPosAll[UID];
                        WriteBytes(writeBufferStartPos, 0x00, 15);
                        writtenmark = 0;
                        newestStartPos = 15;
                        newestEndPos = 15;
                        careOnlineStatus.AddRange(Enumerable.Repeat(-1, maxClientNum));
                        clientOfflineTick.AddRange(Enumerable.Repeat(-1, maxClientNum));
                        WriteVersionInfo();                 // server 写入协议元数据（末尾3字节）
                    }
                    // }
                }
                //----------------------------------------
            }
            else
            {
                return 1; //参数错误    
            }
 
            return 0;     //创建成功
        }

        bool IsValidHandle(IntPtr handle) {
            // return handle == IntPtr.Zero || handle == new IntPtr(-1);
            return handle != IntPtr.Zero && handle != new IntPtr(-1);
        }

        /// <summary>
        /// 释放原生资源并完整复位状态。Init 的 throw 路径与 CloseSharedmm 共用此方法，
        /// 避免出现“已 unmap 但 shmInitiled 仍为 true / m_pwData 未清零 / ShmBuffer 悬挂”
        /// 的脏实例（脏实例被 GC finalizer 回收时会误 unmap 复用同一虚拟地址的新实例视图，
        /// 是本次偶发崩溃的根因）。
        /// 注意：调用方负责在之后将 closed 置 true。
        /// </summary>
        unsafe void ReleaseNative(){
            if(IsValidHandle(m_pwData)){
                UnmapViewOfFile(m_pwData);
            }
            m_pwData = IntPtr.Zero;
            if(IsValidHandle(m_hSharedMemoryFile)){
                CloseHandle(m_hSharedMemoryFile);
            }
            m_hSharedMemoryFile = IntPtr.Zero;
            ShmBuffer = null;          // 关键：清掉悬挂指针（原先从不清，崩溃点 WriteByte:399 的根因）
            shmInitiled = false;
            shmCreated = false;
        }

        /// <summary>
        /// 是否已关闭（含半关闭/异常关闭后）。供外部调用方判断实例是否仍可用。
        /// </summary>
        public bool IsClosed { get { return closed; } }

        /// <summary>
        /// 是否已初始化且未关闭，可安全读写。
        /// </summary>
        public bool IsReady { get { return shmInitiled && !closed; } }

        /// <summary>
        /// 关闭共享内存
        /// </summary>
        public void CloseSharedmm(bool manually = false)
        {
            if(closed) return;                 // 幂等：已关闭直接返回
            try{
                if(shmInitiled){
                    if(manually && IsValidHandle(m_pwData) && IsValidHandle(m_hSharedMemoryFile)){
                        if(userName == "server"){
                            WriteByte(0, 0);
                        }else{
                            // WriteByte(2, (byte)(nowServerStatus[2]-1));
                            WriteByte(2 + UID, 0);
                            if(mySlotOffset >= 0){ WriteBytes(mySlotOffset, 0x00, 20); }   // 清自己的名称槽
                        }
                    }
                    ReleaseNative();           // 统一释放 + 复位
                }
                else if (shmCreated){
                    ReleaseNative();
                }
            }
            catch(Exception e){
                Debug.LogError(e.Message);
            }
            finally{
                closed = true;                 // 关键：无论是否异常都置位，杜绝“半关闭脏实例”
            }
        }

        public int BytesToInts(byte[] bytes){
            if (bytes.Length != 2){
                throw new ArgumentException("");
            }
            return bytes[0] * 256 + bytes[1];
        }

        public byte[] IntToBytes(int i){
            if(i < 0 || i > 65535){return splitCondon;}

            return new byte[]{(byte)(i / 256), (byte)(i % 256)};
        }
        
        /// <summary>
        /// return 0~4(max player number)
        /// </summary>
        /// <param name="_id"></param>
        /// <returns></returns>
        int GetProjectedWritePos(int _id){
            return UID > _id? UID-1 : UID;
        }

        unsafe int WriteByte(int _ind, byte _val){
            if(!shmInitiled || ShmBuffer == null) return -1;   // 守卫：已关闭/未初始化则安全返回
            ShmBuffer[_ind] = _val;
            return 1;
        }

        unsafe int WriteBytes(int _pos, byte _byte, int _length){
            if(!shmInitiled || m_pwData == IntPtr.Zero) return -1;   // 守卫
            byte[] bytes;
            if(_byte == 0x00){
                bytes = new byte[_length];
            }else{
                bytes = new byte[_length];
                Array.Fill(bytes, _byte);
            }

            Marshal.Copy(bytes, 0, m_pwData+_pos, _length);
            return 1;
        }

        /// <summary>
        /// _ind: start address to write
        /// </summary>
        /// <param name="_ind"></param>
        /// <param name="bytes"></param>
        /// <returns></returns> <summary>
        /// 
        /// </summary>
        /// <param name="_ind"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        unsafe int WriteBytes(int _ind, byte[] bytes){
            if(!shmInitiled || m_pwData == IntPtr.Zero) return -1;   // 守卫
            Marshal.Copy(bytes, 0, m_pwData+_ind, bytes.Length);
            return 1;
        }

        void WriteWritingStatus(int _ind, bool _isWriting){
            WriteByte(_ind, (byte)(_isWriting? 0: 1));
        }
 
        /// <summary>
        /// 写数据
        /// </summary>
        /// <param name="bytData">数据</param>
        /// <param name="lngAddr">起始地址</param>
        /// <param name="lngSize">个数</param>
        /// <returns></returns>
        public int WriteContent(string message, bool clear = false)
        {
            return WriteContent(Encoding.UTF8.GetBytes(message), clear);
        }
        
        int WriteContent(byte[] message, bool clear = false)
        {
            if (UID == -1)
            {
                throw new InvalidOperationException("Index not set.");
            }
            int[] status = ReadWriteBufferHead(UID);

            int clearPos = -1;
            if (clear){
                WriteClear();
            }else if(careIndex != -1){
                int readMark = status[1+GetProjectedWritePos(careIndex)];
                if(readMark >= writtenmark){
                    clearPos = writtenmark - 1;
                    if(readMark - writtenmark >= 20){//起码20个消息后再看care id是否读完
                        WriteClear();
                        int writePos = writeBufferStartPosAll[UID] + 1 + GetProjectedWritePos(careIndex)*2;
                        WriteBytes(writePos, IntToBytes(0));
                    }
                }
            }

            int contentLen = message.Length;
            int startPos = writeBufferStartPos + newestEndPos;  // P0 #4: use newestEndPos as current write position
            if(newestEndPos + contentLen + 2 + 2 >= writeBufferLength){
                //后续再加对careindex的判断
                if(careIndex != -1){
                    clearPos = status[1 + GetProjectedWritePos(careIndex)];
                }
                WriteClear(clearPos);
                startPos = writeBufferStartPos + 15;
                newestStartPos = 15;
                newestEndPos = 15;
            }

            WriteWritingStatus(writeBufferStartPos, true);

            // P0 #4: align with Python semantics:
            //   newestStartPos = current message start (relative to writeBufferStartPos)
            //   newestEndPos = current message end including 0xFF 0xFF separator
            newestStartPos = newestEndPos;  // current message starts where last one ended
            newestEndPos = newestStartPos + contentLen + 2 + splitCondon.Length;  // +2 length prefix, +2 separator

            // P0 #5: record current message start BEFORE updating positions
            messageStartPosLs.Add(newestStartPos);
            messageLengthLs.Add(contentLen);  // P0 #7: store content length (not prefixed length)

            // write [length(2B)][content][0xFF 0xFF]
            byte[] fullMsg = IntToBytes(contentLen).Concat(message).Concat(splitCondon).ToArray();
            WriteBytes(startPos, fullMsg);

            writtenmark += 1;
            WriteWriteMark(writtenmark);
            WriteNewStartAndEndPos(newestStartPos, newestEndPos);

            WriteWritingStatus(writeBufferStartPos, false);

            return clearPos;
        }
        
        int WriteClear(int clearPos = -1){
            if (writtenmark == 0){return 0;}
            if (clearPos <= 0 || clearPos > writtenmark){clearPos = writtenmark;}
            
            byte[] storedMsg = new byte[newestEndPos - messageStartPosLs[clearPos - 1]];
            ReadByte(ref storedMsg, writeBufferStartPos + messageStartPosLs[clearPos - 1], newestEndPos - messageStartPosLs[clearPos - 1]);

            messageStartPosLs.RemoveRange(0, clearPos);
            messageLengthLs.RemoveRange(0, clearPos);
            // P0 #7: reset to 15 when empty; correctly compute endPos with separator
            if (messageStartPosLs.Count > 0){
                // remap positions: messages are moved to start at offset 15
                int shift = 15 - messageStartPosLs[0];
                for (int i = 0; i < messageStartPosLs.Count; i++){
                    messageStartPosLs[i] += shift;
                }
                newestStartPos = messageStartPosLs[messageStartPosLs.Count - 1];
                newestEndPos = newestStartPos + messageLengthLs[messageLengthLs.Count - 1] + 2 + 2;  // +2 length, +2 separator
            }else{
                newestStartPos = 15;  // P0 #7: was 0, should be 15
                newestEndPos = 15;    // P0 #7: was 0, should be 15
            }
            writtenmark = writtenmark - clearPos;

            byte[] bytes = new byte[writeBufferLength - 15];
            Array.Fill(bytes, (byte)0xFF);
            WriteBytes(writeBufferStartPos + 15, bytes);

            if (messageStartPosLs.Count > 0){
                WriteBytes(writeBufferStartPos + 15, storedMsg);
            }
            // update header after clear
            WriteWriteMark(writtenmark);
            WriteNewStartAndEndPos(newestStartPos, newestEndPos);
            // Bug3 fix: reset all readers' readMark so they don't miss new messages after clear
            WriteBytes(writeBufferStartPos + 1, new byte[8]);  // 4 slots * 2 bytes
            return 1;
        }

        int WriteWriteMark(int writemark){
            int writePos = writeBufferStartPos + 9;
            // ShmBuffer[writePos] = (byte)writemark;
            WriteBytes(writePos, IntToBytes(writemark));
            // Debug.Log($"Write mark: {writemark}, writePos: {writePos}, self.id: {UID}");  // P2 #18: disabled for high-frequency writes

            return 1;
        }

        int WriteNewStartAndEndPos(int start, int end){
            int writePos = writeBufferStartPos + 11;
            byte[] tempPos = IntToBytes(start).Concat(IntToBytes(end)).ToArray();
            // ShmBuffer[writePos] = (byte)writemark;
            WriteBytes(writePos, tempPos);
            return 1;
        }

        int WriteReadMark(int _id, int readmark){
            int writeInd = GetProjectedWritePos(_id);
            int writePos = writeBufferStartPosAll[_id] + 1 + writeInd*2;

            WriteBytes(writePos, IntToBytes(readmark));
            // Debug.Log($"writemark written at {_id}: {readmark}");
            // Array.Copy(IntToBytes(readmark), 0, ShmBuffer, writePos, 2);
            return 1;
        }

        int ReadInt(int lngAddr){
            if (lngAddr + 1 > m_MemSize) return -2; //超出数据区
            if (shmInitiled)
            {   
                byte[] bytData = new byte[2];
                Marshal.Copy(m_pwData+lngAddr, bytData, 0, 2);
                return BytesToInts(bytData);
            }
            else
            {
                return -1; //共享内存未初始化
            }
        }
        
        int ReadByte(int lngAddr){
            if (lngAddr > m_MemSize) return -2; //超出数据区
            if (shmInitiled)
            {   
                byte[] bytData = new byte[1];
                Marshal.Copy(m_pwData+lngAddr, bytData, 0, 1);
                return bytData[0];
            }
            else
            {
                return -1; //共享内存未初始化
            }
        }

        int ReadByte(ref byte[] bytData, int lngAddr, int lngSize)
        {
            if (lngAddr + lngSize > m_MemSize) return -2; //超出数据区
            if (shmInitiled)
            {
                Marshal.Copy(m_pwData+lngAddr, bytData, 0, lngSize);
            }
            else
            {
                return -1; //共享内存未初始化
            }
            return 0;     //读成功
        }

        /// <summary>
        /// 0:server online status 1:maxClientNum, 2:nowClientNum,3-6:ClientOnlineStat, 7:clientIndex applied
        /// </summary>
        /// <returns></returns>
        public byte[] ReadShmHead(){
            byte[] tempResult = new byte[15];   // 0-14: server在线/maxClientNum/当前数/在线状态区(3-14)
            var _ = ReadByte(ref tempResult, 0, 15);
            
            return tempResult;
        }

        public bool CheckServerOnlineStatus(){
            return ReadShmHead()[0] > 0;
        }

        public int UpdateOnlineStatus(){

            byte[] head = ReadShmHead();
            if(head[2 + UID] == 0){
                return -3; //server set offline
            }
            if(userName == "server"){
                int onlineClients = 0;
                for(int i = 0; i < maxClientNum; i++){
                    if(head[3 + i] > 0){onlineClients++;}
                }
                WriteByte(2, (byte)onlineClients);
                ResolveCare();          // server 也扫名称区解析自己的 care 目标(如 "UnityProject")

                if(!heartbeat){return 1;}

                WriteByte(0, (byte)(head[0] % 255 + 1));
                for(int i = 0; i < maxClientNum; i++){
                    if(head[3 + i] > 0){
                        if(head[3 + i] != careOnlineStatus[i]){
                            careOnlineStatus[i] = head[3 + i];
                            clientOfflineTick[i] = 0;
                        }else{
                            clientOfflineTick[i]++;

                            if(clientOfflineTick[i] >= maxOfflineTick){
                                onlineClients = Math.Max(0, onlineClients - 1);
                                WriteByte(2, (byte)onlineClients);
                                WriteByte(3 + i, 0);
                                ClearSlotByUid(i + 1);             // 清下线client的名称槽
                                clientOfflineTick[i] = 0;
                                if(careIndex == i + 1){careIndex = -1;}
                            }
                        }
                    }
                }
                return 1;
            }
            else{
                if(head[0] == 0){return -1;}//server offline
                if(careIndex == -1){ ResolveCare(); }   // care目标可能后连入,未解析则重扫
                if(heartbeat){
                    WriteByte(2 + UID, (byte)(head[2 + UID] % 255 + 1));

                    if(head[0] != careOnlineStatus[0]){
                        careOnlineStatus[0] = head[0];
                        offlineTick = 0;
                    }else{offlineTick ++;}
                    
                    // if(head[2 + careIndex] > 0){//暂时不支持client间互相关注
                    //     if(head[2 + careIndex] != careOnlineStatus[1]){
                    //         careOnlineStatus[1] = head[2 + careIndex];
                    //         careOfflineTick = 0;
                    //     }else{careOfflineTick ++;}
                    // }

                    if(offlineTick == maxOfflineTick){
                        offlineTick = 0;
                        return -2;//server offline accidentally
                    }else{
                        return 1;
                    }
                }
                else{
                    return 1;
                }
            }
        }

        /// <summary>
        /// 从名称区(18..)前向后找第一个空槽写入自己的 uid+名字(先到先得)。server 不登记。
        /// </summary>
        public int RegisterSelf(){
            byte[] nameBytes = Encoding.UTF8.GetBytes(userName);
            if(nameBytes.Length > 16){ nameBytes = nameBytes.Take(16).ToArray(); }   // 已在 Init 校验, 双保险
            int slots = (HEADER_SIZE - 18) / 20;
            for(int i = 0; i < slots; i++){
                int off = 18 + i * 20;
                if(ReadByte(off + 1) == 0){          // len==0 视为空槽
                    WriteBytes(off, 0x00, 20);       // 先清本槽(名字区0x00填充)
                    WriteByte(off, (byte)UID);
                    WriteByte(off + 1, (byte)nameBytes.Length);
                    WriteBytes(off + 2, nameBytes);
                    WriteByte(off + 18, 0xFF);
                    WriteByte(off + 19, 0xFF);
                    mySlotOffset = off;
                    return 1;
                }
            }
            return -1;   // 无空槽(超容量), 理论上被 maxClientNum 检查挡住
        }

        /// <summary>
        /// 解析 care 目标: ""→-1; "server"→0; 其它→扫名称区匹配名字, 命中则 careIndex=槽内uid。
        /// </summary>
        public void ResolveCare(){
            if(string.IsNullOrEmpty(care)){ careIndex = -1; return; }
            if(care == "server"){ careIndex = 0; return; }
            int slots = (HEADER_SIZE - 18) / 20;
            for(int i = 0; i < slots; i++){
                int off = 18 + i * 20;
                int len = ReadByte(off + 1);
                if(len >= 1 && len <= 16){
                    byte[] nb = new byte[len];
                    var _ = ReadByte(ref nb, off + 2, len);
                    if(Encoding.UTF8.GetString(nb) == care){
                        careIndex = ReadByte(off);   // 槽内 uid
                        return;
                    }
                }
            }
            careIndex = -1;   // 目标尚未登记, 后续 tick 重试
        }

        /// <summary>
        /// 扫描名称区(18..)，返回用户名等于 targetName 的槽内 uid；找不到返回 -1。供按名解析对端 buffer id。
        /// </summary>
        public int GetUidByName(string targetName){
            if(string.IsNullOrEmpty(targetName)){ return -1; }
            int slots = (HEADER_SIZE - 18) / 20;
            for(int i = 0; i < slots; i++){
                int off = 18 + i * 20;
                int len = ReadByte(off + 1);
                if(len >= 1 && len <= 16){
                    byte[] nb = new byte[len];
                    var _ = ReadByte(ref nb, off + 2, len);
                    if(Encoding.UTF8.GetString(nb) == targetName){
                        return ReadByte(off);
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// 按 uid 清除名称区中对应的槽(用于参与者下线)。
        /// </summary>
        void ClearSlotByUid(int uid){
            int slots = (HEADER_SIZE - 18) / 20;
            for(int i = 0; i < slots; i++){
                int off = 18 + i * 20;
                if(ReadByte(off + 1) >= 1 && ReadByte(off) == uid){
                    WriteBytes(off, 0x00, 20);
                    return;
                }
            }
        }

        /// <summary>
        /// 服务端写入协议元数据（头部末尾3字节）：大小、版本、异或校验。仅 server 在 Init 时调用一次。
        /// </summary>
        public int WriteVersionInfo(){
            byte s = (byte)HEADER_SIZE;
            byte v = PROTOCOL_VERSION;
            byte x = (byte)(s ^ v ^ VERSION_MAGIC);
            WriteByte(15, s);
            WriteByte(16, v);
            WriteByte(17, x);
            return 1;
        }

        /// <summary>
        /// 客户端校验协议元数据一致性。异或/大小/版本任一不符即返回 false（按启动失败处理）。
        /// </summary>
        public bool ValidateVersionInfo(out string reason){
            byte s = (byte)ReadByte(15);
            byte v = (byte)ReadByte(16);
            byte x = (byte)ReadByte(17);
            if((byte)(s ^ v ^ VERSION_MAGIC) != x){
                reason = $"metadata checksum/layout mismatch (s={s}, v={v}, x={x})";
                return false;
            }
            if(s != (byte)HEADER_SIZE){
                reason = $"header size mismatch: shm={s}, local={HEADER_SIZE}";
                return false;
            }
            if(v != PROTOCOL_VERSION){
                reason = $"protocol version mismatch: shm={v}, local={PROTOCOL_VERSION}";
                return false;
            }
            reason = "";
            return true;
        }

        /// <summary>
        /// return: 0:writting, 1-4:readMark, 5:writtenmark, 6:newest start number, 7:newest end number
        /// </summary>
        /// <returns></returns>
        public int[] ReadWriteBufferHead(int _id){
            byte[] tempResult = new byte[15];
            var _ = ReadByte(ref tempResult, writeBufferStartPosAll[_id], 15);
            
            int[] result = new int[8];
            result[0] = tempResult[0];
            for (int i = 1; i < 8; i++){
                result[i] = BytesToInts(tempResult[(i*2-1)..(i*2+1)]);
            }
            return result;
        }
        
        /// <summary>
        /// mode: all, new, newone, newest
        /// </summary>
        /// <param name="_id"></param>
        /// <param name="mode"></param>
        /// <param name="updateReadMark"></param>
        /// <returns></returns>
        public List<byte[]> ReadContent(int _id, string mode = "new")
        {
            if (_id < 0 || _id >= writeBufferStartPosAll.Count || _id == UID)
            {
                throw new ArgumentOutOfRangeException(nameof(_id), "Invalid client ID.");
            }

            // // seqlock: retry up to 3 times if header changes during read (writer modified buffer mid-read)
            // for (int attempt = 0; attempt < 3; attempt++)
            // {
            int[] status = ReadWriteBufferHead(_id);

            // P2 #16: bounds-check endPos to guard against corrupt header from race
            int startPos = writeBufferStartPosAll[_id] + 15;
            int endPos = writeBufferStartPosAll[_id] + status[7];
            if (status[7] < 15 || status[7] > writeBufferLength)
            {
                return new List<byte[]>{};
            }

            if(mode == "newest"){startPos = writeBufferStartPosAll[_id] + status[6];}

            int readMark = status[1+GetProjectedWritePos(_id)];
            int writeMark = status[5];
            if(writeMark < readMark){readMark = 0;}
            else if(mode != "all" && writeMark == readMark){return new List<byte[]>{};}

            if(status[0] == 0 || endPos - startPos <= 4){return new List<byte[]>{};}

            byte[] tempResult = new byte[endPos - startPos];
            var _ = ReadByte(ref tempResult, startPos, endPos-startPos);

            List<byte[]> result = new List<byte[]>();
            for(int i = 0; i < (endPos-startPos) - 1; ){
                int _length = BytesToInts(tempResult[i..(i+2)]);
                // P1 #10: validate length and separator before adding; skip bad bytes instead of breaking
                if(_length != 65535 && _length > 0 && (i + _length + 4) <= tempResult.Length){
                    // verify 0xFF 0xFF separator exists after content
                    if(tempResult[i + _length + 2] == 0xFF && tempResult[i + _length + 3] == 0xFF){
                        result.Add(tempResult[(i+2)..(i+_length+2)]);
                        i += _length + 4;  // skip length(2) + content + separator(2)
                        // newone handled below by readMark selection, not early break
                    }else{
                        i += 1;  // separator mismatch, skip one byte and rescan
                    }
                }else{
                    i += 1;  // P1 #10: skip bad byte and continue scanning instead of breaking
                }
            }

            switch(mode){
                case "all":{
                    readMark = writeMark;
                    WriteReadMark(_id, readMark);
                    return result;
                }
                case "new":{
                    int validReadMark = Math.Min(result.Count, readMark);
                    int validWriteMark = Math.Min(result.Count, writeMark); // FIX-BUG4: save before GetRange
                    result = result.GetRange(validReadMark, validWriteMark - validReadMark);
                    readMark = validWriteMark;  // FIX-BUG4: was Math.Min(result.Count, writeMark) after GetRange
                    WriteReadMark(_id, readMark);
                    return result;
                }
                case "newone":{
                    int newOneIdx = Math.Min(readMark, result.Count - 1);
                    List<byte[]> newOneResult = new List<byte[]>{};
                    if(newOneIdx >= 0 && newOneIdx < result.Count){newOneResult.Add(result[newOneIdx]);}
                    readMark += 1;
                    WriteReadMark(_id, readMark);
                    return newOneResult;
                }
                case "newest":{
                    readMark = writeMark;
                    WriteReadMark(_id, readMark);
                    return result;
                }
                default:{
                    return new List<byte[]>(){};
                }
            }
            // }
            throw new InvalidOperationException("Failed to read content from shared memory buffer.");
        }

            // return Encoding.UTF8.GetString(messageBytes);


        /// <summary>
        /// mode: all, new, newone, newest
        /// </summary>
        /// <param name="_id"></param>
        /// <param name="_mode"></param>
        /// <returns></returns>
        public List<string> ReadMsg(int _id, string _mode){
            List<string> result = new List<string>();
            List<byte[]> msgs = ReadContent(_id, _mode);
            // ReadContent already strips the 2-byte length prefix and 0xFF 0xFF separator,
            // returning pure content bytes. Do NOT re-parse length prefix here.
            foreach(byte[] msg in msgs){
                if(msg.Length > 0){
                    result.Add(Encoding.UTF8.GetString(msg));
                }
            }

            return result;
        }
 
    }
}
