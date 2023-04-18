using System.Linq;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Default Volume Profile Editor.
    /// </summary>
    [CustomEditor(typeof(VolumeProfile))]
    [SupportedOnRenderPipeline]
    public sealed class VolumeProfileEditor : Editor
    {
        /// <summary>
        /// The VolumeComponentListEditor for this Volume Profile editor.
        /// </summary>
        public VolumeComponentListEditor componentList { get; private set; }

        void OnEnable()
        {
            componentList = new VolumeComponentListEditor(this);
            if (VolumeManager.instance.isInitialized)
                Init();
        }

        void Init()
        {
            var volumeProfile = target as VolumeProfile;

            if (volumeProfile == VolumeManager.instance.globalDefaultProfile)
            {
                componentList.SetIsGlobalDefaultVolumeProfile(true);
                VolumeProfileUtils.EnsureAllOverridesForDefaultProfile(volumeProfile);
            }

            componentList.Init(volumeProfile, serializedObject);
        }

        void OnDisable()
        {
            componentList?.Clear();
        }

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            if (componentList == null)
            {
                if (!VolumeManager.instance.isInitialized)
                    return; // Defer initialization until VolumeManager is initialized

                Init();
            }

            serializedObject.Update();
            componentList.OnGUI();

            EditorGUILayout.Space();
            if (componentList.hasHiddenVolumeComponents)
                EditorGUILayout.HelpBox("There are Volume Components that are hidden in this asset because they are incompatible with the current active Render Pipeline. Change the active Render Pipeline to see them.", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
