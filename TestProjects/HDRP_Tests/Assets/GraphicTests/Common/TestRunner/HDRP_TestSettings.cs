using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Events;

public class HDRP_TestSettings : GraphicsTestSettings
{
	public UnityEngine.Events.UnityEvent doBeforeTest;
	public int captureFramerate = 0;
	public int waitFrames = 0;
}
