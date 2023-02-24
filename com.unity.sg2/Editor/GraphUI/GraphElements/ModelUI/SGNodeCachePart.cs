using Unity.GraphToolsFoundation.Editor;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class SGNodeCachePart : NodeLodCachePart
    {
        /// <summary>
        /// Creates a new instance of the <see cref="NodeLodCachePart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="NodeLodCachePart"/>.</returns>
        public new static SGNodeCachePart Create(string name, Model model, ModelView ownerElement, string parentClassName)
        {
            if (model is SGNodeModel)
            {
                return new SGNodeCachePart(name, model, ownerElement, parentClassName);
            }

            return null;
        }


        Image m_PreviewImage;

        SGNodeCachePart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {
            m_OwnerElement.AddStylesheet("SGNodeCachePart.uss");
        }

        protected override void BuildPartUI(VisualElement parent)
        {
            base.BuildPartUI(parent);

            var previewContainer = new VisualElement(){name = "previewContainer"};

            m_PreviewImage = new Image(){name = "previewImage"};
            previewContainer.Add(m_PreviewImage);

            Root.Add(previewContainer);
        }

        protected override void UpdatePartFromModel()
        {
            base.UpdatePartFromModel();

            var sgNodeModel = m_Model as SGNodeModel;

            m_OwnerElement.EnableInClassList(m_ParentClassName.WithUssModifier("preview"), sgNodeModel?.IsPreviewExpanded ?? false);

            var newPreviewTexture = sgNodeModel?.PreviewTexture;

            if (newPreviewTexture != m_PreviewImage.image && newPreviewTexture != null)
            {
                m_PreviewImage.image = newPreviewTexture;
                m_PreviewImage.MarkDirtyRepaint();
            }
        }
    }
}
