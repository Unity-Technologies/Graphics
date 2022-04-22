using System;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.ImportedGraph
{
    [ScriptedImporter(1, GraphWrapper.assetExtension)]
    public class GraphWrapperImporter : ScriptedImporter
    {
        /// <inheritdoc />
        public override void OnImportAsset(AssetImportContext ctx)
        {
            // The file contains some JSON data representing our assets.
            var fileContent = File.ReadAllText(ctx.assetPath);

            // GTF graph asset does not have to be the main asset.
            // We demonstrate this by importing some other asset and
            // setting it as the main asset.
            var grrObject = ScriptableObject.CreateInstance<GraphWrapper>();
            JsonUtility.FromJsonOverwrite(fileContent, grrObject);
            ctx.AddObjectToAsset("some other asset", grrObject);
            ctx.SetMainObject(grrObject);

            // The main asset has a field (GraphAsset) that
            // holds a JSON representation of the graph asset.
            var graphAsset = ScriptableObject.CreateInstance<ImportedGraphAsset>();
            JsonUtility.FromJsonOverwrite(grrObject.GraphAsset, graphAsset);
            graphAsset.name = Path.GetFileNameWithoutExtension(assetPath) + " Graph";
            ctx.AddObjectToAsset("imported graph", graphAsset);
        }
    }
}
