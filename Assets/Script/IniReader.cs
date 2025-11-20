using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

public class IniReader
{
  public string path;//ini文件的路径
  List<string> readContents = new List<string>();
  List<string> readDefaultContents = new List<string>();
  public IniReader(string path)
  {
    this.path=path;
  }
  [DllImport("kernel32")]
  public static extern long WritePrivateProfileString(string section,string key,string value,string path);
  [DllImport("kernel32")]
  public static extern int GetPrivateProfileString(string section,string key,string deval,StringBuilder stringBuilder,int size,string path);

//写入ini文件
public void WriteIniContent(string section,string key,string value)
{
  WritePrivateProfileString(section,key,value,this.path);
}

//读取Ini文件
public string ReadIniContent(string section,string key, string value_in_default, bool Confirm = false){
  StringBuilder temp=new StringBuilder(8192);
  int i=GetPrivateProfileString(section,key,"",temp,8192,this.path);
  if(temp.Length == 0 && Confirm){return "";}
  if(temp.Length==0){
    readDefaultContents.Add($"section: {section}, key: {key}, default value:{value_in_default}");
    return value_in_default;
  }
  else{
    readContents.Add($"section: {section}, key: {key}, value:{temp.ToString()}");
    return temp.ToString();
  }
}

public List<List<string>> GetReadContent(){
  return new List<List<string>>{readDefaultContents, readContents};
}

//判断路径是否正确
public bool Exists()
{
   return File.Exists(this.path);
}
}