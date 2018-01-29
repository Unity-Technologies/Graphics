using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace UnityEngine.Experimental.Rendering
{
    public class GraphicTestContext
    {
        [MenuItem("Assets/Open Graphic Test Result")]
        private static void OpenGraphicTestResult()
        {

        }

        [MenuItem("Assets/Open Graphic Test Result", true)]
        private static bool OpenGraphicTestResultValidation()
        {
            List<string> scenes = new List<string>();

            foreach (TestFrameworkTools.TestInfo testInfo in TestFrameworkTools.CollectScenes.HDRP)
            {
                scenes.Add(testInfo.name);
            }

            foreach (TestFrameworkTools.TestInfo testInfo in TestFrameworkTools.CollectScenes.LWRP)
            {
                scenes.Add(testInfo.name);
            }

            return scenes.Contains(Path.GetFileName( AssetDatabase.GetAssetPath(Selection.activeObject) ));
        }
    }
}
