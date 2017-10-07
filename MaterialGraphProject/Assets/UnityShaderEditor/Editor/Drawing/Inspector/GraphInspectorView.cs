﻿using System;
using System.Linq;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
 using UnityEngine.Experimental.UIElements.StyleEnums;
 using UnityEngine.Experimental.UIElements.StyleSheets;
 using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing.Inspector
{
    public class GraphInspectorView : VisualElement, IDisposable
    {
        [SerializeField]
        GraphInspectorPresenter m_Presenter;

        int m_SelectionHash;

        VisualElement m_Title;
        VisualElement m_PropertyItems;
        VisualElement m_ContentContainer;
        AbstractNodeEditorView m_EditorView;

        TypeMapper m_TypeMapper;
        Image m_Preview;
        VisualElement m_TopContainer;

        public GraphInspectorView()
        {
            AddStyleSheetPath("Styles/MaterialGraph");

            m_TopContainer = new VisualElement { name = "top" };
            {
                var headerContainer = new VisualElement { name = "header" };
                {
                    m_Title = new VisualElement() { name = "title" };
                    headerContainer.Add(m_Title);
                }
                m_TopContainer.Add(headerContainer);

                m_ContentContainer = new VisualElement { name = "content" };
                m_TopContainer.Add(m_ContentContainer);
            }
            Add(m_TopContainer);

            var bottomContainer = new VisualElement { name = "bottom" };
            {
                var propertiesContainer = new VisualElement { name = "properties" };
                {
                    var header = new VisualElement { name = "header" };
                    {
                        var title = new VisualElement { name = "title", text = "Properties" };
                        header.Add(title);

                        var addPropertyButton = new Button(OnAddProperty) { text = "Add", name = "addButton" };
                        header.Add(addPropertyButton);
                    }
                    propertiesContainer.Add(header);

                    m_PropertyItems = new VisualContainer { name = "items" };
                    propertiesContainer.Add(m_PropertyItems);
                }
                bottomContainer.Add(propertiesContainer);

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

        void OnAddProperty()
        {
            var gm = new GenericMenu();
            gm.AddItem(new GUIContent("Float"), false, () => m_Presenter.graph.AddShaderProperty(new FloatShaderProperty()));
            gm.AddItem(new GUIContent("Vector2"), false, () => m_Presenter.graph.AddShaderProperty(new Vector2ShaderProperty()));
            gm.AddItem(new GUIContent("Vector3"), false, () => m_Presenter.graph.AddShaderProperty(new Vector3ShaderProperty()));
            gm.AddItem(new GUIContent("Vector4"), false, () => m_Presenter.graph.AddShaderProperty(new Vector4ShaderProperty()));
            gm.AddItem(new GUIContent("Color"), false, () => m_Presenter.graph.AddShaderProperty(new ColorShaderProperty()));
            gm.AddItem(new GUIContent("Texture"), false, () => m_Presenter.graph.AddShaderProperty(new TextureShaderProperty()));
                gm.ShowAsContext();
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

            if ((changeType & GraphInspectorPresenter.ChangeType.Graph) != 0)
            {
                if (m_Graph != null)
                {
                    m_Graph.onChange -= OnGraphChange;
                    m_PropertyItems.Clear();
                    m_Graph = null;
                }
                if (m_Presenter.graph != null)
                {
                    m_Graph = m_Presenter.graph;
                    foreach (var property in m_Graph.properties)
                        m_PropertyItems.Add(new ShaderPropertyView(m_Graph, property));
                    m_Graph.onChange += OnGraphChange;
                }
        }
        }

        void OnGraphChange(GraphChange change)
        {
            var propertyAdded = change as ShaderPropertyAdded;
            if (propertyAdded != null)
                m_PropertyItems.Add(new ShaderPropertyView(m_Graph, propertyAdded.shaderProperty));

            var propertyRemoved = change as ShaderPropertyRemoved;
            if (propertyRemoved != null)
            {
                var propertyView = m_PropertyItems.OfType<ShaderPropertyView>().FirstOrDefault(v => v.property.guid == propertyRemoved.guid);
                if (propertyView != null)
                    m_PropertyItems.Remove(propertyView);
            }
        }

        AbstractMaterialGraph m_Graph;

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
