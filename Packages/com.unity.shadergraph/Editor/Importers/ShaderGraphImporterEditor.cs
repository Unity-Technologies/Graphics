using System;
using System.Collections.Generic;
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
        MaterialEditor materialEditor = null;

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

                var pathList = new List<string>();
                pathList.Add(path);

                if (update)
                {
                    var graphData = GetGraphData(importer);
                    var generator = new Generator(graphData, null, GenerationMode.ForReals, assetName, humanReadable: true);
                    if (!GraphUtil.WriteToFile(path, generator.generatedShader))
                        open = false;
                    var generatedShaderCount = 0;
                    foreach (var generatedShader in generator.allGeneratedShaders)
                    {
                        if (generatedShaderCount > 0)
                        {
                            string pathSub = String.Format("Temp/GeneratedFromGraph-{0}_{1}.shader", assetName.Replace(" ", ""), generatedShaderCount);
                            pathList.Add(pathSub);
                            GraphUtil.WriteToFile(pathSub, generatedShader.codeString);
                        }
                        generatedShaderCount++;
                    }
                }

                if (open)
                {
                    for (int i = 1; i < pathList.Count; ++i)
                        GraphUtil.OpenFile(pathList[i]);
                    GraphUtil.OpenFile(pathList[0]);
                }
            }
            if (Unsupported.IsDeveloperMode())
            {
                if (GUILayout.Button("View Preview Shader"))
                {
                    AssetImporter importer = target as AssetImporter;
                    string assetName = Path.GetFileNameWithoutExtension(importer.assetPath);
                    string path = String.Format("Temp/GeneratedFromGraph-{0}-Preview.shader", assetName.Replace(" ", ""));

                    var graphData = GetGraphData(importer);
                    var generator = new Generator(graphData, null, GenerationMode.Preview, $"{assetName}-Preview", humanReadable: true);
                    if (GraphUtil.WriteToFile(path, generator.generatedShader))
                        GraphUtil.OpenFile(path);
                }
            }
            if (GUILayout.Button("Copy Shader"))
            {
                AssetImporter importer = target as AssetImporter;
                string assetName = Path.GetFileNameWithoutExtension(importer.assetPath);

                var graphData = GetGraphData(importer);
                var generator = new Generator(graphData, null, GenerationMode.ForReals, assetName, humanReadable: true);
                GUIUtility.systemCopyBuffer = generator.generatedShader;
            }

            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(ShaderGraphImporter.UseAsTemplateFieldName));
            bool needsReimport = EditorGUI.EndChangeCheck();
            using (new EditorGUI.IndentLevelScope(1))
            using (new EditorGUI.DisabledScope(!(target as ShaderGraphImporter)?.UseAsTemplate ?? true))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serializedObject.FindProperty(ShaderGraphImporter.ExposeTemplateAsShaderFieldName), new GUIContent("Expose as Shader", "Toggle whether or not the template shader should be exposed in shader dropdowns."));
                needsReimport |= EditorGUI.EndChangeCheck();

                EditorGUILayout.PropertyField(serializedObject.FindProperty(ShaderGraphImporter.TemplateFieldName));
            }

            ApplyRevertGUI();
            if (needsReimport)
            {
                AssetImporter importer = target as AssetImporter;
                AssetDatabase.ImportAsset(importer.assetPath);
            }

            if (materialEditor)
            {
                EditorGUILayout.Space();
                materialEditor.DrawHeader();
                using (new EditorGUI.DisabledGroupScope(true))
                    materialEditor.OnInspectorGUI();
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            AssetImporter importer = target as AssetImporter;
            var material = AssetDatabase.LoadAssetAtPath<Material>(importer.assetPath);
            if (material)
                materialEditor = (MaterialEditor)CreateEditor(material);
        }

        public override void OnDisable()
        {
            base.OnDisable();
            if (materialEditor != null)
                DestroyImmediate(materialEditor);
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
                    if (w.m_Parent != null)
                    {
                        w.Focus();
                        return true;
                    }
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
            var path = AssetDatabase.GetAssetPath((EntityId)instanceID);
            return ShowGraphEditWindow(path);
        }
    }
}
