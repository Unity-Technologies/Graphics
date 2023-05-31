using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CreateAssetMenu(fileName = "SettingHelper", menuName = "HDRP Asset Setting/Setting Helper")]
    public class SettingHelperSO : ScriptableObject
    {
        [SerializeField]
        public string header;
        [SerializeField]
        public RequiredSetting[] requiredSettings;
    }

    [System.Serializable]
    public class RequiredSetting
    {
        [SerializeField]
        internal HDRenderPipelineUI.ExpandableGroup uiSection;
        [SerializeField]
        public string propertyPath;
        private string relativePath => propertyPath.Remove(0, 25);
        [SerializeField]
        public string message;

        public int uiSectionInt => (int)uiSection;

        public void ShowSetting()
        {
            ShowSettingUI(uiSectionInt, propertyPath);
        }
        static public void ShowSettingUI(int uiSection, string propertyPath)
        {
            SettingsService.OpenProjectSettings("Project/Quality/HDRP");
            HDRenderPipelineUI.Inspector.Expand((int)uiSection);
            CoreEditorUtils.Highlight("Project Settings", propertyPath, HighlightSearchMode.Identifier);
        }

        static internal void ShowSettingUI(HDRenderPipelineUI.ExpandableRendering uiSection, string propertyPath)
        {
            ShowSettingUI((int)uiSection, propertyPath);
        }

        public bool needsToBeEnabled
        {
            get
            {
                var rpAsset = QualitySettings.GetRenderPipelineAssetAt(QualitySettings.GetQualityLevel());
                if (rpAsset == null) return false;

                var hdrpAsset = (HDRenderPipelineAsset) rpAsset;
                if (hdrpAsset == null) return false;

                var serializedHDRPAsset = new SerializedObject(hdrpAsset);
                var settingSP = serializedHDRPAsset.FindProperty("m_RenderPipelineSettings");

                var state = settingSP.FindPropertyRelative(relativePath).boolValue;

                return !state;
            }
        }
    }
}
