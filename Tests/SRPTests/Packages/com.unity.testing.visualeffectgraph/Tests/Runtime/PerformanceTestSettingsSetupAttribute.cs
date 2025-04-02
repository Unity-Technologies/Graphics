using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.Scripting;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.VFX;
using UnityEngine.TestTools.Graphics.Performance;

namespace Unity.Testing.VisualEffectGraph.Tests
{
    public class PerformanceTestSettingsSetupAttribute : GraphicsPrebuildSetupAttribute
    {
        public PerformanceTestSettingsSetupAttribute() : base() { }

        // The asset 'Resources/PerformanceTestsSettings' in PerformanceTestSettings.cs sometimes doesn't get created during standalone test builds
        // This causes 'PerformanceTestSettings instance' to be null
        // and any calls to PerformanceTestUtils will throw a NullReferenceException error during test runs
        // By calling PerformanceTestSettings.GetSerializedSettings() as a prebuild setup step, we ensure 'Resources/PerformanceTestsSettings' exists,
        // or gets created if it doesn't exist
        public override void Setup()
        {
#if UNITY_EDITOR
            PerformanceTestSettings.GetSerializedSettings();
#endif
        }
    }
}
