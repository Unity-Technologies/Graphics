using System;
using Unity.GraphToolsFoundation.Editor;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class DebugStringPart : BaseModelViewPart
    {
        readonly Func<GraphElementModel, string> m_GetString;

        public DebugStringPart(string name, GraphElementModel model, ModelView ownerElement, string parentClassName,
            Func<GraphElementModel, string> getString) : base(name, model, ownerElement, parentClassName)
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
            m_Root.text = m_GetString(m_Model as GraphElementModel);
        }

        Label m_Root;
        public override VisualElement Root => m_Root;
    }
}
