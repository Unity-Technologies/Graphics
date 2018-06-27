using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [ScriptedImporter(1, "ies")]
    public class IesScriptedImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            string[] lines = File.ReadAllLines(ctx.assetPath);
            
        }
    }
}