using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedGPUResidentDrawerSettings
    {
        public SerializedProperty root;
        public SerializedProperty mode;

        public SerializedGPUResidentDrawerSettings(SerializedProperty root)
        {
            this.root = root;
            mode = root.Find((GlobalGPUResidentDrawerSettings s) => s.mode);
        }
    }
}
