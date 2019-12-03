using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Graphing.Util
{
    static class HackweekHacks
    {
        private const string HACK_PATH = "Assets/S/guiInfo.asset";

        private class SG_ShaderGUIInfo : ScriptableObject
        {
            public string[] tooltips;
            public string[] headers;
        }

        public static void CreateShaderGUIInfo(string[] shaderTooltips, string[] shaderHeaders)
        {
            SG_ShaderGUIInfo guiInfo = new SG_ShaderGUIInfo();

            guiInfo.tooltips = shaderTooltips;
            guiInfo.headers = shaderHeaders;

            AssetDatabase.CreateAsset(guiInfo, HACK_PATH);
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow ();
            Selection.activeObject = guiInfo;
        }

        [MenuItem("Test/Test Creation of SG_ShaderGUIInfo")]
        private static void CreationTest()
        {
            string[] s1 =
            {
                "s1",
                "s2",
                null,
                "s4",
                null
            };
            string[] s2 =
            {
                "cat",
                "dog",
                "pig",
                null,
                null
            };

            CreateShaderGUIInfo(s1, s2);
        }

        [MenuItem("Test/Test Read of SG_ShaderGUIInfo")]
        private static void ReadTest()
        {
            SG_ShaderGUIInfo guiInfo = AssetDatabase.LoadAssetAtPath<SG_ShaderGUIInfo>(HACK_PATH);

            if (guiInfo == null)
            {
                Debug.LogError("Object Not Found!");
                return;
            }

            foreach (string s in guiInfo.tooltips)
            {
                if (s != null)
                    Debug.Log("tooltip: " + s);
                else
                    Debug.Log("null tooltip!");
            }
            foreach (string s in guiInfo.headers)
            {
                if (s != null)
                    Debug.Log("header: " + s);
                else
                    Debug.Log("null header!");
            }

        }
    }
}
