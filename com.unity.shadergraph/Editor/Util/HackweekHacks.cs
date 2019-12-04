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

        public static void CreateShaderGUIInfo(string[] shaderTooltips, string[] shaderHeaders)
        {
            SG_ShaderGUIInfo guiInfo = new SG_ShaderGUIInfo();

            if (shaderTooltips.Length != shaderHeaders.Length)
            {
                Debug.LogError("shaderTooltips.Length != shaderHeaders.Length");
                return;
            }

            guiInfo.tooltips = shaderTooltips;
            guiInfo.headers = shaderHeaders;

            AssetDatabase.CreateAsset(guiInfo, HACK_PATH);
            AssetDatabase.Refresh();
        }

        public static void GatherTooltipsAndHeaders(out string[] tips, out string[] heads)
        {
            SG_ShaderGUIInfo guiInfo = AssetDatabase.LoadAssetAtPath<SG_ShaderGUIInfo>(HACK_PATH);
            if (guiInfo == null)
            {
                Debug.LogError("SG_ShaderGUIInfo guiInfo Object Not Found!");
                tips = null;
                heads = null;
                return;
            }

            int c = guiInfo.tooltips.Length;
            if (c != guiInfo.headers.Length)
            {
                Debug.LogError("somehow... shaderTooltips.Length != shaderHeaders.Length");
                tips = null;
                heads = null;
                return;
            }

            tips = new string[c];
            heads = new string[c];
            for (int x = 0; x < c; x++)
            {
                tips[x] = guiInfo.tooltips[x];
                heads[x] = guiInfo.headers[x];
            }

            // guiInfo.PrintMe();
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

            // guiInfo.PrintMe();
        }
    }
}
