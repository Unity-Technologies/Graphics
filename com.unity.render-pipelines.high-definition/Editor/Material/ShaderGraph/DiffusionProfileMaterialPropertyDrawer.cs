using UnityEngine;
using UnityEditor;
using System;

namespace UnityEditor.Rendering.HighDefinition
{
    class DiffusionProfileDrawer : MaterialPropertyDrawer
    {
        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor) => 0;

        public override void OnGUI(Rect position, MaterialProperty prop, String label, MaterialEditor editor)
        {
            // Find properties
            var assetProperty = MaterialEditor.GetMaterialProperty(editor.targets, prop.name + "_Asset");
            DiffusionProfileMaterialUI.OnGUI(editor, assetProperty, prop, 0, prop.displayName);
        }
    }
}
