#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics
{
    /// <summary>
    /// Set the player aspect ratio and resolution before the build
    /// </summary>
    public class PreBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }
        public void OnPreprocessBuild(BuildReport report)
        {
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.SetAspectRatio(AspectRatio.Aspect16by9, true);
            PlayerSettings.defaultScreenHeight = ImageAssert.kBackBufferHeight;
            PlayerSettings.defaultScreenWidth = ImageAssert.kBackBufferWidth;
        }
    }
}
#endif
