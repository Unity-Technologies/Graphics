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
            return ShaderUtil.GetShaderErrorCount(shader) > 0;
        }
    }
}
