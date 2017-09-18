﻿using System;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
 using UnityEngine.Experimental.UIElements.StyleEnums;
 using UnityEngine.Experimental.UIElements.StyleSheets;
 using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using Object = UnityEngine.Object;

namespace UnityEditor.MaterialGraph.Drawing.Inspector
{
    public class GraphInspectorView : VisualElement, IDisposable
    {
        [SerializeField]
        GraphInspectorPresenter m_Presenter;

        int m_SelectionHash;

        VisualElement m_Title;
        VisualElement m_PropertiesContainer;
        VisualElement m_ContentContainer;
        AbstractNodeEditorView m_EditorView;

        TypeMapper m_TypeMapper;
        Image m_Preview;

        public GraphInspectorView()
        {
            AddStyleSheetPath("Styles/MaterialGraph");

            var topContainer = new VisualElement { name = "top" };
            {
                var headerContainer = new VisualElement { name = "header" };
                {
                    m_Title = new VisualElement() { name = "title" };
                    headerContainer.Add(m_Title);
                }
                topContainer.Add(headerContainer);

                m_ContentContainer = new VisualElement { name = "content" };
                topContainer.Add(m_ContentContainer);
            }
            Add(topContainer);

            ReaddProps();

            var bottomContainer = new VisualElement { name = "bottom" };
            {
                m_Preview = new Image { name = "preview", image = Texture2D.blackTexture};
                bottomContainer.Add(m_Preview);
            }
            Add(bottomContainer);

            // Nodes missing custom editors:
            // - PropertyNode
            // - SubGraphInputNode
            // - SubGraphOutputNode
            m_TypeMapper = new TypeMapper(typeof(INode), typeof(AbstractNodeEditorView), typeof(StandardNodeEditorView))
            {
                  // { typeof(AbstractSurfaceMasterNode), typeof(SurfaceMasterNodeEditorView) }
            };
        }

        private void ReaddProps()
        {
            if (m_PropertiesContainer != null)
                Remove(m_PropertiesContainer);

            m_PropertiesContainer = new VisualElement()
            {
                new IMGUIContainer(OnGuiHandler)
            };

            m_PropertiesContainer.style.flexDirection = StyleValue<FlexDirection>.Create(FlexDirection.Column);
            Add(m_PropertiesContainer);
        }


        private void OnGuiHandler()
        {
            if (m_Presenter == null)
                return;

            if (GUILayout.Button("Add Property"))
            {
                var gm = new GenericMenu();
                gm.AddItem(new GUIContent("Float"), false, () =>
                {
                    m_Presenter.graph.AddShaderProperty(new FloatShaderProperty());
                    ReaddProps();

                });
                gm.AddItem(new GUIContent("Vector2"), false, () =>
                {
                    m_Presenter.graph.AddShaderProperty(new Vector2ShaderProperty());

                    ReaddProps();

                });
                gm.AddItem(new GUIContent("Vector3"), false, () =>
                {
                    m_Presenter.graph.AddShaderProperty(new Vector3ShaderProperty());
                    ReaddProps();

                });
                gm.AddItem(new GUIContent("Vector4"), false, () =>
                {
                    m_Presenter.graph.AddShaderProperty(new Vector4ShaderProperty());
                    ReaddProps();

                });
                gm.AddItem(new GUIContent("Color"), false, () =>
                {
                    m_Presenter.graph.AddShaderProperty(new ColorShaderProperty());
                    ReaddProps();

                });
                gm.AddItem(new GUIContent("Texture"), false, () =>
                {
                    m_Presenter.graph.AddShaderProperty(new TextureShaderProperty());
                    ReaddProps();

                });
                gm.ShowAsContext();
            }

            EditorGUI.BeginChangeCheck();
            foreach (var property in m_Presenter.graph.properties.ToArray())
            {
                property.name = EditorGUILayout.DelayedTextField("Name", property.name);
                property.description = EditorGUILayout.DelayedTextField("Description", property.description);

                if (property is FloatShaderProperty)
                {
                    var fProp = property as FloatShaderProperty;
                    fProp.value = EditorGUILayout.FloatField("Value", fProp.value);
                }
                else if (property is Vector2ShaderProperty)
                {
                    var fProp = property as Vector2ShaderProperty;
                    fProp.value = EditorGUILayout.Vector2Field("Value", fProp.value);
                }
                else if (property is Vector3ShaderProperty)
                {
                    var fProp = property as Vector3ShaderProperty;
                    fProp.value = EditorGUILayout.Vector3Field("Value", fProp.value);
                }
                else if (property is Vector4ShaderProperty)
                {
                    var fProp = property as Vector4ShaderProperty;
                    fProp.value = EditorGUILayout.Vector4Field("Value", fProp.value);
                }
                else if (property is ColorShaderProperty)
                {
                    var fProp = property as ColorShaderProperty;
                    fProp.value = EditorGUILayout.ColorField("Value", fProp.value);
                }
                else if (property is TextureShaderProperty)
                {
                    var fProp = property as TextureShaderProperty;
                    fProp.value.texture = EditorGUILayout.MiniThumbnailObjectField(new GUIContent("Texture"), fProp.value.texture, typeof(Texture), null) as Texture;
                }

                if (GUILayout.Button("Remove"))
                {
                    m_Presenter.graph.RemoveShaderProperty(property.guid);
                }
                EditorGUILayout.Separator();
            }
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var node in m_Presenter.graph.GetNodes<PropertyNode>())
                    node.onModified(node, ModificationScope.Node);
            }
        }

        public void OnChange(GraphInspectorPresenter.ChangeType changeType)
        {
            if (presenter == null)
            {
                m_ContentContainer.Clear();
                m_SelectionHash = 0;
                return;
            }

            if ((changeType & GraphInspectorPresenter.ChangeType.AssetName) != 0)
                m_Title.text = presenter.assetName;

            if ((changeType & GraphInspectorPresenter.ChangeType.SelectedNodes) != 0)
            {
                var selectionHash = UIUtilities.GetHashCode(presenter.selectedNodes.Count, presenter.selectedNodes != null ? presenter.selectedNodes.FirstOrDefault() : null);
                if (selectionHash != m_SelectionHash)
                {
                    m_SelectionHash = selectionHash;
                    m_ContentContainer.Clear();
                    if (presenter.selectedNodes.Count > 1)
                    {
                        var element = new VisualElement { name = "selectionCount", text = string.Format("{0} nodes selected.", presenter.selectedNodes.Count) };
                        m_ContentContainer.Add(element);
                    }
                    else if (presenter.selectedNodes.Count == 1)
                    {
                        var node = presenter.selectedNodes.First();
                        var view = (AbstractNodeEditorView)Activator.CreateInstance(m_TypeMapper.MapType(node.GetType()));
                        view.node = node;
                        m_ContentContainer.Add(view);
                    }
                }
            }

            if ((changeType & GraphInspectorPresenter.ChangeType.PreviewTexture) != 0)
            {
                m_Preview.image = presenter.previewTexture ?? Texture2D.blackTexture;
            }
        }

        public GraphInspectorPresenter presenter
        {
            get { return m_Presenter; }
            set
            {
                if (m_Presenter == value)
                    return;
                if (m_Presenter != null)
                    m_Presenter.onChange -= OnChange;
                m_Presenter = value;
                OnChange(GraphInspectorPresenter.ChangeType.All);
                m_Presenter.onChange += OnChange;
            }
        }

        public void Dispose()
        {
            if (m_Presenter != null)
                m_Presenter.onChange -= OnChange;
        }
    }
}
