using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrDestroyParent : MonoBehaviour
{

    void Awake()
    {
        
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnDestroy(){
        Destroy(transform.parent.gameObject);
    }
}
