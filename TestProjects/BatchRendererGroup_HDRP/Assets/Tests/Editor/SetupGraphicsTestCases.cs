using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections.Generic;

public class SetupGraphicsTestCases : IPrebuildSetup
{
    public void Setup()
    {
        // Work around case #1033694, unable to use PrebuildSetup types directly from assemblies that don't have special names.
        // Once that's fixed, this class can be deleted and the SetupGraphicsTestCases class in Unity.TestFramework.Graphics.Editor
        // can be used directly instead.
        UnityEditor.TestTools.Graphics.SetupGraphicsTestCases.Setup(GraphicsTests.path);

        // Configure project for XR tests
        Unity.Testing.XR.Editor.InjectMockHMD.SetupLoader();
    }
}
