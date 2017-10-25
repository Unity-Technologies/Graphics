using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing.Inspector
{
    public class GraphInspectorView : VisualElement, IDisposable
    {
        int m_SelectionHash;

        VisualElement m_PropertyItems;
        VisualElement m_LayerItems;
        VisualElement m_ContentContainer;
        AbstractNodeEditorView m_EditorView;

        TypeMapper m_TypeMapper;
        PreviewView m_Preview;

        AbstractMaterialGraph m_Graph;
        PreviewSystem m_PreviewSystem;
        MasterNode m_MasterNode;
        PreviewData m_PreviewHandle;

        List<INode> m_SelectedNodes;

        public GraphInspectorView(string assetName, PreviewSystem previewSystem, AbstractMaterialGraph graph)
        {
            m_Graph = graph;
            m_PreviewSystem = previewSystem;
            m_SelectedNodes = new List<INode>();

            AddStyleSheetPath("Styles/MaterialGraph");

            var topContainer = new VisualElement {name = "top"};
            {
                var headerContainer = new VisualElement {name = "header"};
                {
                    headerContainer.Add(new VisualElement {name = "title", text = assetName});
                }
                topContainer.Add(headerContainer);

                m_ContentContainer = new VisualElement {name = "content"};
                topContainer.Add(m_ContentContainer);
            }
            Add(topContainer);

            var bottomContainer = new VisualElement {name = "bottom"};
            {
                var propertiesContainer = new VisualElement {name = "properties"};
                {
                    var header = new VisualElement {name = "header"};
                    {
                        var title = new VisualElement {name = "title", text = "Properties"};
                        header.Add(title);

                        var addPropertyButton = new Button(OnAddProperty) {text = "Add", name = "addButton"};
                        header.Add(addPropertyButton);
                    }
                    propertiesContainer.Add(header);

                    m_PropertyItems = new VisualContainer {name = "items"};
                    propertiesContainer.Add(m_PropertyItems);
                }
                bottomContainer.Add(propertiesContainer);

                //if (m_Presenter.graph is LayeredShaderGraph)
                {
                    var layersContainer = new VisualElement {name = "properties"};
                    {
                        var header = new VisualElement {name = "header"};
                        {
                            var title = new VisualElement {name = "title", text = "Layers"};
                            header.Add(title);

                            var addLayerButton = new Button(OnAddLayer) {text = "Add", name = "addButton"};
                            header.Add(addLayerButton);
                        }
                        propertiesContainer.Add(header);

                        m_LayerItems = new VisualContainer {name = "items"};
                        propertiesContainer.Add(m_LayerItems);
                    }
                    bottomContainer.Add(layersContainer);
                }

                m_Preview = new PreviewView {name = "preview", image = Texture2D.blackTexture};
                bottomContainer.Add(m_Preview);
            }
            Add(bottomContainer);

            masterNode = graph.GetNodes<MasterNode>().FirstOrDefault();

            foreach (var property in m_Graph.properties)
                m_PropertyItems.Add(new ShaderPropertyView(m_Graph, property));
            m_Graph.onChange += OnGraphChange;

            var layerGraph = m_Graph as LayeredShaderGraph;
            if (layerGraph != null)
                foreach (var layer in layerGraph.layers)
                    m_LayerItems.Add(new ShaderLayerView(layerGraph, layer));

            // Nodes missing custom editors:
            // - PropertyNode
            // - SubGraphInputNode
            // - SubGraphOutputNode
            m_TypeMapper = new TypeMapper(typeof(INode), typeof(AbstractNodeEditorView), typeof(StandardNodeEditorView))
            {
                // { typeof(AbstractSurfaceMasterNode), typeof(SurfaceMasterNodeEditorView) }
            };
        }

        MasterNode masterNode
        {
            get { return m_MasterNode; }
            set
            {
                if (value == m_MasterNode)
                    return;
                if (m_MasterNode != null)
                    m_PreviewHandle.onPreviewChanged -= OnPreviewChanged;
                m_PreviewHandle = null;
                m_MasterNode = value;
                if (m_MasterNode != null)
                {
                    m_PreviewHandle = m_PreviewSystem.GetPreview(m_MasterNode);
                    m_PreviewHandle.onPreviewChanged += OnPreviewChanged;
                }
            }
        }

        void OnAddProperty()
        {
            var gm = new GenericMenu();
            gm.AddItem(new GUIContent("Float"), false, () => AddProperty(new FloatShaderProperty()));
            gm.AddItem(new GUIContent("Vector2"), false, () => AddProperty(new Vector2ShaderProperty()));
            gm.AddItem(new GUIContent("Vector3"), false, () => AddProperty(new Vector3ShaderProperty()));
            gm.AddItem(new GUIContent("Vector4"), false, () => AddProperty(new Vector4ShaderProperty()));
            gm.AddItem(new GUIContent("Color"), false, () => AddProperty(new ColorShaderProperty()));
            gm.AddItem(new GUIContent("Texture"), false, () => AddProperty(new TextureShaderProperty()));
            gm.ShowAsContext();
        }

        void OnAddLayer()
        {
            var layerGraph = m_Graph as LayeredShaderGraph;
            if (layerGraph == null)
                return;

            layerGraph.AddLayer();
        }


        void AddProperty(IShaderProperty property)
        {
            m_Graph.owner.RegisterCompleteObjectUndo("Add Property");
            m_Graph.AddShaderProperty(property);
        }

        void OnPreviewChanged()
        {
            m_Preview.image = m_PreviewHandle.texture ?? Texture2D.blackTexture;
        }

        public void UpdateSelection(IEnumerable<INode> nodes)
        {
            m_SelectedNodes.Clear();
            m_SelectedNodes.AddRange(nodes);

            var selectionHash = UIUtilities.GetHashCode(m_SelectedNodes.Count,
                m_SelectedNodes != null ? m_SelectedNodes.FirstOrDefault() : null);
            if (selectionHash != m_SelectionHash)
            {
                m_SelectionHash = selectionHash;
                m_ContentContainer.Clear();
                if (m_SelectedNodes.Count > 1)
                {
                    var element = new VisualElement
                    {
                        name = "selectionCount",
                        text = string.Format("{0} nodes selected.", m_SelectedNodes.Count)
                    };
                    m_ContentContainer.Add(element);
                }
                else if (m_SelectedNodes.Count == 1)
                {
                    var node = m_SelectedNodes.First();
                    var view = (AbstractNodeEditorView) Activator.CreateInstance(m_TypeMapper.MapType(node.GetType()));
                    view.node = node;
                    m_ContentContainer.Add(view);
                }
            }
        }

        void OnGraphChange(GraphChange change)
        {
            var propertyAdded = change as ShaderPropertyAdded;
            if (propertyAdded != null)
            {
                m_PropertyItems.Add(new ShaderPropertyView(m_Graph, propertyAdded.shaderProperty));
                return;
            }

            var propertyRemoved = change as ShaderPropertyRemoved;
            if (propertyRemoved != null)
            {
                var propertyView = m_PropertyItems.OfType<ShaderPropertyView>().FirstOrDefault(v => v.property.guid == propertyRemoved.guid);
                if (propertyView != null)
                    m_PropertyItems.Remove(propertyView);
                return;
            }

            var layerGraph = m_Graph as LayeredShaderGraph;
            if (layerGraph != null)
            {
                var layerAdded = change as LayerAdded;
                if (layerAdded != null)
                    m_LayerItems.Add(new ShaderLayerView(layerGraph, layerAdded.layer));

                var layerRemoved = change as LayerRemoved;
                if (layerRemoved != null)
                {
                    var view = m_LayerItems.OfType<ShaderLayerView>().FirstOrDefault(v => v.layer.guid == layerRemoved.id);
                    if (view != null)
                        m_LayerItems.Remove(view);
                }
            }

            var nodeAdded = change as NodeAddedGraphChange;
            if (nodeAdded != null)
            {
                var node = nodeAdded.node as MasterNode;
                if (node != null && masterNode == null)
                    masterNode = node;
                return;
            }

            var nodeRemoved = change as NodeRemovedGraphChange;
            if (nodeRemoved != null)
            {
                if (nodeRemoved.node == masterNode)
                    masterNode = null;
                return;
            }
        }

        public void Dispose()
        {
            if (m_PreviewHandle != null)
            {
                m_PreviewHandle.onPreviewChanged -= OnPreviewChanged;
                m_PreviewHandle = null;
            }
        }
    }
}
