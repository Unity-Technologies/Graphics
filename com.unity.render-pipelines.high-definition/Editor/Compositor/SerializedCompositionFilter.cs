using UnityEditor;

namespace UnityEditor.Rendering.HighDefinition.Compositor
{
    internal class SerializedCompositionFilter
    {
        public SerializedProperty filterType;
        public SerializedProperty maskColor;
        public SerializedProperty keyThreshold;
        public SerializedProperty keyTolerance;
        public SerializedProperty spillRemoval;
        public SerializedProperty alphaMask;

        public SerializedCompositionFilter(SerializedProperty root)
        {
            filterType = root.FindPropertyRelative("m_Type");
            maskColor = root.FindPropertyRelative("m_MaskColor");
            keyThreshold = root.FindPropertyRelative("m_KeyThreshold");
            keyTolerance = root.FindPropertyRelative("m_KeyTolerance");
            spillRemoval = root.FindPropertyRelative("m_SpillRemoval");
            alphaMask = root.FindPropertyRelative("m_AlphaMask");
        }

        public float GetHeight()
        {
            if (filterType.intValue == 0)
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
