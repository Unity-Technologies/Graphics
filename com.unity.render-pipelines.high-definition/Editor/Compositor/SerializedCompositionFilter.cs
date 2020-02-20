using UnityEditor;

namespace UnityEditor.Rendering.HighDefinition.Compositor
{
    internal class SerializedCompositionFilter
    {
        public SerializedProperty FilterType;
        public SerializedProperty MaskColor;
        public SerializedProperty KeyThreshold;
        public SerializedProperty KeyTolerance;
        public SerializedProperty SpillRemoval;
        public SerializedProperty AlphaMask;

        public SerializedCompositionFilter(SerializedProperty root)
        {
            FilterType = root.FindPropertyRelative("m_Type");
            MaskColor = root.FindPropertyRelative("m_MaskColor");
            KeyThreshold = root.FindPropertyRelative("m_KeyThreshold");
            KeyTolerance = root.FindPropertyRelative("m_KeyTolerance");
            SpillRemoval = root.FindPropertyRelative("m_SpillRemoval");
            AlphaMask = root.FindPropertyRelative("m_AlphaMask");
        }

        public float GetHeight()
        {
            if (FilterType.intValue == 0)
            {
                return 5 * CompositorStyle.k_Spacing;
            }
            else
            {
                return CompositorStyle.k_Spacing;
            }
        }
    }
}
