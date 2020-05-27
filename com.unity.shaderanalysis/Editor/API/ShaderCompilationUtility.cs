using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEditor.ShaderAnalysis
{
    public class ShaderCompilationUtility
    {
        static string s_UnityVersionForShader;
        /// <summary>The string to use as UNITY_VERSION in shaders.</summary>
        public static string unityVersionForShader
        {
            get
            {
                if (string.IsNullOrEmpty(s_UnityVersionForShader))
                {
                    var regex = new Regex(@"(?<ver>\d+)\.(?<maj>\d+)\.(?<min>\d+)");
                    var m = regex.Match(Application.unityVersion);
                    var ver = int.Parse(m.Groups["ver"].Value);
                    var maj = int.Parse(m.Groups["maj"].Value);
                    var min = int.Parse(m.Groups["min"].Value);
                    s_UnityVersionForShader = (ver * 100 + maj * 10 + min).ToString();
                }
                return s_UnityVersionForShader;
            }
        }
    }
}
