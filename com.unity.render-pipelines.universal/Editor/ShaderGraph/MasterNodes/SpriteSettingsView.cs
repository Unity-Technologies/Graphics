using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.Experimental.Rendering.Universal
{
    class SpriteSettingsView : MasterNodeSettingsView
    {
        public SpriteSettingsView(AbstractMaterialNode node) : base(node)
        {
            Add(GetShaderGUIOverridePropertySheet());
        }
    }
}
