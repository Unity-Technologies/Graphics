using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Base class to inherit to create custom post process volume editors.
    /// </summary>
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(CustomPostProcessVolumeComponent))]
    public class CustomPostProcessVolumeComponentEditor : VolumeComponentEditor
    {
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
                HDEditorUtils.GlobalSettingsHelpBox("The custom post process is not registered in the Global Settings.",
                    MessageType.Error, HDRenderPipelineGlobalSettingsUI.Styles.customPostProcessOrderLabel.text);
                return;
            }

            base.OnInspectorGUI();
        }
    }
}
