using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Base class to inherit to create custom post process volume editors.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CustomPostProcessVolumeComponent), true)]
    public class CustomPostProcessVolumeComponentEditor : VolumeComponentEditor
    {
        internal static class Styles
        {
            public static readonly string customPostProcessNotInGlobalSettingsText = "This Custom Postprocess is not registered in the Global Settings.";
        }

        /// <summary>
        /// Unity calls this method each time it re-draws the Inspector.
        /// </summary>
        /// <remarks>
        /// You can safely override this method and not call <c>base.OnInspectorGUI()</c> unless you
        /// want Unity to display all the properties from the <see cref="VolumeComponent"/>
        /// automatically.
        /// </remarks>
        public override void OnInspectorGUI()
        {
            if (!HDRenderPipelineGlobalSettings.instance?.IsCustomPostProcessRegistered(target.GetType()) ?? false)
            {
                HDEditorUtils.GlobalSettingsHelpBox(Styles.customPostProcessNotInGlobalSettingsText,
                    MessageType.Error, HDRenderPipelineGlobalSettingsUI.Styles.customPostProcessOrderLabel.text);
                return;
            }

            base.OnInspectorGUI();
        }
    }
}
