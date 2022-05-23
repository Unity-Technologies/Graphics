using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class SGNodeFieldsInspector : SGFieldsInspector
    {
        public SGNodeFieldsInspector(string name, IModel model, IModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        GraphDataNodeModel nodeModel => m_Model as GraphDataNodeModel;

        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
            var propertyFieldList = new List<BaseModelPropertyField>();

            if (nodeModel == null)
                return propertyFieldList;

            var previewModeField =
                new ModelPropertyField<HeadlessPreviewManager.PreviewRenderMode, ChangePreviewModeCommand>(
                    m_OwnerElement.RootView as RootView,
                    nodeModel,
                    "Preview Mode",
                    "Preview Mode",
                    "Controls the way the preview output is rendered for this node",
                    (model) => ((GraphDataNodeModel)model).NodePreviewMode);

            propertyFieldList.Add(previewModeField);
            return propertyFieldList;
        }

        public override bool IsEmpty() => m_Fields.Count != 0;
    }
}
