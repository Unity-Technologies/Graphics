using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Contains a double list of <see cref="IRenderPipelineGraphicsSettings"/> one is used for editor
    /// and the other for standalone release, the standalone release will be stripped by <see cref="IRenderPipelineGraphicsSettingsStripper{T}"/>
    /// </summary>
    [Serializable]
    public class RenderPipelineGraphicsSettingsContainer : ISerializationCallbackReceiver
    {
#if UNITY_EDITOR
        [SerializeField] private RenderPipelineGraphicsSettingsCollection m_SettingsList = new();
#endif

        [SerializeField, HideInInspector] private RenderPipelineGraphicsSettingsCollection m_RuntimeSettings = new();

        /// <summary>
        /// Returns one list for editor and another for runtime
        /// </summary>
        public List<IRenderPipelineGraphicsSettings> settingsList
        {
#if UNITY_EDITOR
            get => m_SettingsList.settingsList;
#else
            get => m_RuntimeSettings.settingsList;
#endif
        }

        /// <summary>
        /// On Before Serialize callback where the stripping is performed
        /// </summary>
        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            m_RuntimeSettings.settingsList.Clear();
            if (BuildPipeline.isBuildingPlayer) // Same behaviour as transfer.IsSerializingForGameRelease
                RenderPipelineGraphicsSettingsStripper.PerformStripping(m_SettingsList.settingsList, m_RuntimeSettings.settingsList);
#endif
        }

        /// <summary>
        /// On After Deserialize callback, nothing is implemented
        /// </summary>
        public void OnAfterDeserialize()
        {
#if UNITY_EDITOR
            m_RuntimeSettings.settingsList.Clear();
#endif
        }
    }
}
