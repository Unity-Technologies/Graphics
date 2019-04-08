using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.VFX;
using UnityEditor.VFX;


public class LWRP_VFX_PostProcessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        var commandLineArgs = System.Environment.GetCommandLineArgs();
        if (commandLineArgs != null && !commandLineArgs.Contains("-runTests"))
            return;

        var vfxAssets = importedAssets.Where(o => o.EndsWith(".vfx"));
        foreach (var vfxPath in vfxAssets)
        {
            var resource = VisualEffectResource.GetResourceAtPath(vfxPath);
            if (resource)
            {
                VFXShaderSourceDesc[] shaderSources = resource.shaderSources;
                foreach (var shaderSource in shaderSources)
                {
                    var source = shaderSource.source;
                    if (source.Contains("/RenderPipeline/HDRP"))
                    {
                        try
                        {
                            //This asset has been compiled with old vfx
                            var vfxGraph = resource.GetOrCreateGraph();
                            if (vfxGraph)
                            {
                                Debug.Log("VFXGraph automatically migrated : " + vfxPath);
                                vfxGraph.SetExpressionGraphDirty();
                                vfxGraph.RecompileIfNeeded();
                                EditorUtility.SetDirty(vfxGraph);
                            }
                        }
                        catch (System.Exception exp)
                        {
                            Debug.Log("VFXGraph automatically migrated has failed : " + vfxPath + exp.ToString());
                        }
                        break;
                    }
                }
            }
        }
    }
}
