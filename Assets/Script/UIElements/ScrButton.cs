using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrButton : MonoBehaviour
{
    // Start is called before the first frame update
    public UIUpdate ui_update;

    public void OnClick(){
        if(ui_update != null){ui_update.ControlsParse(name, 1);}
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
