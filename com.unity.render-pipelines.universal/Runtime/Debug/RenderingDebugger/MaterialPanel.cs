using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    class MaterialPanel : RenderingDebuggerPanel
    {
        public override string panelName => "Material";

        public DebugMaterialMode materialDebugMode;
        public DebugVertexAttributeMode vertexAttributeDebugMode;

        public DebugMaterialValidationMode materialValidationMode;

        // Albedo
        struct AlbedoDebugValidationPresetData
        {
            public string name;
            public Color color;
            public float minLuminance;
            public float maxLuminance;
        }

        AlbedoDebugValidationPresetData[] m_AlbedoDebugValidationPresetData =
        {
            new AlbedoDebugValidationPresetData()
            {
                name = "Default Luminance",
                color = new Color(127f / 255f, 127f / 255f, 127f / 255f),
                minLuminance = 0.01f,
                maxLuminance = 0.90f
            },
            // colors taken from http://www.babelcolor.com/index_htm_files/ColorChecker_RGB_and_spectra.xls
            new AlbedoDebugValidationPresetData()
            {
                name = "Black Acrylic Paint",
                color = new Color(56f / 255f, 56f / 255f, 56f / 255f),
                minLuminance = 0.03f,
                maxLuminance = 0.07f
            },
            new AlbedoDebugValidationPresetData()
            {
                name = "Dark Soil",
                color = new Color(85f / 255f, 61f / 255f, 49f / 255f),
                minLuminance = 0.05f,
                maxLuminance = 0.14f
            },

            new AlbedoDebugValidationPresetData()
            {
                name = "Worn Asphalt",
                color = new Color(91f / 255f, 91f / 255f, 91f / 255f),
                minLuminance = 0.10f,
                maxLuminance = 0.15f
            },
            new AlbedoDebugValidationPresetData()
            {
                name = "Dry Clay Soil",
                color = new Color(137f / 255f, 120f / 255f, 102f / 255f),
                minLuminance = 0.15f,
                maxLuminance = 0.35f
            },
            new AlbedoDebugValidationPresetData()
            {
                name = "Green Grass",
                color = new Color(123f / 255f, 131f / 255f, 74f / 255f),
                minLuminance = 0.16f,
                maxLuminance = 0.26f
            },
            new AlbedoDebugValidationPresetData()
            {
                name = "Old Concrete",
                color = new Color(135f / 255f, 136f / 255f, 131f / 255f),
                minLuminance = 0.17f,
                maxLuminance = 0.30f
            },
            new AlbedoDebugValidationPresetData()
            {
                name = "Red Clay Tile",
                color = new Color(197f / 255f, 125f / 255f, 100f / 255f),
                minLuminance = 0.23f,
                maxLuminance = 0.33f
            },
            new AlbedoDebugValidationPresetData()
            {
                name = "Dry Sand",
                color = new Color(177f / 255f, 167f / 255f, 132f / 255f),
                minLuminance = 0.20f,
                maxLuminance = 0.45f
            },
            new AlbedoDebugValidationPresetData()
            {
                name = "New Concrete",
                color = new Color(185f / 255f, 182f / 255f, 175f / 255f),
                minLuminance = 0.32f,
                maxLuminance = 0.55f
            },
            new AlbedoDebugValidationPresetData()
            {
                name = "White Acrylic Paint",
                color = new Color(227f / 255f, 227f / 255f, 227f / 255f),
                minLuminance = 0.75f,
                maxLuminance = 0.85f
            },
            new AlbedoDebugValidationPresetData()
            {
                name = "Fresh Snow",
                color = new Color(243f / 255f, 243f / 255f, 243f / 255f),
                minLuminance = 0.85f,
                maxLuminance = 0.95f
            },
            new AlbedoDebugValidationPresetData()
            {
                name = "Blue Sky",
                color = new Color(93f / 255f, 123f / 255f, 157f / 255f),
                minLuminance = new Color(93f / 255f, 123f / 255f, 157f / 255f).linear.maxColorComponent - 0.05f,
                maxLuminance = new Color(93f / 255f, 123f / 255f, 157f / 255f).linear.maxColorComponent + 0.05f
            },
            new AlbedoDebugValidationPresetData()
            {
                name = "Foliage",
                color = new Color(91f / 255f, 108f / 255f, 65f / 255f),
                minLuminance = new Color(91f / 255f, 108f / 255f, 65f / 255f).linear.maxColorComponent - 0.05f,
                maxLuminance = new Color(91f / 255f, 108f / 255f, 65f / 255f).linear.maxColorComponent + 0.05f
            },
            new AlbedoDebugValidationPresetData()
            {
                name = "Custom",
                color = new Color(127f / 255f, 127f / 255f, 127f / 255f),
                minLuminance = 0.01f,
                maxLuminance = 0.90f
            },
        };

        public DebugDisplaySettingsMaterial.AlbedoDebugValidationPreset albedoValidationPreset;
        public float albedoMinLuminance = 0.01f;
        public float albedoMaxLuminance = 0.90f;
        public float albedoHueTolerance = 0.104f;
        public float albedoSaturationTolerance = 0.214f;
        public Color albedoCompareColor = new Color(127f / 255f, 127f / 255f, 127f / 255f, 255f / 255f);

        // Metalic
        public float metallicMinValue = 0.0f;
        public float metallicMaxValue = 0.9f;

        public override VisualElement CreatePanel()
        {
            var panelVisualTreeAsset = Resources.Load<VisualTreeAsset>("MaterialPanel");
            VisualElement panel = CreateVisualElement(panelVisualTreeAsset);
            return panel;
        }

        public override bool AreAnySettingsActive => (materialDebugMode != DebugMaterialMode.None) ||
                (vertexAttributeDebugMode != DebugVertexAttributeMode.None) ||
                (materialValidationMode != DebugMaterialValidationMode.None);

        public override bool IsPostProcessingAllowed => !AreAnySettingsActive;
        public override bool IsLightingActive => !AreAnySettingsActive;

        public override bool TryGetScreenClearColor(ref Color color) => false;

        protected override void RegisterCallbacks(VisualElement element)
        {
            RegisterCallback<Enum>(element, nameof(materialValidationMode), materialValidationMode, OnMaterialValidationModeChanged);
            RegisterCallback<Enum>(element, nameof(albedoValidationPreset), albedoValidationPreset, OnMaterialAlbedoDebugValidationPresetChanged);
        }

#if !UNITY_EDITOR
        protected override void BindToInternal(VisualElement targetElement)
        {
            // Material filters
            RegisterChange<Enum>(targetElement, nameof(materialDebugMode), materialDebugMode, evt => materialDebugMode = (DebugMaterialMode)evt.newValue);
            RegisterChange<Enum>(targetElement, nameof(vertexAttributeDebugMode), vertexAttributeDebugMode, evt => vertexAttributeDebugMode = (DebugVertexAttributeMode)evt.newValue);

            // Material Validation
            RegisterChange<Enum>(targetElement, nameof(materialValidationMode), materialValidationMode, evt => materialValidationMode = (DebugMaterialValidationMode)evt.newValue);

            // Albedo
            RegisterChange<Enum>(targetElement, nameof(albedoValidationPreset), albedoValidationPreset, evt => albedoValidationPreset = (DebugDisplaySettingsMaterial.AlbedoDebugValidationPreset)evt.newValue);
            RegisterChange<float>(targetElement, nameof(albedoMinLuminance), albedoMinLuminance, evt => albedoMinLuminance = evt.newValue);
            RegisterChange<float>(targetElement, nameof(albedoMaxLuminance), albedoMaxLuminance, evt => albedoMaxLuminance = evt.newValue);
            RegisterChange<float>(targetElement, nameof(albedoHueTolerance), albedoHueTolerance, evt => albedoHueTolerance = evt.newValue);
            RegisterChange<float>(targetElement, nameof(albedoSaturationTolerance), albedoSaturationTolerance, evt => albedoSaturationTolerance = evt.newValue);

            // Metallic
            RegisterChange<float>(targetElement, nameof(metallicMinValue), metallicMinValue, evt => metallicMinValue = evt.newValue);
            RegisterChange<float>(targetElement, nameof(metallicMaxValue), metallicMaxValue, evt => metallicMaxValue = evt.newValue);
        }
#endif

        private void OnMaterialAlbedoDebugValidationPresetChanged(ChangeEvent<Enum> evt)
        {
            var newEnum = (DebugDisplaySettingsMaterial.AlbedoDebugValidationPreset)evt.newValue;
            AlbedoDebugValidationPresetData presetData = m_AlbedoDebugValidationPresetData[(int)newEnum];
            albedoMinLuminance = presetData.minLuminance;
            albedoMaxLuminance = presetData.maxLuminance;
            albedoCompareColor = presetData.color;
            
            SetElementHidden(nameof(albedoCompareColor), DebugDisplaySettingsMaterial.AlbedoDebugValidationPreset.Custom != newEnum);

            bool isDefaultLuminance = DebugDisplaySettingsMaterial.AlbedoDebugValidationPreset.DefaultLuminance == newEnum;
            if (isDefaultLuminance)
            {
                albedoSaturationTolerance = 1.0f;
                albedoHueTolerance = 1.0f;
            }
            SetElementHidden(nameof(albedoSaturationTolerance), isDefaultLuminance);
            SetElementHidden(nameof(albedoHueTolerance), isDefaultLuminance);
        }

        private void OnMaterialValidationModeChanged(ChangeEvent<Enum> evt)
        {
            SetElementHidden("AlbedoSettings", (DebugMaterialValidationMode)evt.newValue != DebugMaterialValidationMode.Albedo);
            SetElementHidden("MetallicSettings", (DebugMaterialValidationMode)evt.newValue != DebugMaterialValidationMode.Metallic);
        }
    }
}
