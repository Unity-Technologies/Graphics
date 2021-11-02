using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Events;

public class HDRP_ShaderGraph_TestSettings : HDRP_TestSettings
{
    public bool compareSGtoBI = true;
    public GameObject sgObjs;
    public GameObject biObjs;
}
