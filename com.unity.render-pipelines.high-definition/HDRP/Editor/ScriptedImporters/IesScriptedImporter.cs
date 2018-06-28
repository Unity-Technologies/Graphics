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
            AsciiFileParser parser = new AsciiFileParser(ctx.assetPath);

            var iesAsset = ScriptableObject.CreateInstance< IesAsset >();

            parser.Parse(iesAsset);

            ctx.AddObjectToAsset("IES asset", iesAsset);

            // TODO: generate a cookie from the IES asset and set it as main asset
        }
    }
}