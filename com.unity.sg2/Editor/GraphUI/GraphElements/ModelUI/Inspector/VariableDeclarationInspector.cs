using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class VariableDeclarationInspector : SGFieldsInspector
    {
        public VariableDeclarationInspector(string name, IModel model, IModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
            if (m_Model is not GraphDataVariableDeclarationModel variableDeclarationModel) yield break;

            yield return new SGModelPropertyField<string>(m_OwnerElement.RootView,
                m_Model,
                nameof(GraphDataVariableDeclarationModel.Title),
                "Name",
                null);

            yield return InlineValueEditor.CreateEditorForConstant(m_OwnerElement.RootView, variableDeclarationModel,
                        variableDeclarationModel.InitializationModel, false,"Value");

            yield return new SGModelPropertyField<ContextEntryEnumTags.DataSource>(m_OwnerElement.RootView,
                m_Model,
                nameof(GraphDataVariableDeclarationModel.ShaderDeclaration),
                "Shader Declaration",
                null);
        }

        public override bool IsEmpty() => false;
    }
}
