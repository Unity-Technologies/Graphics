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
using System.Linq;

namespace UnityEditor.ShaderGraph
{
    [CustomEditor(typeof(ShaderGraphImporter))]
    class ShaderGraphImporterEditor : ScriptedImporterEditor
    {
        protected override bool needsApplyRevert => false;

        bool checkedAssetOutOfDate = false;
        bool assetIsOutOfDate = false;

        bool CheckIsAssetOutOfDate(AssetImporter importer)
        {
            // already checked
            if (checkedAssetOutOfDate)
                return assetIsOutOfDate;

            // check it -- load graph and compare serialized
            string originalJSon = null;
            var graphData = GetGraphData(importer.assetPath, text => { originalJSon = text; });

            var newJSon = MultiJson.Serialize(graphData);

            assetIsOutOfDate = !string.Equals(originalJSon, newJSon, StringComparison.Ordinal);
            checkedAssetOutOfDate = true;

            return assetIsOutOfDate;
        }

        static GraphData GetGraphData(string shaderGraphPath, Action<string> onOriginalText = null)
        {
            var textGraph = File.ReadAllText(shaderGraphPath, Encoding.UTF8);
            if (onOriginalText != null)
                onOriginalText(textGraph);
            var graphObject = CreateInstance<GraphObject>();
            graphObject.hideFlags = HideFlags.HideAndDontSave;
            bool isSubGraph;
            var extension = Path.GetExtension(shaderGraphPath).Replace(".", "");
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
            var assetGuid = AssetDatabase.AssetPathToGUID(shaderGraphPath);
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

        public override void OnInspectorGUI()
        {
            AssetImporter importer = target as AssetImporter;

            if (GUILayout.Button("Open Shader Editor"))
            {
                Debug.Assert(importer != null, "importer != null");
                ShowGraphEditWindow(importer.assetPath);
            }
            using (var horizontalScope = new GUILayout.HorizontalScope("box"))
            {
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
                    var graphData = GetGraphData(importer.assetPath);
                    var generator = new Generator(graphData, null, GenerationMode.ForReals, assetName, null);
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
                    string assetName = Path.GetFileNameWithoutExtension(importer.assetPath);
                    string path = String.Format("Temp/GeneratedFromGraph-{0}-Preview.shader", assetName.Replace(" ", ""));

                    var graphData = GetGraphData(importer.assetPath);
                    var generator = new Generator(graphData, null, GenerationMode.Preview, $"{assetName}-Preview", null);
                    if (GraphUtil.WriteToFile(path, generator.generatedShader))
                        GraphUtil.OpenFile(path);
                }
            }
            if (GUILayout.Button("Copy Shader"))
            {               
                string assetName = Path.GetFileNameWithoutExtension(importer.assetPath);

                var graphData = GetGraphData(importer.assetPath);
                var generator = new Generator(graphData, null, GenerationMode.ForReals, assetName, null);
                GUIUtility.systemCopyBuffer = generator.generatedShader;
            }
            if (CheckIsAssetOutOfDate(importer))
            {
                using (var horizontalScope = new GUILayout.HorizontalScope("box"))
                {
                    if (GUILayout.Button("Update File To Latest"))
                    {
                        if (EditorUtility.DisplayDialog("Update ShaderGraph File",
                            "This will update the ShaderGraph file\n" +
                            "by loading and saving it.  This applies any version\n" +
                            "or file format changes to the file on disk.\n\n" +
                            "Please backup the file before running this process!\n",
                            "Update File", "Cancel"))
                        {
                            UpdateShaderGraph(importer.assetPath, true);
                            checkedAssetOutOfDate = false;
                        }
                    }
                    if (GUILayout.Button("Update All ShaderGraphs"))
                    {
                        if (EditorUtility.DisplayDialog("Update All ShaderGraph Files",
                            "This will update ALL ShaderGraph files in your project,\n" +
                            "by loading and saving them.  This applies any version\n" +
                            "or file format changes to the files on disk.\n\n" +
                            "Please backup your project before running this process!\n",
                            "Update All", "Cancel"))
                        {
                            try
                            {
                                AssetDatabase.StartAssetEditing();
                                DirectoryInfo directory = new DirectoryInfo(Application.dataPath);
                                FileInfo[] fileInfos = directory.GetFiles("*.shadergraph", SearchOption.AllDirectories);

                                int fileIdx = 0;
                                int totalFiles = fileInfos.Length;
                                foreach (var file in fileInfos)
                                {
                                    fileIdx++;
                                    var path = file.FullName;
                                    path = path.Replace(@"\", "/").Replace(Application.dataPath, "Assets");
                                    EditorUtility.DisplayProgressBar("ShaderGraph File Updater", string.Format("({0} of {1}) {2}", fileIdx, totalFiles, path), (float)fileIdx / (float)totalFiles);
                                    UpdateShaderGraph(path, false);
                                }
                            }
                            finally
                            {
                                AssetDatabase.StopAssetEditing();
                                AssetDatabase.Refresh();
                                EditorUtility.ClearProgressBar();
                                checkedAssetOutOfDate = false;
                            }
                        }
                    }
                }
            }

            ApplyRevertGUI();
        }

        internal static void UpdateShaderGraph(string shaderGraphPath, bool import)
        {
            var graphData = GetGraphData(shaderGraphPath);
            if (graphData != null)
            {
                AssetDatabase.MakeEditable(shaderGraphPath);

                // TODO check out file!
                var newFileContents = FileUtilities.WriteShaderGraphToDisk(shaderGraphPath, graphData);
                if ((newFileContents != null) && import)
                {
                    AssetDatabase.ImportAsset(shaderGraphPath);
                }
            }
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
