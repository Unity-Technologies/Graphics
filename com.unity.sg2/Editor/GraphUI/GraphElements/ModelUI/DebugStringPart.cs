using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class DebugStringPart : BaseModelViewPart
    {
        readonly Func<IGraphElementModel, string> m_GetString;

        public DebugStringPart(string name, IGraphElementModel model, IModelView ownerElement, string parentClassName,
            Func<IGraphElementModel, string> getString) : base(name, model, ownerElement, parentClassName)
        {
            m_GetString = getString;
        }

        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new Label();
            parent.Add(m_Root);
        }

        protected override void UpdatePartFromModel()
        {
            m_Root.text = m_GetString(m_Model as IGraphElementModel);
        }

        Label m_Root;
        public override VisualElement Root => m_Root;
    }
}
