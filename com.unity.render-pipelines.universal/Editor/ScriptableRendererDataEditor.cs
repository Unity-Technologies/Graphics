using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomPropertyDrawer(typeof(ScriptableRendererData), true)]
    public class ScriptableRendererDataEditor : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            //TODO: Fix it like for renderer features.
        }
    }
}
