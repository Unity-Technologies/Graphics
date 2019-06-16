using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    public class LightTest : MonoBehaviour
    {
       // public static Vector3 position;
        // Update is called once per frame
        void Update()
        {
            Shader.SetGlobalVector("_LightPos", transform.position);
            //position = transform.position;
        }
    }
}
