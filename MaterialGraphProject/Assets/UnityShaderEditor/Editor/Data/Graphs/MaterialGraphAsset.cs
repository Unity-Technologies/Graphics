using UnityEngine;
#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
#endif
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public class MaterialGraphAsset
    {
        public static bool ShaderHasError(Shader shader)
        {
            var hasErrorsCall = typeof(ShaderUtil).GetMethod("GetShaderErrorCount", BindingFlags.Static | BindingFlags.NonPublic);
            var result = hasErrorsCall.Invoke(null, new object[] { shader });
            return (int)result != 0;
        }
    }
}
