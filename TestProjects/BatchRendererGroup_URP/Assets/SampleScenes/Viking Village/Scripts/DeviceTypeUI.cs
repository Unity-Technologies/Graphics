using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeviceTypeUI : MonoBehaviour
{
    
    // Start is called before the first frame update
    void Start()
    {
        string deviceName = SystemInfo.graphicsDeviceName;
        string deviceTypeName = SystemInfo.graphicsDeviceType.ToString();
        GetComponent<Text>().text = deviceName + "\n" + deviceTypeName;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
