using System;
using System.IO;
using UnityEditor.Callbacks;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEditor.Graphing;
using System.Text;
using UnityEditor.ShaderGraph.Serialization;

namespace UnityEditor.ShaderGraph
{
    [CustomEditor(typeof(ShaderGraphImporter))]
    class ShaderGraphImporterEditor : ScriptedImporterEditor
    {
        protected override bool needsApplyRevert => false;

        public override void OnInspectorGUI()
        {
            GraphData GetGraphData(AssetImporter importer)
            {
                var textGraph = File.ReadAllText(importer.assetPath, Encoding.UTF8);
                var graphObject = CreateInstance<GraphObject>();
                graphObject.hideFlags = HideFlags.HideAndDontSave;
                bool isSubGraph;
                var extension = Path.GetExtension(importer.assetPath).Replace(".", "");
                switch (extension)
                {
                    case ShaderGraphImporter.Extension:
                        isSubGraph = false;
                        break;
                    case ShaderGraphImporter.LegacyExtension:
                        isSubGraph = false;
                        break;
                    case ShaderSubGraphImporter.Extension:
                        isSubGraph = true;
                        break;
                    default:
                        throw new Exception($"Invalid file extension {extension}");
                }
                var assetGuid = AssetDatabase.AssetPathToGUID(importer.assetPath);
                graphObject.graph = new GraphData
                {
                    assetGuid = assetGuid,
                    isSubGraph = isSubGraph,
                    messageManager = null
                };
                MultiJson.Deserialize(graphObject.graph, textGraph);
                graphObject.graph.OnEnable();
                graphObject.graph.ValidateGraph();
                return graphObject.graph;
            }

            if (GUILayout.Button("Open Shader Editor"))
            {
                AssetImporter importer = target as AssetImporter;
                Debug.Assert(importer != null, "importer != null");
                ShowGraphEditWindow(importer.assetPath);
            }
            using (var horizontalScope = new GUILayout.HorizontalScope("box"))
            {
                AssetImporter importer = target as AssetImporter;
                string assetName = Path.GetFileNameWithoutExtension(importer.assetPath);
                string path = String.Format("Temp/GeneratedFromGraph-{0}.shader", assetName.Replace(" ", ""));
                bool alreadyExists = File.Exists(path);
                bool update = false;
                bool open = false;

                if (GUILayout.Button("View Generated Shader"))
                {
                    update = true;
                    open = true;
                }

                if (alreadyExists && GUILayout.Button("Regenerate"))
                    update = true;

                if (update)
                {
                    var graphData = GetGraphData(importer);
                    var generator = new Generator(graphData, null, GenerationMode.ForReals, assetName, null, true);
                    if (!GraphUtil.WriteToFile(path, generator.generatedShader))
                        open = false;
                }

                if (open)
                    GraphUtil.OpenFile(path);
            }
            if (Unsupported.IsDeveloperMode())
            {
                if (GUILayout.Button("View Preview Shader"))
                {
                    AssetImporter importer = target as AssetImporter;
                    string assetName = Path.GetFileNameWithoutExtension(importer.assetPath);
                    string path = String.Format("Temp/GeneratedFromGraph-{0}-Preview.shader", assetName.Replace(" ", ""));

                    var graphData = GetGraphData(importer);
                    var generator = new Generator(graphData, null, GenerationMode.Preview, $"{assetName}-Preview", null, true);
                    if (GraphUtil.WriteToFile(path, generator.generatedShader))
                        GraphUtil.OpenFile(path);
                }
            }
            if (GUILayout.Button("Copy Shader"))
            {
                AssetImporter importer = target as AssetImporter;
                string assetName = Path.GetFileNameWithoutExtension(importer.assetPath);

                var graphData = GetGraphData(importer);
                var generator = new Generator(graphData, null, GenerationMode.ForReals, assetName, null, true);
                GUIUtility.systemCopyBuffer = generator.generatedShader;
            }

            ApplyRevertGUI();
        }

        internal static bool ShowGraphEditWindow(string path)
        {
            var guid = AssetDatabase.AssetPathToGUID(path);
            var extension = Path.GetExtension(path);
            if (string.IsNullOrEmpty(extension))
                return false;
            // Path.GetExtension returns the extension prefixed with ".", so we remove it. We force lower case such that
            // the comparison will be case-insensitive.
            extension = extension.Substring(1).ToLowerInvariant();
            if (extension != ShaderGraphImporter.Extension && extension != ShaderSubGraphImporter.Extension)
                return false;

            foreach (var w in Resources.FindObjectsOfTypeAll<MaterialGraphEditWindow>())
            {
                if (w.selectedGuid == guid)
                {
                    w.Focus();
                    return true;
                }
            }

            var window = EditorWindow.CreateWindow<MaterialGraphEditWindow>(typeof(MaterialGraphEditWindow), typeof(SceneView));
            window.Initialize(guid);
            window.Focus();
            return true;
        }

        [OnOpenAsset(0)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var path = AssetDatabase.GetAssetPath(instanceID);
            return ShowGraphEditWindow(path);
        }
    }
}
