//using System.Collections.Generic;
//using UnityEditor.GraphToolsFoundation.Overdrive;
//using UnityEditor.ShaderGraph.GraphUI.DataModel;
//using UnityEditor.ShaderGraph.GraphUI.EditorCommon.CommandStateObserver;
//using UnityEditor.ShaderGraph.GraphUI.GraphElements.CommandDispatch;

//namespace UnityEditor.ShaderGraph.GraphUI.GraphElements.Inspector
//{
//    public class ShaderGraphNodeFieldsInspector : NodeFieldsInspector
//    {
//        /// <summary>
//        /// Creates a new instance of the <see cref="ShaderGraphNodeFieldsInspector"/> class.
//        /// </summary>
//        /// <param name="name">The name of the part.</param>
//        /// <param name="model">The model displayed in this part.</param>
//        /// <param name="ownerElement">The owner of the part.</param>
//        /// <param name="parentClassName">The class name of the parent.</param>
//        /// <returns>A new instance of <see cref="ShaderGraphNodeFieldsInspector"/>.</returns>
//        public static ShaderGraphNodeFieldsInspector CreateInspector(string name, IGraphElementModel model, IModelView ownerElement, string parentClassName)
//        {
//            return new ShaderGraphNodeFieldsInspector(name, model, ownerElement, parentClassName);
//        }

//        protected ShaderGraphNodeFieldsInspector(string name, IGraphElementModel model, IModelView ownerElement, string parentClassName)
//            : base(name, model, ownerElement, parentClassName)
//        {

//        }

//        protected override IEnumerable<BaseModelPropertyField> GetFields()
//        {
//            if (m_Model is GraphDataNodeModel { HasPreview: true })
//                yield return new ModelPropertyField<PreviewMode>(
//                    m_OwnerElement.CommandDispatcher,
//                    m_Model,
//                    nameof(PreviewMode),
//                    null,
//                    typeof(ChangePreviewModeCommand));

//            if (m_Model is ICollapsible)
//                yield return new ModelPropertyField<bool>(
//                    m_OwnerElement.CommandDispatcher,
//                    m_Model,
//                    nameof(ICollapsible.Collapsed),
//                    null,
//                    typeof(CollapseNodeCommand));

//            yield return new ModelPropertyField<ModelState>(
//                m_OwnerElement.CommandDispatcher,
//                m_Model,
//                nameof(INodeModel.State),
//                null,
//                typeof(ChangeNodeStateCommand));
//        }
//    }
//}
