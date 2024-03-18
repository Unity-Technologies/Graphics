using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor.VFX.UI
{
    class CurvePropertyRM : PropertyRM<AnimationCurve>
    {
        private const string VFXGraphCurvePresetFileName = "VFX Graph Curves";

        public CurvePropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_CurveField = new CurveField(ObjectNames.NicifyVariableName(controller.name));
            m_CurveField.Q<VisualElement>("unity-border").RemoveFromHierarchy();
            m_CurveField.RegisterCallback<ChangeEvent<AnimationCurve>>(OnValueChanged);
            m_CurveField.onShowPresets += OnShowCurvePreset;

            Add(m_CurveField);
        }

        private void OnShowCurvePreset()
        {
            var saveLoadHelper = new ScriptableObjectSaveLoadHelper<CurvePresetLibrary>("curves", SaveType.Text);

            var defaultLibFilePath = PresetLibraryLocations.defaultLibraryLocation;
            if (!string.IsNullOrEmpty(defaultLibFilePath))
            {
                var defaultPath = Path.GetDirectoryName(defaultLibFilePath);
                var vfxLibraryFilePath = $"{defaultPath}/{VFXGraphCurvePresetFileName}".Replace("\\", "/");


                var library = PresetLibraryManager.instance.GetLibrary(saveLoadHelper, vfxLibraryFilePath);
                if (library == null)
                {
                    library = PresetLibraryManager.instance.CreateLibrary(saveLoadHelper, vfxLibraryFilePath);

                    library.Add(new AnimationCurve(CurveEditorWindow.GetConstantKeys(0.5f)), "Constant");
                    library.Add(new AnimationCurve(CurveEditorWindow.GetLinearKeys()), "Linear");
                    library.Add(new AnimationCurve(CurveEditorWindow.GetLinearMirrorKeys()), "Linear Mirror");
                    library.Add(new AnimationCurve(CurveEditorWindow.GetEaseInKeys()), "EaseIn");
                    library.Add(new AnimationCurve(CurveEditorWindow.GetEaseInMirrorKeys()), "EaseIn Mirror");
                    library.Add(new AnimationCurve(CurveEditorWindow.GetEaseOutKeys()), "EaseOut");
                    library.Add(new AnimationCurve(CurveEditorWindow.GetEaseOutMirrorKeys()), "EaseOut Mirror");
                    library.Add(new AnimationCurve(CurveEditorWindow.GetEaseInOutKeys()), "EaseInOut");
                    library.Add(new AnimationCurve(CurveEditorWindow.GetEaseInOutMirrorKeys()), "EaseInOut Mirror");

                    PresetLibraryManager.instance.SaveLibrary(saveLoadHelper, library, vfxLibraryFilePath);
                    CurveEditorWindow.instance.currentPresetLibrary = vfxLibraryFilePath;
                }
            }
        }

        public override float GetPreferredControlWidth()
        {
            return 110;
        }

        public void OnValueChanged(ChangeEvent<AnimationCurve> e)
        {
            AnimationCurve newValue = m_CurveField.value;
            m_Value = newValue;
            NotifyValueChanged();
        }

        CurveField m_CurveField;

        protected override void UpdateEnabled()
        {
            m_CurveField.SetEnabled(propertyEnabled);
        }

        protected override void UpdateIndeterminate()
        {
            m_CurveField.visible = !indeterminate;
        }

        public override void UpdateGUI(bool force)
        {
            m_CurveField.SetValueWithoutNotify(m_Value);
        }

        public override bool showsEverything { get { return true; } }
    }
}
