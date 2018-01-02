using System;
using System.Linq;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph.Drawing.Inspector
{
    public class GraphInspectorView : VisualElement, IDisposable
    {
        int m_SelectionHash;

        VisualElement m_PropertyItems;
        VisualElement m_LayerItems;
        ObjectField m_PreviewMeshPicker;

        PreviewTextureView m_PreviewTextureView;

        AbstractMaterialGraph m_Graph;
        MasterNode m_MasterNode;
        PreviewRenderData m_PreviewRenderHandle;

        Vector2 m_PreviewScrollPosition;

        public Action onUpdateAssetClick { get; set; }
        public Action onShowInProjectClick { get; set; }

        public GraphInspectorView(string assetName, PreviewManager previewManager, AbstractMaterialGraph graph)
        {
            m_Graph = graph;

            AddStyleSheetPath("Styles/MaterialGraph");

            var topContainer = new VisualElement {name = "top"};
            {
                var headerContainer = new VisualElement {name = "header"};
                {
                    var title = new Label(assetName) {name = "title"};
                    title.AddManipulator(new Clickable(() =>
                    {
                        if (onShowInProjectClick != null)
                            onShowInProjectClick();
                    }));
                    headerContainer.Add(title);
                    headerContainer.Add(new Button(() =>
                    {
                        if (onUpdateAssetClick != null)
                            onUpdateAssetClick();
                    }) { name = "save", text = "Save" });
                }
                topContainer.Add(headerContainer);

                var propertiesContainer = new VisualElement {name = "properties"};
                {
                    var header = new VisualElement {name = "header"};
                    {
                        var title = new Label("Properties") {name = "title"};
                        header.Add(title);

                        var addPropertyButton = new Button(OnAddProperty) {text = "Add", name = "addButton"};
                        header.Add(addPropertyButton);
                    }
                    propertiesContainer.Add(header);

                    m_PropertyItems = new VisualContainer {name = "items"};
                    propertiesContainer.Add(m_PropertyItems);
                }
                topContainer.Add(propertiesContainer);
            }
            Add(topContainer);

            var bottomContainer = new VisualElement {name = "bottom"};
            {
                m_PreviewTextureView = new PreviewTextureView { name = "preview", image = Texture2D.blackTexture };
                m_PreviewTextureView.AddManipulator(new Draggable(OnMouseDrag, true));
                bottomContainer.Add(m_PreviewTextureView);

                m_PreviewScrollPosition = new Vector2(0f, 0f);

                m_PreviewMeshPicker = new ObjectField { objectType = typeof(Mesh) };
                m_PreviewMeshPicker.OnValueChanged(OnPreviewMeshChanged);

                bottomContainer.Add(m_PreviewMeshPicker);
            }
            Add(bottomContainer);

            m_PreviewRenderHandle = previewManager.masterRenderData;
            m_PreviewRenderHandle.onPreviewChanged += OnPreviewChanged;

            m_PreviewMeshPicker.SetValueAndNotify(m_Graph.previewData.serializedMesh.mesh);

            foreach (var property in m_Graph.properties)
                m_PropertyItems.Add(new ShaderPropertyView(m_Graph, property));

            Add(new ResizeSideHandle(this, ResizeHandleAnchor.TopLeft, new string[] {"resize", "diagonal", "top-left"}));
            Add(new ResizeSideHandle(this, ResizeHandleAnchor.Top, new string[] {"resize", "vertical", "top"}));
            Add(new ResizeSideHandle(this, ResizeHandleAnchor.TopRight, new string[] {"resize", "diagonal", "top-right"}));
            Add(new ResizeSideHandle(this, ResizeHandleAnchor.Right, new string[] {"resize", "horizontal", "right"}));
            Add(new ResizeSideHandle(this, ResizeHandleAnchor.BottomRight, new string[] {"resize", "diagonal", "bottom-right"}));
            Add(new ResizeSideHandle(this, ResizeHandleAnchor.Bottom, new string[] {"resize", "vertical", "bottom"}));
            Add(new ResizeSideHandle(this, ResizeHandleAnchor.BottomLeft, new string[] {"resize", "diagonal", "bottom-left"}));
            Add(new ResizeSideHandle(this, ResizeHandleAnchor.Left, new string[] {"resize", "horizontal", "left"}));
        }

        MasterNode masterNode
        {
            get { return m_PreviewRenderHandle.shaderData.node as MasterNode; }
        }

        void OnMouseDrag(Vector2 deltaMouse)
        {
            Vector2 previewSize = m_PreviewTextureView.contentRect.size;

            m_PreviewScrollPosition -= deltaMouse * (Event.current.shift ? 3f : 1f) / Mathf.Min(previewSize.x, previewSize.y) * 140f;
            m_PreviewScrollPosition.y = Mathf.Clamp(m_PreviewScrollPosition.y, -90f, 90f);
            Quaternion previewRotation = Quaternion.Euler(m_PreviewScrollPosition.y, 0, 0) * Quaternion.Euler(0, m_PreviewScrollPosition.x, 0);
            m_Graph.previewData.rotation = previewRotation;

            masterNode.Dirty(ModificationScope.Node);
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
            gm.AddItem(new GUIContent("Cubemap"), false, () => AddProperty(new CubemapShaderProperty()));
            gm.ShowAsContext();
        }

        void AddProperty(IShaderProperty property)
        {
            m_Graph.owner.RegisterCompleteObjectUndo("Add Property");
            m_Graph.AddShaderProperty(property);
        }

        void OnPreviewChanged()
        {
            m_PreviewTextureView.image = m_PreviewRenderHandle.texture ?? Texture2D.blackTexture;
            m_PreviewTextureView.Dirty(ChangeType.Repaint);
        }

        void OnPreviewMeshChanged(ChangeEvent<Object> changeEvent)
        {
            Mesh changedMesh = changeEvent.newValue as Mesh;

            masterNode.Dirty(ModificationScope.Node);

            if (m_Graph.previewData.serializedMesh.mesh != changedMesh)
            {
                m_Graph.previewData.rotation = Quaternion.identity;
            }

            m_Graph.previewData.serializedMesh.mesh = changedMesh;
        }

        public void HandleGraphChanges()
        {
            foreach (var propertyGuid in m_Graph.removedProperties)
            {
                var propertyView = m_PropertyItems.OfType<ShaderPropertyView>().FirstOrDefault(v => v.property.guid == propertyGuid); if (propertyView != null)
                    m_PropertyItems.Remove(propertyView);
            }

            foreach (var property in m_Graph.addedProperties)
                m_PropertyItems.Add(new ShaderPropertyView(m_Graph, property));
        }

        public void Dispose()
        {
            if (m_PreviewRenderHandle != null)
            {
                m_PreviewRenderHandle.onPreviewChanged -= OnPreviewChanged;
                m_PreviewRenderHandle = null;
            }
        }
    }
}
