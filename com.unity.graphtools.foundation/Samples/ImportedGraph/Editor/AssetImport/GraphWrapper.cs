using System;
using System.IO;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.ImportedGraph
{
    [Serializable]
    class GraphWrapper : ScriptableObject
    {
        public const string promptToCreateTitle = "Create a GraphWrapper File";
        public const string promptToCreate = "Create a new importable graph file";
        public const string assetExtension = "grr";

        [field: SerializeField]
        public string GraphAsset { get; set; }

        /// <summary>
        /// Creates a ".grr" file that contains a JSON-serialized graph asset.
        /// </summary>
        [MenuItem("Assets/Create/GTF Samples/Importable Graph")]
        static void CreateGrrFile()
        {
            const string path = "Assets";
            var template = new GraphTemplate<ImportedGraphStencil>(ImportedGraphStencil.graphName, assetExtension);

            GraphAssetCreationHelpers.CreateInProjectWindow<ImportedGraphAsset>(template, null, path,
                () => GraphViewEditorWindow.FindOrCreateGraphWindow<ImportedGraphWindow>());
        }

        public void Export(ImportedGraphAsset graphAsset, string path)
        {
            if (string.IsNullOrEmpty(path) || graphAsset == null)
                return;

            GraphAsset = JsonUtility.ToJson(graphAsset);
            var serializedGrr = JsonUtility.ToJson(this);
            File.WriteAllText(path, serializedGrr);
        }
    }
}
