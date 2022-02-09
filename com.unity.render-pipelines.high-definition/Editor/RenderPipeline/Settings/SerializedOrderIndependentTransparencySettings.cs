using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedOrderIndependentTransparencySettings
    {
        public SerializedProperty root;

        public SerializedProperty enabled;
        public SerializedProperty memoryBudget;
        public SerializedProperty oitLightingMode;
        public SerializedProperty maxHiZMip;
        public SerializedProperty enableAccumulation;
        public SerializedProperty accumulationCoef;

        public SerializedOrderIndependentTransparencySettings(SerializedProperty root)
        {
            this.root = root;

            enabled = root.Find((GlobalOrderIndependentTransparencySettings s) => s.enabled);
            memoryBudget = root.Find((GlobalOrderIndependentTransparencySettings s) => s.memoryBudget);
            oitLightingMode = root.Find((GlobalOrderIndependentTransparencySettings s) => s.oitLightingMode);
            maxHiZMip = root.Find((GlobalOrderIndependentTransparencySettings s) => s.maxHiZMip);
            enableAccumulation = root.Find((GlobalOrderIndependentTransparencySettings s) => s.enableAccumulation);
            accumulationCoef = root.Find((GlobalOrderIndependentTransparencySettings s) => s.accumulationCoef);
        }
    }
}
