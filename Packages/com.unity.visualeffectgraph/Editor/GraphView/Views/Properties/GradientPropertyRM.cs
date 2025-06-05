using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor.VFX.UI
{
    class GradientPropertyRM : PropertyRM<Gradient>
    {
        public override bool showsEverything { get { return true; } }

        private const string VFXGraphGradientPresetFileName = "VFXGradients";
        private GradientField m_GradientField;

        public GradientPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_GradientField = new GradientField(ObjectNames.NicifyVariableName(controller.name));
            m_GradientField.RegisterCallback<ChangeEvent<Gradient>>(OnValueChanged);
            m_GradientField.colorSpace = ColorSpace.Linear;
            m_GradientField.hdr = true;
            m_GradientField.RegisterCallback<ClickEvent>(OnClick);
            Add(m_GradientField);
        }

        public override float GetPreferredControlWidth()
        {
            return 120;
        }

        public void OnValueChanged(ChangeEvent<Gradient> e)
        {
            Gradient newValue = m_GradientField.value;
            m_Value = newValue;
            NotifyValueChanged();
        }

        public override void UpdateGUI(bool force)
        {
            m_GradientField.SetValueWithoutNotify(m_Value);
        }

        protected override void UpdateEnabled()
        {
            m_GradientField.SetEnabled(propertyEnabled);
        }

        protected override void UpdateIndeterminate()
        {
            m_GradientField.visible = !indeterminate;
        }

        private void OnClick(ClickEvent evt)
        {
            OnShowGradientPreset();
        }

        private void OnShowGradientPreset()
        {
            var saveLoadHelper = new ScriptableObjectSaveLoadHelper<GradientPresetLibrary>("gradients", SaveType.Text);
            var defaultLibFilePath = PresetLibraryLocations.defaultLibraryLocation;

            if (!string.IsNullOrEmpty(defaultLibFilePath))
            {
                var vfxPackagePath = VisualEffectGraphPackageInfo.assetPackagePath;
                var vfxLibraryFilePath = $"{vfxPackagePath}/Editor/{VFXGraphGradientPresetFileName}".Replace("\\", "/");
                var defaultLibraryFilePath = $"{defaultLibFilePath}/{VFXGraphGradientPresetFileName}".Replace("\\", "/");
                var vfxLibrary = PresetLibraryManager.instance.GetLibrary(saveLoadHelper, vfxLibraryFilePath);
                var defaultLibrary = PresetLibraryManager.instance.GetLibrary(saveLoadHelper, defaultLibraryFilePath);
                if (vfxLibrary != null && defaultLibrary == null)
                {
                    PresetLibraryManager.instance.SaveLibrary(saveLoadHelper, vfxLibrary, defaultLibFilePath + VFXGraphGradientPresetFileName);
                }
            }
        }
    }
}
