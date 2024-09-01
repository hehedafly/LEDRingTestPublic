using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class move : MonoBehaviour
{
    public Transform tf;
    public Rigidbody m_rb;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(GetType()+ "Display.displays.Length = " + Display.displays.Length);
        for (int i = 0; i < Display.displays.Length; i++)
        {
            Display.displays[i].Activate();
            Screen.SetResolution(Display.displays[i].renderingWidth, Display.displays[i].renderingHeight, true);
        }
        tf = GetComponent<Transform>();
        m_rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        float dz = (float)((Convert.ToInt32(Input.GetKey(KeyCode.UpArrow))-Convert.ToInt32(Input.GetKey(KeyCode.DownArrow)))*0.1);
        float dy = (float)((Convert.ToInt32(Input.GetKey(KeyCode.Space))-Convert.ToInt32(Input.GetKey(KeyCode.LeftShift)))*0.02);
        float dx = (float)((Convert.ToInt32(Input.GetKey(KeyCode.RightArrow))-Convert.ToInt32(Input.GetKey(KeyCode.LeftArrow)))*0.1);
        float dr = (float)(Convert.ToInt32(Input.GetKey(KeyCode.R))- Convert.ToInt32(Input.GetKey(KeyCode.L))*0.1);
        tf.position = new Vector3(tf.position.x+dx, tf.position.y+dy, tf.position.z+dz);
        m_rb.MoveRotation(m_rb.rotation * Quaternion.Euler(0, dr, 0));
        if(Input.GetKeyDown(KeyCode.Escape)){
            Application.Quit();
        }
    }

}
