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
            filterType = root.FindPropertyRelative("filterType");
            maskColor = root.FindPropertyRelative("maskColor");
            keyThreshold = root.FindPropertyRelative("keyThreshold");
            keyTolerance = root.FindPropertyRelative("keyTolerance");
            spillRemoval = root.FindPropertyRelative("spillRemoval");
            alphaMask = root.FindPropertyRelative("alphaMask");
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
