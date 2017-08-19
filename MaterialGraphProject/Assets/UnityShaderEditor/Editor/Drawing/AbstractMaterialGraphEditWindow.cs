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
    public interface IMaterialGraphEditWindow
    {
        void PingAsset();

        void UpdateAsset();

        void Repaint();

        void ToggleRequiresTime();
    }

    public class MaterialGraphEditWindow : AbstractMaterialGraphEditWindow<UnityEngine.MaterialGraph.MaterialGraph>
    { }
    public class SubGraphEditWindow : AbstractMaterialGraphEditWindow<SubGraph>
    { }

    public abstract class AbstractMaterialGraphEditWindow<TGraphType> : EditorWindow, IMaterialGraphEditWindow where TGraphType : AbstractMaterialGraph
    {
        public static bool allowAlwaysRepaint = true;

        [SerializeField]
        Object m_Selected;

        [SerializeField]
        TGraphType m_InMemoryAsset;

        GraphEditorView m_GraphEditorView;

        public TGraphType inMemoryAsset
        {
            get { return m_InMemoryAsset; }
            set { m_InMemoryAsset = value; }
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

                if (typeof(TGraphType) == typeof(UnityEngine.MaterialGraph.MaterialGraph))
                    UpdateShaderGraphOnDisk(path);

                if (typeof(TGraphType) == typeof(SubGraph))
                    UpdateShaderSubGraphOnDisk(path);
            }
        }

        private void UpdateShaderSubGraphOnDisk(string path)
        {
            var graph = inMemoryAsset as SubGraph;
            if (graph == null)
                return;

            File.WriteAllText(path, EditorJsonUtility.ToJson(inMemoryAsset));
        }

        private void UpdateShaderGraphOnDisk(string path)
        {
            var graph = inMemoryAsset as UnityEngine.MaterialGraph.MaterialGraph;
            if (graph == null)
                return;

            var masterNode = graph.masterNode;
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
            File.WriteAllText(path, EditorJsonUtility.ToJson(inMemoryAsset));
            shaderImporter.SaveAndReimport();
            AssetDatabase.ImportAsset(path);
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
            
            var path = AssetDatabase.GetAssetPath(newSelection);
            var textGraph = File.ReadAllText(path, Encoding.UTF8);
            inMemoryAsset = JsonUtility.FromJson<TGraphType>(textGraph);
            inMemoryAsset.OnEnable();
            inMemoryAsset.ValidateGraph();

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
    }
}
