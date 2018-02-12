using System;
using System.Linq;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

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

            foreach (var property in m_Graph.properties)
                m_PropertyItems.Add(new ShaderPropertyView(m_Graph, property));

            Add(new ResizeBorderFrame(this) { name = "resizeBorderFrame" });

            this.AddManipulator(new WindowDraggable());
        }

        void OnAddProperty()
        {
            var gm = new GenericMenu();
            gm.AddItem(new GUIContent("Vector1"), false, () => AddProperty(new Vector1ShaderProperty()));
            gm.AddItem(new GUIContent("Vector2"), false, () => AddProperty(new Vector2ShaderProperty()));
            gm.AddItem(new GUIContent("Vector3"), false, () => AddProperty(new Vector3ShaderProperty()));
            gm.AddItem(new GUIContent("Vector4"), false, () => AddProperty(new Vector4ShaderProperty()));
            gm.AddItem(new GUIContent("Color"), false, () => AddProperty(new ColorShaderProperty()));
            gm.AddItem(new GUIContent("HDR Color"), false, () => AddProperty(new ColorShaderProperty() {colorMode = ColorMode.HDR}));
            gm.AddItem(new GUIContent("Boolean"), false, () => AddProperty(new BooleanShaderProperty()));
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
