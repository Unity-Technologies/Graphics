using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEditor.ShaderGraph.GraphUI.EditorCommon.CommandStateObserver;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.CommandDispatch;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements.Inspector
{
    public class ShaderGraphNodeFieldsInspector : NodeFieldsInspector
    {
        protected ShaderGraphNodeFieldsInspector(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {

        }

        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
            if (m_Model is GraphDataNodeModel { HasPreview: true })
                yield return new ModelPropertyField<PreviewMode>(
                    m_OwnerElement.CommandDispatcher,
                    m_Model,
                    nameof(PreviewMode),
                    null,
                    typeof(ChangePreviewModeCommand));

            if (m_Model is ICollapsible)
                yield return new ModelPropertyField<bool>(
                    m_OwnerElement.CommandDispatcher,
                    m_Model,
                    nameof(ICollapsible.Collapsed),
                    null,
                    typeof(CollapseNodeCommand));

            yield return new ModelPropertyField<ModelState>(
                m_OwnerElement.CommandDispatcher,
                m_Model,
                nameof(INodeModel.State),
                null,
                typeof(ChangeNodeStateCommand));
        }
    }
}
