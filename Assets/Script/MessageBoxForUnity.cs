using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public static class MessageBoxForUnity
{
    // Start is called before the first frame update
    [DllImport("User32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
    public static extern int MessageBox(IntPtr handle, String message, String title, int type);//具体方法

    public static int Ensure(string message, string title){
        return MessageBox(IntPtr.Zero, message, title, 0);
        //确认：1
    }

    public static int EnsureAndCancel(string message, string title){
        return MessageBox(IntPtr.Zero, message, title, 1);
        //确认：1，取消：2
    }

    public static int Pause(string message, string title){
        return MessageBox(IntPtr.Zero, message, title, 2);
        //中止：3，重试：4，忽略：5
    }

    public static int YesOrNoWithCancel(string message, string title){
        return MessageBox(IntPtr.Zero, message, title, 3);
        //是：6，否：7，取消：2
    }

    public static int YesOrNo(string message, string title){
        return MessageBox(IntPtr.Zero, message, title, 4);
        //是：6，否：7
    }

    public static int Retry(string message, string title){
        return MessageBox(IntPtr.Zero, message, title, 5);
        //重试：4，取消：2
    }

    public static int PauseOrRetry(string message, string title){
        return MessageBox(IntPtr.Zero, message, title, 6);
        //取消：2，重试：10，继续：11
    }

}


/*
using System; //引用命名空间下
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// Windows消息框
/// </summary>
public class ChinarWindowsMessage : MonoBehaviour
{
    public  Button[] Buttons;      //按钮组
    private int      returnNumber; //返回值


    private void Start()
    {
        for (int i = 0; i < Buttons.Length; i++)//动态绑定
        {
            var i1 = i;
            Buttons[i].onClick.AddListener(() => Button(i1));
        }
    }


    /// <summary>
    /// 9个按钮对应弹框
    /// </summary>
    /// <param name="index"></param>
    private void Button(int index)
    {
        switch (index)
        {
            case 0:
                returnNumber = ChinarMessage.MessageBox(IntPtr.Zero, "Chinar-0:返回值均：1", "确认", 0);
                print(returnNumber);
                break;
            case 1:
                returnNumber = ChinarMessage.MessageBox(IntPtr.Zero, "Chinar-1:确认：1，取消：2", "确认|取消", 1);
                print(returnNumber);
                break;
            case 2:
                returnNumber = ChinarMessage.MessageBox(IntPtr.Zero, "Chinar-2:中止：3，重试：4，忽略：5", "中止|重试|忽略", 2);
                print(returnNumber);
                break;
            case 3:
                returnNumber = ChinarMessage.MessageBox(IntPtr.Zero, "Chinar-3:是：6，否：7，取消：2", "是 | 否 | 取消", 3);
                print(returnNumber);
                break;
            case 4:
                returnNumber = ChinarMessage.MessageBox(IntPtr.Zero, "Chinar-4:是：6，否：7", "是 | 否", 4);
                print(returnNumber);
                break;
            case 5:
                returnNumber = ChinarMessage.MessageBox(IntPtr.Zero, "Chinar-5:重试：4，取消：2", "重试 | 取消", 5);
                print(returnNumber);
                break;
            case 6:
                returnNumber = ChinarMessage.MessageBox(IntPtr.Zero, "Chinar-6:取消：2，重试：10，继续：11", "取消 | 重试 | 继续", 6);
                print(returnNumber);
                break;
        }
    }
}
*/