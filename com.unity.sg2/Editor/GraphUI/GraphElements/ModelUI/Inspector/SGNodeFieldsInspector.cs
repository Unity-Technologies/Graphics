using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class SGNodeFieldsInspector : SGFieldsInspector
    {
        public SGNodeFieldsInspector(string name, IEnumerable<Model> models, RootView rootView, string parentClassName)
            : base(name, models, rootView, parentClassName) { }

        IEnumerable<GraphDataNodeModel> nodeModels => m_Models.OfType<GraphDataNodeModel>();

        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
            var propertyFieldList = new List<BaseModelPropertyField>();

            if (nodeModels == null || !nodeModels.Any())
                return propertyFieldList;

            var previewModeField =
                new ModelPropertyField<PreviewService.PreviewRenderMode, ChangePreviewModeCommand>(
                    RootView,
                    nodeModels,
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
