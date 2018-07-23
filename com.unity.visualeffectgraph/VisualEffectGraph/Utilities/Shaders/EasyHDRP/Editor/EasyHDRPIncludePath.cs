using System.IO;
using UnityEngine;
using UnityEditor;

static class EasyHDRPIncludePath
{
#if UNITY_EDITOR
    [ShaderIncludePath]
    public static string[] GetPaths()
    {
        string EasyHDRPPath = "VFXEditor/Utilities/Shaders/EasyHDRP/";
        string path = Path.Combine(Application.dataPath, EasyHDRPPath);
        Debug.Log("Added EasyHDRP Shader include path : " + path);
        return new string[] { path };
    }
#endif
}
