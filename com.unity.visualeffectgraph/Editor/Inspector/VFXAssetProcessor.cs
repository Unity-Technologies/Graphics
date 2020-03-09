using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.VFX;
using UnityEditor.VFX;


class VFXAssetProcessor : AssetPostprocessor
{
    public const string k_ShaderDirectory = "Shaders";
    public const string k_ShaderExt = ".vfxshader";
    public static bool allowExternalization { get { return EditorPrefs.GetBool(VFXViewPreference.allowShaderExternalizationKey, false); } }

    void OnPreprocessAsset()
    {
        if (!allowExternalization)
            return;
        bool isVFX = assetPath.EndsWith(VisualEffectResource.Extension);
        if (isVFX)
        {
            string vfxName = Path.GetFileNameWithoutExtension(assetPath);
            string vfxDirectory = Path.GetDirectoryName(assetPath);

            string shaderDirectory = vfxDirectory + "/" + k_ShaderDirectory + "/" + vfxName;

            if (!Directory.Exists(shaderDirectory))
            {
                return;
            }
            VisualEffectAsset asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(assetPath);
            if (asset == null)
                return;

            bool oneFound = false;
            VisualEffectResource resource = asset.GetResource();
            if (resource == null)
                return;
            VFXShaderSourceDesc[] descs = resource.shaderSources;

            foreach (var shaderPath in Directory.GetFiles(shaderDirectory))
            {
                if (shaderPath.EndsWith(k_ShaderExt))
                {
                    System.IO.StreamReader file = new System.IO.StreamReader(shaderPath);

                    string shaderLine = file.ReadLine();
                    file.Close();
                    if (shaderLine == null || !shaderLine.StartsWith("//"))
                        continue;

                    string[] shaderParams = shaderLine.Split(',');

                    string shaderName = shaderParams[0].Substring(2);

                    int index;
                    if (!int.TryParse(shaderParams[1], out index))
                        continue;

                    if (index < 0 || index >= descs.Length)
                        continue;
                    if (descs[index].name != shaderName)
                        continue;

                    string shaderSource = File.ReadAllText(shaderPath);
                    //remove the first two lines that where added when externalized
                    shaderSource = shaderSource.Substring(shaderSource.IndexOf("\n", shaderSource.IndexOf("\n") + 1) + 1);

                    descs[index].source = shaderSource;
                    oneFound = true;
                }
            }
            if (oneFound)
            {
                resource.shaderSources = descs;
            }
        }
    }


    static Dictionary<string,HashSet<string>> dependantAssetCache;


    static public void RecompileDependencies(VisualEffectObject visualEffectObject)
    {
        if( dependantAssetCache == null)
        {
            dependantAssetCache = new Dictionary<string, HashSet<string>>();
            //building cache
            foreach (var graph in VFXGraph.GetAllGraphs<VisualEffectAsset>())
            {
                if( graph != null)
                {
                    foreach(var dep in graph.subgraphDependencies)
                    {
                        HashSet<string> dependencyList;

                        string dependantGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(dep));
                        if( ! dependantAssetCache.TryGetValue(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(dep)), out dependencyList))
                        {
                            dependencyList = new HashSet<string>();
                            dependantAssetCache[dependantGUID] = dependencyList;
                        }
                        dependencyList.Add(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(graph)));
                    }
                }
            }
        }


        {
            HashSet<string> dependencyList;
            if( dependantAssetCache.TryGetValue(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(visualEffectObject)),out dependencyList))
            {
                foreach(var dep in dependencyList.ToList())
                {
                    var resource = VisualEffectResource.GetResourceAtPath(AssetDatabase.GUIDToAssetPath(dep));
                    if( resource != null)
                    {
                        resource.GetOrCreateGraph().SubgraphDirty(visualEffectObject);
                    }
                }
            }
        }
    }

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (var assetPath in deletedAssets)
        {
            if (VisualEffectAssetModicationProcessor.HasVFXExtension(assetPath))
            {
                if( dependantAssetCache != null)
                {
                    string guid = AssetDatabase.AssetPathToGUID(assetPath);
                    dependantAssetCache.Remove(guid);
                    foreach(var deps in dependantAssetCache.Values)
                    {
                        deps.Remove(guid);
                    }
                }
                VisualEffectResource.DeleteAtPath(assetPath);
            }
        }
        if( dependantAssetCache != null)
        {
            foreach (string assetPath in importedAssets)
            {
                if (assetPath.EndsWith(VisualEffectResource.Extension))
                {
                    string assetGUID = AssetDatabase.AssetPathToGUID(assetPath);

                    foreach (var list in dependantAssetCache.Values)
                        if( list.Contains(assetGUID))
                            list.Remove(assetGUID);

                    VisualEffectResource resource = VisualEffectResource.GetResourceAtPath(assetPath);
                    if( resource != null)
                    {
                        foreach(var dep in resource.GetOrCreateGraph().subgraphDependencies)
                        {
                            HashSet<string> dependencyList;

                            string dependantGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(dep));
                            if( ! dependantAssetCache.TryGetValue(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(dep)), out dependencyList))
                            {
                                dependencyList = new HashSet<string>();
                                dependantAssetCache[dependantGUID] = dependencyList;
                            }
                            dependencyList.Add(assetGUID);
                        }
                    }
                        
                }
            }
        }

        if (!allowExternalization)
            return;
        HashSet<string> vfxToRefresh = new HashSet<string>();
        HashSet<string> vfxToRecompile = new HashSet<string>(); // Recompile vfx if a shader is deleted to replace
        foreach (string assetPath in importedAssets.Concat(deletedAssets).Concat(movedAssets))
        {
            if (assetPath.EndsWith(k_ShaderExt))
            {
                string shaderDirectory = Path.GetDirectoryName(assetPath);
                string vfxName = Path.GetFileName(shaderDirectory);
                string vfxPath = Path.GetDirectoryName(shaderDirectory);

                if (Path.GetFileName(vfxPath) != k_ShaderDirectory)
                    continue;

                vfxPath = Path.GetDirectoryName(vfxPath) + "/" + vfxName + VisualEffectResource.Extension;

                if (deletedAssets.Contains(assetPath))
                    vfxToRecompile.Add(vfxPath);
                else
                    vfxToRefresh.Add(vfxPath);
            }
        }

        foreach (var assetPath in vfxToRecompile)
        {
            VisualEffectAsset asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(assetPath);
            if (asset == null)
                continue;

            // Force Recompilation to restore the previous shaders
            VisualEffectResource resource = asset.GetResource();
            if (resource == null)
                continue;
            resource.GetOrCreateGraph().SetExpressionGraphDirty();
            resource.GetOrCreateGraph().RecompileIfNeeded(false,true);
        }

        foreach (var assetPath in vfxToRefresh)
        {
            VisualEffectAsset asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(assetPath);
            if (asset == null)
                return;
            AssetDatabase.ImportAsset(assetPath);
        }
    }
}
