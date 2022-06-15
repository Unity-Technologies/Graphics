using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class VariableDeclarationInspector : SGFieldsInspector
    {
        public VariableDeclarationInspector(string name, IModel model, IModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
            yield return new LabelPropertyField("WIP", m_OwnerElement.RootView);
        }

        public override bool IsEmpty() => false;
    }
}
