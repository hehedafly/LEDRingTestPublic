using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO.MemoryMappedFiles;
using System.Diagnostics;

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
    initialization: 0-31 0x00, 32.. 0xFF
    name = server/<custom>
    index = 0(server) or 1-4
    care = certain name("UnityProject")/"", only one allowed in this version, if "": ignore all write/read Mark, else update marks base on cared one
    careindex = index/-1, if cared one online and applied, update to index of cared one, if multiple have same name, take the frist one applied, until its offline, then wait for another apply.

    0:              server Online status(0/1)                                           $write by Server
    1:              max client number(4 max)                                            $write by Server
    2:              now client number(0-4)                                              $write by client, read then check online status
    3-6:            clients online status(0/1)                                          $write by client, check from 0 to 3, frist zero value as client's own index
    7:              client index applied                                                $write by client, check if careindex == -1
    8:              name length of client applied                                       $write by client
    9-31            name of client applied                                              $write by client

    32 - 32+16*1024:Server write buffer
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
 
        IntPtr m_hSharedMemoryFile = IntPtr.Zero;
        IntPtr m_pwData = IntPtr.Zero;
        // bool m_bAlreadyExist = false;
        bool m_bInit = false;
        long m_MemSize = 32+5*16*1024;

        string name;
        string care;
        int index;
        int careIndex;//write时关注对应用户读取自己内容情况
        // int contentBegin;
        unsafe byte* ShmBuffer;
        int maxClientNum;
        int writeBufferStartPos;
        int writeBufferLength;
        List<int> writeBufferStartPosAll;

        byte[] splitCondon = new byte[]{0xFF, 0xFF};
        int writtenmark = -1;
        int newestStartPos = -1;
        int newestEndPos = -1;
        List<int> messageStartPos = new List<int>();
 
        /// <summary>
        /// name: "server" or other client name, not the name of the shared memory(claim in Init func)
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_care"></param> <summary>
        /// 
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_care"></param>
        public Sharedmm(string _name, string _care)
        {
            name = _name;
            care = _care;
            index = -1;
            careIndex = -1;
            // contentBegin = 0;
            writeBufferLength = 16 * 1024;
            writeBufferStartPosAll = new List<int>();
        }

        ~Sharedmm()
        {
            if(m_hSharedMemoryFile != IntPtr.Zero){
                CloseHandle(m_hSharedMemoryFile);
            }
            Close();
        }
 
        /// <summary>
        /// init, throw exception if failed to create
        /// </summary>
        /// <param name="strName">共享内存名称</param>
        /// <param name="lngSize">共享内存大小</param>
        /// <returns></returns>
        unsafe public int Init(string strName, long lngSize)
        {
            if (lngSize <= 0 || lngSize > 32+5*16*1024){lngSize = 32+5*16*1024;}

            // m_MemSize = lngSize;
            if (strName.Length > 0){
                if (name == "server"){
                    m_hSharedMemoryFile = CreateFileMapping(INVALID_HANDLE_VALUE, IntPtr.Zero, 0x04, 0, (uint)lngSize, strName);
                }else{
                    m_hSharedMemoryFile = CreateFileMapping(INVALID_HANDLE_VALUE, IntPtr.Zero, 0x04, 0, (uint)lngSize, strName);
                    // m_hSharedMemoryFile = OpenFileMapping(FILE_MAP_ALL_ACCESS, true, strName);
                }

                // if (m_hSharedMemoryFile == IntPtr.Zero)
                if (m_hSharedMemoryFile == null)
                {
                    // m_bAlreadyExist = false;
                    int errorCode = GetLastError();
                    m_bInit = false;
                    throw new Exception("failed to create and map");
                }
                else
                {
                    int errorCode = GetLastError();
                    if (errorCode == ERROR_ALREADY_EXISTS)  //已经创建
                    {
                        if(name == "server"){
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
                    m_bInit = false;
                    CloseHandle(m_hSharedMemoryFile);
                    throw new Exception("failed to map to" + m_hSharedMemoryFile.ToString());
                }
                else
                {
                    m_bInit = true;
                    // if (m_bAlreadyExist == true){
                    // int size = Marshal.SizeOf(typeof(byte));
                    ShmBuffer = (byte*)m_pwData.ToPointer();

                    List<byte> nowServerStatus = ReadShmHead().ToList();
                    maxClientNum = nowServerStatus[1];
                    if(nowServerStatus[2] >= maxClientNum){
                        throw new Exception($"{maxClientNum} clients on server");
                    }else{
                        index = nowServerStatus.FindIndex(3, x => x == 0) - 2;
                        if(index < 0 || index > maxClientNum){
                            throw new Exception("wrong status record, failed to get index");
                        }
                        WriteByte(2, (byte)(nowServerStatus[2]+1));
                        WriteByte(3 + index - 1, 1);
                    }

                    for (int i = 32; i < 32 + (maxClientNum + 1) * writeBufferLength; i += writeBufferLength)
                    {
                        writeBufferStartPosAll.Add(i);
                    }
                    
                    if(care == "server"){careIndex = 0;}
                    writeBufferStartPos = writeBufferStartPosAll[index];
                    newestStartPos = writeBufferStartPos  + 15;
                    newestEndPos = newestStartPos;
                    WriteBytes(writeBufferStartPos, 0x00, 15);
                    writtenmark = 0;
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

        /// <summary>
        /// 关闭共享内存
        /// </summary>
        public void Close()
        {
            if (m_bInit)
            {
                List<byte> nowServerStatus = ReadShmHead().ToList();
                WriteByte(2, (byte)(nowServerStatus[2]-1));
                WriteByte(2 + index, 0);
                UnmapViewOfFile(m_pwData);
                CloseHandle(m_hSharedMemoryFile);
            }
        }

        public int BytesToInts(byte[] bytes){
            if (bytes.Length != 2){
                throw new ArgumentException("");
            }
            return bytes[0] * 255 + bytes[1];
        }

        public byte[] IntToBytes(int i){
            if(i < 0 || i > 65535){return splitCondon;}

            return new byte[]{(byte)(i / 255), (byte)(i % 255)};
        }
        
        int GetProjectedWritePos(int _id){
            return index > _id? index-1 : index;
        }

        unsafe int WriteByte(int _ind, byte _val){
            ShmBuffer[_ind] = _val;
            return 1;
        }

        unsafe int WriteBytes(int _pos, byte _byte, int _length){
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

        unsafe int WriteBytes(int _ind, byte[] bytes){
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
        
        public int WriteContent(byte[] message, bool clear = false)
        {
            if (index == -1)
            {
                throw new InvalidOperationException("Index not set.");
            }
            if (clear){
                newestStartPos = 15;
                messageStartPos.Clear();
            }else if(careIndex != -1){
                int[] status = ReadWriteBufferHead(index);
                int readMark = status[1+GetProjectedWritePos(careIndex)];
                if(readMark == writtenmark){
                    newestStartPos = 15;
                    messageStartPos.Clear();
                }
            }

            int startPos = writeBufferStartPos + newestStartPos;//0-15 contains read and write message
            if(newestEndPos + message.Length + 4 >= writeBufferLength)
            {
                //后续再加对careindex的判断
                WriteClear();
                startPos = writeBufferStartPos + 15;
            }

                WriteWritingStatus(writeBufferStartPos, true);

                message = IntToBytes(message.Length).Concat(message).ToArray();
                // Array.Copy(message, 0, ShmBuffer, startPos, message.Length);
                //11-14暂时不写
                WriteBytes(startPos, message);
                newestStartPos = startPos;
                newestEndPos = newestStartPos + message.Length + 2;//+2:0xFF, 0xFF
                messageStartPos.Add(newestStartPos);
                writtenmark += 1;
                WriteWriteMark(writtenmark);
                WriteNewStartAndEndPos(newestEndPos, newestEndPos);

                WriteWritingStatus(writeBufferStartPos, false);


            return 1;
        }
        
        int WriteClear(int clearPos = 0){
            writtenmark = 0;
            newestStartPos = 0;
            newestEndPos = 0;
            messageStartPos.Clear();
            byte[] bytes = new byte[writeBufferLength - 15];
            Array.Fill(bytes, (byte)0xFF);
            WriteBytes(writeBufferStartPos + 15, bytes);
            // Array.Copy(bytes, 0, ShmBuffer, writeBufferStartPosAll[index] + 15, bytes.Length);
            return 1;
        }

        int WriteWriteMark(int writemark){
            int writePos = writeBufferStartPos + 9;
            // ShmBuffer[writePos] = (byte)writemark;
            WriteBytes(writePos, IntToBytes(writemark));
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
            // Array.Copy(IntToBytes(readmark), 0, ShmBuffer, writePos, 2);
            return 1;
        }

        public int ReadByte(ref byte[] bytData, int lngAddr, int lngSize)
        {
            if (lngAddr + lngSize > m_MemSize) return 2; //超出数据区
            if (m_bInit)
            {
                Marshal.Copy(m_pwData+lngAddr, bytData, 0, lngSize);
            }
            else
            {
                return 1; //共享内存未初始化
            }
            return 0;     //读成功
        }

        /// <summary>
        /// 0:server online status 1:maxClientNum, 2:nowClientNum,3-6:ClientOnlineStat, 7:clientIndex applied
        /// </summary>
        /// <returns></returns>
        public byte[] ReadShmHead(){
            byte[] tempResult = new byte[7];
            var _ = ReadByte(ref tempResult, 0, 7);
            
            return tempResult;
        }

        public bool CheckServerOnlineStatus(){
            return ReadShmHead()[0] == 1;
        }

        /// <summary>
        /// 0:writting, 1-4:readMark, 5:writtenmark, 6:newest startIndex, 7:newest endIndex
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
        /// 
        /// </summary>
        /// <param name="_id"></param>
        /// <param name="mode"></param>
        /// <param name="updateReadMark"></param>
        /// <returns></returns>
        public List<byte[]> ReadContent(int _id, string mode = "new")
        {
            if (_id < 0 || _id >= writeBufferStartPosAll.Count || _id == index)
            {
                throw new ArgumentOutOfRangeException(nameof(_id), "Invalid client ID.");
            }

            int[] status = ReadWriteBufferHead(_id);

            int startPos = writeBufferStartPosAll[_id] + 15;
            int endPos = writeBufferStartPosAll[_id] + status[7];

            if(mode == "newest"){startPos = writeBufferStartPosAll[_id] + status[6];}

            int readMark = status[1+GetProjectedWritePos(_id)];
            int writeMark = status[5];
            if(writeMark < readMark){readMark = 0;}
            else if(mode != "all" && writeMark == readMark){return new List<byte[]>(){};}

            if(status[0] == 0 || endPos - startPos <= 4){return new List<byte[]>(){};}

            byte[] tempResult = new byte[endPos - startPos];
            var _ = ReadByte(ref tempResult, startPos, endPos-startPos);

            List<byte[]> result = new List<byte[]>();
            for(int i = 0; i < (endPos-startPos); ){
                int _length = BytesToInts(tempResult[i..(i+2)]);
                if(_length != 65280){
                    result.Add(tempResult[(i+2)..(i+_length+2)]);
                    i += _length+4;
                }else{
                    break;
                }
            }
            // Find the length of the message
            switch(mode){
                case "all":{
                    readMark = writeMark;
                    WriteReadMark(_id, readMark);
                    return result;
                    // break;
                }
                case "new":{
                    result =  result.GetRange(readMark, writeMark);
                    readMark = writeMark;
                    WriteReadMark(_id, readMark);
                    return result;
                    // break;
                }
                case "newone":{
                    result =  result.GetRange(readMark, readMark);
                    readMark += 1;
                    WriteReadMark(_id, readMark);
                    return result;
                    // break;
                }
                case "newest":{
                    readMark += 1;
                    WriteReadMark(_id, readMark);
                    return result;
                    // break;
                }
                default:{
                    return new List<byte[]>(){};
                    // break;
                }
            }

            // return Encoding.UTF8.GetString(messageBytes);
        }

        public List<string> ReadMsg(int _id, string _mode){
            List<string> result = new List<string>();
            List<byte[]> msgs = ReadContent(_id, _mode);
            foreach(byte[] msg in msgs){
                if(msg.Length >= 3){
                    result.Add(Encoding.UTF8.GetString(msg));
                }
            }

            return result;
        }
 
    }
}