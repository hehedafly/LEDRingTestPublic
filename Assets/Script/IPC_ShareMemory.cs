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
        long m_MemSize = 32+5*16*1024;

        string name;
        string care;
        int UID;
        int careIndex;//write时关注对应用户读取自己内容情况
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
            UID = -1;
            careIndex = -1;
            // contentBegin = 0;
            writeBufferLength = 16 * 1024;
            writeBufferStartPosAll = new List<int>();
        }

        ~Sharedmm()
        {
            // if(m_hSharedMemoryFile != IntPtr.Zero){
            //     CloseHandle(m_hSharedMemoryFile);
            // }
            CloseSharedmm();
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

                    List<byte> nowServerStatus = ReadShmHead().ToList();
                    maxClientNum = nowServerStatus[1];
                    if(nowServerStatus[2] >= maxClientNum){
                        UnmapViewOfFile(m_pwData);
                        CloseHandle(m_hSharedMemoryFile);
                        shmCreated = false;

                        if(maxClientNum == 0){throw new Exception("server offline");}
                        else{throw new Exception($"already {maxClientNum} clients on server");}
                    }else{
                        UID = nowServerStatus.FindIndex(3, x => x == 0) - 2;
                        if(UID < 0 || UID > maxClientNum){
                            UnmapViewOfFile(m_pwData);
                            CloseHandle(m_hSharedMemoryFile);
                            shmCreated = false;

                            throw new Exception("wrong status record, failed to get index");
                        }
                        WriteByte(2, (byte)(nowServerStatus[2]+1));
                        WriteByte(3 + UID - 1, 1);
                        ApplyForCare();
                        Debug.Log($"UID: {UID}");
                    }

                    for (int i = 32; i < 32 + (maxClientNum + 1) * writeBufferLength; i += writeBufferLength)
                    {
                        writeBufferStartPosAll.Add(i);
                    }
                    
                    if(care == "server"){careIndex = 0;}
                    writeBufferStartPos = writeBufferStartPosAll[UID];
                    newestStartPos = 15;
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

        bool IsValidHandle(IntPtr handle) {
            // return handle == IntPtr.Zero || handle == new IntPtr(-1);
            return handle != IntPtr.Zero && handle != new IntPtr(-1);
        }

        /// <summary>
        /// 关闭共享内存
        /// </summary>
        public void CloseSharedmm(bool manually = false)
        {   
            if(!closed){
                Debug.Log("closing");
                try{
                if(shmInitiled){
                    Debug.Log("shmInitiled");
                    if(manually && IsValidHandle(m_pwData) && IsValidHandle(m_hSharedMemoryFile)){
                        List<byte> nowServerStatus = ReadShmHead().ToList();
                        WriteByte(2, (byte)(nowServerStatus[2]-1));
                        WriteByte(2 + UID, 0);
                    }
                    if(IsValidHandle(m_pwData)){
                        Debug.Log($"UnmapViewOfFile({m_pwData})");
                        UnmapViewOfFile(m_pwData); m_pwData = IntPtr.Zero;
                    }
                    if(IsValidHandle(m_hSharedMemoryFile)){
                        Debug.Log($"CloseHandle({m_hSharedMemoryFile})");
                        CloseHandle(m_hSharedMemoryFile); m_hSharedMemoryFile = IntPtr.Zero;
                    }
                    shmInitiled = false;
                }
                else if (shmCreated){
                    if(m_hSharedMemoryFile != null){CloseHandle(m_hSharedMemoryFile); m_hSharedMemoryFile = IntPtr.Zero;}
                    shmCreated = false;
                }
                Debug.Log("closed");
                closed = true;
                }
                catch(Exception e){
                    Debug.LogError(e.Message);
                }
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

            int startPos = writeBufferStartPos + newestStartPos;//0-15 contains read and write message
            if(newestEndPos + message.Length + 4 >= writeBufferLength){
                //后续再加对careindex的判断
                if(careIndex != -1){
                    clearPos = status[1 + GetProjectedWritePos(careIndex)];
                }
                WriteClear(clearPos);
                startPos = writeBufferStartPos + 15;
            }

            WriteWritingStatus(writeBufferStartPos, true);

            message = IntToBytes(message.Length).Concat(message).ToArray();
            WriteBytes(startPos, message);
            newestStartPos = newestStartPos + message.Length + 2;
            newestEndPos = newestStartPos + message.Length + 2;//+2:split codon: 0xFF, 0xFF
            messageStartPosLs.Add(newestStartPos);
            messageLengthLs.Add(message.Length);
            writtenmark += 1;
            WriteWriteMark(writtenmark);
            WriteNewStartAndEndPos(newestStartPos, newestEndPos);

            WriteWritingStatus(writeBufferStartPos, false);

            // Debug.Log($"writtenmark: {writtenmark}");
            return clearPos;
        }
        
        int WriteClear(int clearPos = -1){
            if (writtenmark == 0){return 0;}
            if (clearPos <= 0 || clearPos > writtenmark){clearPos = writtenmark;}
            
            byte[] storedMsg = new byte[newestEndPos - messageStartPosLs[clearPos - 1]];
            ReadByte(ref storedMsg, writeBufferStartPos + messageStartPosLs[clearPos - 1], newestEndPos - messageStartPosLs[clearPos - 1]);

            // Debug.Log($"writtenmark: {writtenmark}, clearPos: {clearPos}");
            messageStartPosLs.RemoveRange(0, clearPos);
            messageLengthLs.RemoveRange(0, clearPos);
            newestStartPos = messageStartPosLs.Count > 0? messageStartPosLs[messageLengthLs.Count-1] : 0;
            newestEndPos = messageStartPosLs.Count > 0? newestStartPos + messageLengthLs[messageLengthLs.Count-1] : 0;
            writtenmark = writtenmark - clearPos;

            byte[] bytes = new byte[writeBufferLength - 15];
            Array.Fill(bytes, (byte)0xFF);
            WriteBytes(writeBufferStartPos + 15, bytes);

            if (messageStartPosLs.Count > 0){
                WriteBytes(writeBufferStartPos + 15, storedMsg);
            }
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
            byte[] tempResult = new byte[7];
            var _ = ReadByte(ref tempResult, 0, 7);
            
            return tempResult;
        }

        public bool CheckServerOnlineStatus(){
            return ReadShmHead()[0] == 1;
        }

        public int ApplyForCare(){
            WriteByte(7, (byte)UID);
            WriteByte(8, (byte)name.Length);
            WriteBytes(9, Encoding.UTF8.GetBytes(name));
            return 1;
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
                if(_length != 65535 && tempResult.Length > (i+_length+2)){
                    result.Add(tempResult[(i+2)..(i+_length+2)]);
                    i += _length+4;
                    if(mode == "newone"){break;}
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
                    int validReadMark = Math.Min(result.Count, readMark);
                    result =  result.GetRange(validReadMark, Math.Min(result.Count - validReadMark, writeMark));
                    readMark = writeMark;
                    WriteReadMark(_id, readMark);
                    return result;
                    // break;
                }
                case "newone":{
                    // result =  result.GetRange(readMark, 1);
                    readMark += 1;
                    WriteReadMark(_id, readMark);
                    return result;
                    // break;
                }
                case "newest":{//result根据被读对象最新的结果读取了最新的内容
                    readMark = writeMark;
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

        /// <summary>
        /// mode: all, new, newone, newest
        /// </summary>
        /// <param name="_id"></param>
        /// <param name="_mode"></param>
        /// <returns></returns>
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