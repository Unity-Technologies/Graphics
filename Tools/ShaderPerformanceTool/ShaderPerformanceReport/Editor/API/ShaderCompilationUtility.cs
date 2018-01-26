using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEditor.Experimental.ShaderTools
{
    public class ShaderCompilationUtility
    {
        static string k_UnityVersionForShader = null;
        public static string unityVersionForShader
        {
            get
            {
                if (string.IsNullOrEmpty(k_UnityVersionForShader))
                {
                    var regex = new Regex(@"(?<ver>\d+)\.(?<maj>\d+)\.(?<min>\d+)");
                    var m = regex.Match(Application.unityVersion);
                    var ver = int.Parse(m.Groups["ver"].Value);
                    var maj = int.Parse(m.Groups["maj"].Value);
                    var min = int.Parse(m.Groups["min"].Value);
                    k_UnityVersionForShader = (ver * 100 + maj * 10 + min).ToString();
                }
                return k_UnityVersionForShader;
            }
        }
    }
}
