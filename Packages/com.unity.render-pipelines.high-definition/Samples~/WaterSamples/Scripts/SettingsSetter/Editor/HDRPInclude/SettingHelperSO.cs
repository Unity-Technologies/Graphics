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
        internal int uiSubSection;
        [SerializeField]
        public string propertyPath;
        private string relativePath => propertyPath.Remove(0, 25);
        [SerializeField]
        public string message;

        public void ShowSetting()
        {
            SettingsService.OpenProjectSettings("Project/Quality/HDRP");
            HDRenderPipelineUI.ExpandGroup(uiSection);
            if (uiSubSection != -1)
                HDRenderPipelineUI.SubInspectors[uiSection].Expand(uiSubSection);
            CoreEditorUtils.Highlight("Project Settings", propertyPath, HighlightSearchMode.Identifier);
        }

        public bool needsToBeEnabled
        {
            get
            {
                var hdrpAsset = HDRenderPipeline.currentAsset;
                if (hdrpAsset == null) return false;

                var serializedHDRPAsset = new SerializedObject(hdrpAsset);
                var settingSP = serializedHDRPAsset.FindProperty("m_RenderPipelineSettings");

                var state = settingSP.FindPropertyRelative(relativePath).boolValue;

                return !state;
            }
        }
    }
}
