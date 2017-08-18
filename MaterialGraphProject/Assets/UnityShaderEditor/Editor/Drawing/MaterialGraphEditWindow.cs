using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using Object = UnityEngine.Object;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class MaterialGraphEditWindow : EditorWindow, ISerializationCallbackReceiver
    {
        public static bool allowAlwaysRepaint = true;

        bool shouldRepaint
        {
            get { return allowAlwaysRepaint && inMemoryAsset != null && inMemoryAsset.shouldRepaint; }
        }

        [SerializeField]
        Object m_Selected;

        [SerializeField]
        MaterialGraphAsset m_InMemoryAsset;

        GraphEditorView m_GraphEditorView;

        public IGraphAsset inMemoryAsset
        {
            get { return m_InMemoryAsset; }
            set { m_InMemoryAsset = value as MaterialGraphAsset; }
        }

        public Object selected
        {
            get { return m_Selected; }
            set { m_Selected = value; }
        }

        public MaterialGraphPresenter CreateDataSource()
        {
            return CreateInstance<MaterialGraphPresenter>();
        }

        public GraphView CreateGraphView()
        {
            return new MaterialGraphView(this);
        }

        void OnEnable()
        {
            m_GraphEditorView = new GraphEditorView(CreateGraphView());
            rootVisualContainer.Add(m_GraphEditorView);
            var source = CreateDataSource();
            source.Initialize(inMemoryAsset, this);
            m_GraphEditorView.presenter = source;
        }

        void OnDisable()
        {
            rootVisualContainer.Clear();
        }

        void Update()
        {
            if (shouldRepaint)
                Repaint();
        }

        void OnGUI()
        {
            var presenter = m_GraphEditorView.presenter;
            var e = Event.current;

            if (e.type == EventType.ValidateCommand && (
                e.commandName == "Copy" && presenter.canCopy
                || e.commandName == "Paste" && presenter.canPaste
                || e.commandName == "Duplicate" && presenter.canDuplicate
                || e.commandName == "Cut" && presenter.canCut
                || (e.commandName == "Delete" || e.commandName == "SoftDelete") && presenter.canDelete))
            {
                e.Use();
            }

            if (e.type == EventType.ExecuteCommand)
            {
                if (e.commandName == "Copy")
                    presenter.Copy();
                if (e.commandName == "Paste")
                    presenter.Paste();
                if (e.commandName == "Duplicate")
                    presenter.Duplicate();
                if (e.commandName == "Cut")
                    presenter.Cut();
                if (e.commandName == "Delete" || e.commandName == "SoftDelete")
                    presenter.Delete();
            }
        }

        public void PingAsset()
        {
            if (selected != null)
                EditorGUIUtility.PingObject(selected);
        }

        public void UpdateAsset()
        {
            if (selected != null && inMemoryAsset != null)
            {
                var path = AssetDatabase.GetAssetPath(selected);
                if (string.IsNullOrEmpty(path) || inMemoryAsset == null)
                {
                    return;
                }

                var masterNode = ((MaterialGraphAsset)inMemoryAsset).materialGraph.masterNode;
                if (masterNode == null)
                    return;

                List<PropertyGenerator.TextureInfo> configuredTextures;
                masterNode.GetFullShader(GenerationMode.ForReals, "NotNeeded", out configuredTextures);

                var shaderImporter = AssetImporter.GetAtPath(path) as ShaderImporter;
                if (shaderImporter == null)
                    return;

                var textureNames = new List<string>();
                var textures = new List<Texture>();
                foreach (var textureInfo in configuredTextures.Where(
                    x => x.modifiable == TexturePropertyChunk.ModifiableState.Modifiable))
                {
                    var texture = EditorUtility.InstanceIDToObject(textureInfo.textureId) as Texture;
                    if (texture == null)
                        continue;
                    textureNames.Add(textureInfo.name);
                    textures.Add(texture);
                }
                shaderImporter.SetDefaultTextures(textureNames.ToArray(), textures.ToArray());

                textureNames.Clear();
                textures.Clear();
                foreach (var textureInfo in configuredTextures.Where(
                    x => x.modifiable == TexturePropertyChunk.ModifiableState.NonModifiable))
                {
                    var texture = EditorUtility.InstanceIDToObject(textureInfo.textureId) as Texture;
                    if (texture == null)
                        continue;
                    textureNames.Add(textureInfo.name);
                    textures.Add(texture);
                }
                shaderImporter.SetNonModifiableTextures(textureNames.ToArray(), textures.ToArray());
                File.WriteAllText(path, EditorJsonUtility.ToJson(inMemoryAsset.graph));
                shaderImporter.SaveAndReimport();
                AssetDatabase.ImportAsset(path);
            }
        }

        public virtual void ToggleRequiresTime()
        {
            allowAlwaysRepaint = !allowAlwaysRepaint;
        }

        public void ChangeSelction(Object newSelection)
        {
            if (!EditorUtility.IsPersistent(newSelection))
                return;

            if (selected == newSelection)
                return;

            if (selected != null)
            {
                if (EditorUtility.DisplayDialog("Save Old Graph?", "Save Old Graph?", "yes!", "no"))
                {
                    UpdateAsset();
                }
            }

            selected = newSelection;

            var mGraph = CreateInstance<MaterialGraphAsset>();
            var path = AssetDatabase.GetAssetPath(newSelection);
            var textGraph = File.ReadAllText(path, Encoding.UTF8);
            mGraph.materialGraph = JsonUtility.FromJson<UnityEngine.MaterialGraph.MaterialGraph>(textGraph);

            inMemoryAsset = mGraph;
            var graph = inMemoryAsset.graph;
            graph.OnEnable();
            graph.ValidateGraph();

            var source = CreateDataSource();
            source.Initialize(inMemoryAsset, this);
            m_GraphEditorView.presenter = source;

            //m_GraphView.StretchToParentSize();
            Repaint();
            /*if (refocus)
            {
                focused = false;
                m_GraphEditorDrawer.graphView.Schedule (Focus).StartingIn (1).Until (() => focused);
            }*/
        }

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize() { }
    }
}
