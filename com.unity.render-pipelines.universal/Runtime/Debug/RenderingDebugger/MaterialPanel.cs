using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    class MaterialPanel : RenderingDebuggerPanel
    {
        public override string panelName => "Material";

        public override VisualElement CreatePanel()
        {
            VisualElement panel = CreateVisualElement(
                "Packages/com.unity.render-pipelines.universal/Runtime/Debug/RenderingDebugger/MaterialPanel.uxml");

            RegisterCallback<Enum>(panel, nameof(materialDebugMode), OnMaterialOverrideChanged);

            return panel;
        }

        public override bool AreAnySettingsActive => (materialDebugMode != DebugMaterialMode.None);
                // || // TODO
                //    (vertexAttributeDebugMode != DebugVertexAttributeMode.None) ||
                //    (materialValidationMode != DebugMaterialValidationMode.None);

        public override bool IsPostProcessingAllowed => !AreAnySettingsActive;
        public override bool IsLightingActive => !AreAnySettingsActive;

        private void OnMaterialOverrideChanged(ChangeEvent<Enum> evt)
        {
            // Handling code
            Debug.Log($"{(DebugMaterialMode)evt.newValue} - {(DebugMaterialMode)evt.previousValue}");
        }

        /// <summary>
        /// Current debug material mode.
        /// </summary>
        public DebugMaterialMode materialDebugMode;

        public int materialHiddenTest;
    }
}
