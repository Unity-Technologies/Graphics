using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class NodePreviewPart : BaseModelViewPart
    {
        VisualElement m_Root;
        VisualElement m_CollapseButton;
        VisualElement m_ExpandButton;
        VisualElement m_PreviewContainer;
        Image m_PreviewImage;

        GraphDataNodeModel m_GraphDataNodeModel;

        const string ussRootName = "ge-node-preview-part";

        public override VisualElement Root => m_Root;

        public NodePreviewPart(string name, IGraphElementModel model, IModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {
            m_GraphDataNodeModel = model as GraphDataNodeModel;
        }

        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement();
            m_Root.name = ussRootName;
            GraphElementHelper.LoadTemplateAndStylesheet(m_Root, "NodePreviewPart", ussRootName);

            AssertHelpers.IsNotNull(m_Root, "Failed to load UXML for NodePreviewPart");

            m_PreviewImage = m_Root.Q<Image>("preview");
            if (m_PreviewImage != null)
            {
                var defaultTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.shadergraph/Editor/GraphUI/GraphElements/Stylesheets/Icons/PreviewDefault.png");
                m_PreviewImage.image = defaultTexture;
            }

            // TODO: (Sai) Re-enable in sprint 2
            //m_CollapseButton = m_Root.Q<VisualElement>("collapse");
            //m_CollapseButton?.RegisterCallback<MouseDownEvent>(OnCollapseButtonClicked);
            //
            //m_ExpandButton = m_Root.Q<VisualElement>("expand");
            //m_ExpandButton?.RegisterCallback<MouseDownEvent>(OnExpandButtonClicked);

            m_PreviewContainer = m_Root.Q<VisualElement>("previewContainer");

            // TODO: (Sai) Re-enable in sprint 2
            // TODO: Handle preview collapse/expand state serialization
            // HandlePreviewExpansionStateChanged(m_GraphDataNodeModel.IsPreviewVisible);

            parent.Add(Root);
        }

        protected override void UpdatePartFromModel()
        {
            // Don't need to do this for node previews in Searcher
            if (!m_GraphDataNodeModel.existsInGraphData)
                return;

            // TODO: (Sai) Re-enable in sprint 2
            //HandlePreviewExpansionStateChanged(m_GraphDataNodeModel.IsPreviewVisible);

            // TODO: When shader compilation is complete and we have updated texture, need to notify NodePreviewPart so image tint can be changed
            HandlePreviewShaderCurrentlyCompiling(m_GraphDataNodeModel.PreviewShaderIsCompiling);

            HandlePreviewTextureUpdated(m_GraphDataNodeModel.PreviewTexture);
        }

        void OnCollapseButtonClicked(MouseDownEvent mouseDownEvent)
        {
            // m_OwnerElement.RootView.Dispatch(new ChangePreviewExpandedCommand(false, new [] { m_GraphDataNodeModel }));
        }

        void OnExpandButtonClicked(MouseDownEvent mouseDownEvent)
        {
            // m_OwnerElement.RootView.Dispatch(new ChangePreviewExpandedCommand(true, new [] { m_GraphDataNodeModel }));
        }

        void HandlePreviewExpansionStateChanged(bool previewExpanded)
        {
            if (previewExpanded)
            {
                // Hide Preview expand button and show image instead (which also contains the collapse button)
                m_ExpandButton.RemoveFromHierarchy();
                if (m_PreviewContainer.Contains(m_PreviewImage) == false)
                {
                    m_PreviewContainer.Add(m_PreviewImage);
                }
            }
            else
            {
                // Hide Image and Show Preview expand button instead
                m_PreviewImage.RemoveFromHierarchy();
                if (m_PreviewContainer.Contains(m_ExpandButton) == false)
                {
                    m_PreviewContainer.Add(m_ExpandButton);
                }
            }
        }

        void HandlePreviewShaderCurrentlyCompiling(bool isPreviewShaderCompiling)
        {
            if(isPreviewShaderCompiling)
                m_PreviewImage.image = Texture2D.blackTexture;
            m_PreviewImage.tintColor = isPreviewShaderCompiling ? new Color(1.0f, 1.0f, 1.0f, 0.3f) : Color.white;
        }

        void HandlePreviewTextureUpdated(Texture newPreviewTexture)
        {
            if(newPreviewTexture != m_PreviewImage.image && newPreviewTexture != null)
                m_PreviewImage.image = newPreviewTexture;
            else
                m_PreviewImage.MarkDirtyRepaint();
        }
    }
}
