using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedOrderIndependentTransparencySettings
    {
        public SerializedProperty root;

        public SerializedProperty enabled;
        public SerializedProperty memoryBudget;

        public SerializedOrderIndependentTransparencySettings(SerializedProperty root)
        {
            this.root = root;

            enabled = root.Find((GlobalOrderIndependentTransparencySettings s) => s.enabled);
            memoryBudget = root.Find((GlobalOrderIndependentTransparencySettings s) => s.memoryBudget);
        }
    }
}
