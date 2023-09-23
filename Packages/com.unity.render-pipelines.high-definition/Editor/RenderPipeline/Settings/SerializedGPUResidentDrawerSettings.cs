using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedGPUResidentDrawerSettings
    {
        public SerializedProperty root;
        public SerializedProperty mode;
        public SerializedProperty allowInEditMode;

        public SerializedGPUResidentDrawerSettings(SerializedProperty root)
        {
            this.root = root;
            mode = root.Find((GlobalGPUResidentDrawerSettings s) => s.mode);
            allowInEditMode = root.Find((GlobalGPUResidentDrawerSettings s) => s.allowInEditMode);
        }
    }
}
