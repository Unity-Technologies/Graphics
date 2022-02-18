using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Colors
{
    class UserColors : ColorProviderFromCode
    {
        const string m_Title = "User Defined";
        public override string GetTitle() => m_Title;

        public override bool AllowCustom() => true;
        public override bool ClearOnDirty() => false;

        protected override bool GetColorFromNode(AbstractMaterialNode node, out Color color)
        {
            color = Color.black;
            return node.TryGetColor(m_Title, ref color);
        }
    }
}
