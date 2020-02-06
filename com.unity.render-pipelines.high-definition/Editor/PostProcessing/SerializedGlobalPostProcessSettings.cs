
namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedGlobalPostProcessSettings
    {
        public SerializedProperty root;

        public SerializedProperty lutSize;
        public SerializedProperty lutFormat;
        public SerializedProperty bufferFormat;

        public SerializedGlobalPostProcessSettings(SerializedProperty root)
        {
            this.root = root;

            lutSize = root.FindPropertyRelative("m_LutSize");
            lutFormat = root.FindPropertyRelative("lutFormat");
            bufferFormat = root.FindPropertyRelative("bufferFormat");
        }
    }
}
