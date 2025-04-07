using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;

// As Custom Nodes and Shader Graphs are imported as part of the same samples we need to refresh the assets after nodes have been compiled.
public static class ForceReloadSampleShaderGraphAssets
{
    static readonly string samplesPath = "Assets/Samples/Shader Graph/version/UGUI Shaders";
    static SearchRequest request;
    
    [InitializeOnLoadMethod]
    static void Init()
    {
        request = Client.Search("com.unity.shadergraph", true);
        EditorApplication.update += Progress;
    }

    static void Progress()
    {
        if (!request.IsCompleted)
            return;
        
        var pkgInfo = request.Result;

        if (request.Status == StatusCode.Success && pkgInfo.Length > 0)
        {
            var pkgVersion = pkgInfo[0].version;
            var searchPath = samplesPath.Replace("version", pkgVersion);
            var guids = AssetDatabase.FindAssets("t:Shader t:SubGraphAsset", new string[] {searchPath});
            
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var ext = Path.GetExtension(path);
                    if (ext == ".shadergraph" || ext == ".shadersubgraph")
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during scripted asset import: {e.Message}");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        EditorApplication.update -= Progress;

        // self destruction
        var thisGuid = AssetDatabase.FindAssets($"t:Script ForceReloadSampleShaderGraphAssets", new string[] {"Assets/Samples/Shader Graph"});
        if (thisGuid.Length > 0)
        {
            AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(thisGuid[0]));
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
