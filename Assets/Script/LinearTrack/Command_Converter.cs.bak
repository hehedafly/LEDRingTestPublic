using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;
using System.Linq;
using System.Collections.Concurrent;

public class Command_Converter
{
    static private List<string> ls_types = new List<string>(){"move", "context_info", "log", "echo", "value_change", "command"};
    private readonly Dictionary<string, byte[]> dic_types =  new Dictionary<string, byte[]>();
    private readonly Dictionary<string, string> dic_types_reverse =  new Dictionary<string, string>();
    public Command_Converter(){
        for(int i=0; i<ls_types.Count(); i++){
            // dic_types.Add(ls_types[i], BitConverter.GetBytes((short)i));
            // dic_types_reverse.Add(BitConverter.ToString(BitConverter.GetBytes((short)i)), ls_types[i]);
            dic_types.Add(ls_types[i], new byte[]{(byte)i});
            dic_types_reverse.Add(i.ToString(), ls_types[i]);
        }
    }


    public byte[] ProcessSerialPortBytes(byte[] readBuffer){
        for (int i = 0; i < readBuffer.Length; i++){
            if (IsStartOfMessage(readBuffer, i)){
                int endIndex = FindMarkOfMessage(false, readBuffer, i);
                if (endIndex != -1){
                    int _msgLength = readBuffer[i+2];
                    if(endIndex - i -3 != _msgLength){
                        Debug.LogError("incomplete msg received: "+string.Join(",", readBuffer[i..(endIndex+1)]));
                        return new byte[]{};
                    }
                    byte[] completeMessage = new byte[endIndex - i];
                    //Array.Copy(readBuffer, i+2, completeMessage, 0, completeMessage.Length-2);
                    Array.Copy(readBuffer, i+1, completeMessage, 0, completeMessage.Length-1);
                    return completeMessage;
                }
            }
        }
        return new byte[]{};
    }

    public bool IsStartOfMessage(byte[] buffer, int index){
        if(index>=buffer.Length-1){return false;}
        //return buffer[index] == 0xAA && buffer[index + 1] == 0xBB;
        return buffer[index] == 0xAA;
    }

    public int FindMarkOfMessage(bool start_or_end, byte[] buffer, int startIndex){
        for (int i = startIndex; i < buffer.Length; i ++){
            //if ((start_or_end && buffer[i] == 0xAA && buffer[i + 1] == 0xBB) ||(!start_or_end && buffer[i] == 0xCC && buffer[i + 1] == 0xDD)){
            if ((start_or_end && buffer[i] == 0xAA) ||(!start_or_end && buffer[i] == 0xDD)){
                return i;
            }
        }
        return -1; // 未找到结束标记
    }

    public int GetCommandType(byte[] command_array, out int startInd){
        int temp_start = 0;
        if(command_array[0] == 0xAA){temp_start = 1;}
        startInd = temp_start+2;//1byte type, 1byte length
        return command_array[temp_start];
    }

    public int GetCommandType(string command_string){
        string temp_type = command_string[..command_string.IndexOf(":")];
        if(dic_types.ContainsKey(temp_type)){
            return dic_types[temp_type][0];
        }
        else{
            return -1;
        }
    }

    public byte[] ConvertToByteArray(string command){
        /*msg form:
        //start byte: 2byte
        start byte: 1byte
        type:       2byte
        length:     1byte
        content:    ...
        //end byte:   2byte
        end byte:   1byte
        total: msg_length+7byte
        */
        string[] parts = command.Split(':');
        string type = parts[0].Trim();
        string content = parts[1].Trim();
        if(content.Length>250){return new byte[]{};}

        // 创建最终的字节数组
        //byte[] header = (new byte[]{0xAA, 0xBB}).Concat(dic_types[type]).ToArray();
        byte[] header = (new byte[]{0xAA}).Concat(dic_types[type]).ToArray();
        byte[] len = new byte[]{(byte)content.Length};
        byte[] body = Encoding.UTF8.GetBytes(content);
        //byte[] footer = new byte[] { 0xCC, 0xDD }; // 结束标志
        byte[] footer = new byte[] {0xDD}; // 结束标志
        
        return header.Concat(len).Concat(body).Concat(footer).ToArray();
    }

    public byte[] ConvertToByteArray(byte[] command, string type){
        /*msg form:
        //start byte: 2byte
        start byte: 1byte
        type:       2byte
        length:     1byte
        content:    ...
        //end byte:   2byte
        end byte:   1byte
        total: msg_length+7byte
        */
        if(command.Length>=250){return new byte[]{};}

        // 创建最终的字节数组
        byte[] header = (new byte[]{0xAA}).Concat(dic_types[type]).ToArray();
        byte[] len = new byte[]{(byte)command.Length};
        byte[] footer = new byte[] {0xDD}; // 结束标志
        
        return header.Concat(len).Concat(command).Concat(footer).ToArray();
    }

    public string ConvertToString(byte[] byteArray){//要求无起止符
        //dic_types_reverse.TryGetValue(BitConverter.ToString(new byte[]{byteArray[temp_id+0], byteArray[temp_id+1]}), out string typeId);
        dic_types_reverse.TryGetValue(byteArray[0].ToString(), out string typeId);
        if(typeId==""){return "";}

        //short contentLength = byteArray[2];
        short contentLength = byteArray[1];
        string content;
        //下两句index均减了1
        if(typeId=="move"){content=$"x={(int)byteArray[2]}, y={(int)byteArray[3]}";}
        else{content = Encoding.UTF8.GetString(byteArray, 2, contentLength);}
        return $"{typeId}:{content}";
    }

    public byte[] Read_buffer_concat(List<byte[]> serial_read_content_ls, int _start, int _end, byte[] _bytes){//cast bytes from _start to end. _end=-1||_end>=length: cast _bytes too
        int temp_end=_end;
        if(_end==-1 || _end>serial_read_content_ls.Count()-1){temp_end=serial_read_content_ls.Count();}
        if(temp_end-_start<1){return _bytes;}
        byte[] result = new byte[]{};
        for(int i=_start; i<temp_end; i++){
            result=result.Concat(serial_read_content_ls[i]).ToArray();
        }
        if(_end==-1 || _end>serial_read_content_ls.Count()-1){result=result.Concat(_bytes).ToArray();}
        return result;
    }

    public byte[] Read_buffer_concat(List<byte[]> serial_read_content_ls, int _start, int _end){//cast bytes from _start to end.
        byte[] temp_byte = new byte[]{};
        return Read_buffer_concat(serial_read_content_ls, _start, _end, temp_byte);
    }

}
