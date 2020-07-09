using System.Runtime.CompilerServices;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    internal abstract class VolumeComponentWithQualityEditor : VolumeComponentEditor
    {
        // Quality settings
        SerializedDataParameter m_QualitySetting;

        // An opaque binary blob storing preset settings (used to remember what were the last custom settings that were used).
        internal abstract class QualitySettingsBlob
        {
            public bool[] overrideState;

            protected QualitySettingsBlob(int overrideCount)
            {
                overrideState = new bool[overrideCount];
            }

            protected static bool IsEqual (QualitySettingsBlob left, QualitySettingsBlob right)
            {
                if ((right == null && left != null) || (right != null && left == null))
                {
                    return false;
                }

                if (right == null && left == null)
                {
                    return true;
                }

                for (int i = 0; i < left.overrideState.Length; ++i)
                {
                    if (left.overrideState[i] != right.overrideState[i])
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        // Note: Editors are refreshed on gui changes by the volume system, so any state that we want to store here needs to be a static (or in a serialized variable)
        // We use ConditionalWeakTable instead of a Dictionary of InstanceIDs to get automatic clean-up of dead entries in the table
        static ConditionalWeakTable<UnityEngine.Object, QualitySettingsBlob> s_CustomSettingsHistory = new ConditionalWeakTable<UnityEngine.Object, QualitySettingsBlob>();

        static readonly int k_CustomQuality = ScalableSettingLevelParameter.LevelCount;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<VolumeComponentWithQuality>(serializedObject);
            m_QualitySetting = Unpack(o.Find(x => x.quality));
        }

        public override void OnInspectorGUI()
        {
            int prevQualityLevel = m_QualitySetting.value.intValue;

            EditorGUI.BeginChangeCheck();
            PropertyField(m_QualitySetting);

            // When a quality preset changes, we want to detect and reflect the settings in the UI. PropertyFields mirror the contents of one memory loccation, so
            // the idea is that we copy the presets to that location. This logic is optional, if volume components don't override the helper functions at the end,
            // they will continue to work, but the preset settings will not be reflected in the UI.
            if (EditorGUI.EndChangeCheck())
            {
                int newQualityLevel = m_QualitySetting.value.intValue;

                if (newQualityLevel == k_CustomQuality)
                {
                    // If we have switched to custom quality from a preset, then load the last custom quality settings the user has used in this volume
                    if (prevQualityLevel != k_CustomQuality)
                    {
                        QualitySettingsBlob history = null;
                        s_CustomSettingsHistory.TryGetValue(serializedObject.targetObject, out history);
                        if (history != null)
                        {
                            LoadSettingsFromObject(history);
                        }
                    }
                }
                else
                {
                    // If we are going to use a quality preset, then load the preset values so they are reflected in the UI
                    var pipeline = (HDRenderPipeline)RenderPipelineManager.currentPipeline;
                    if (pipeline != null)
                    {
                        // If we switch from a custom quality level, then save these values so we can re-use them if teh user switches back
                        if (prevQualityLevel == k_CustomQuality)
                        {
                            QualitySettingsBlob history = null;
                            s_CustomSettingsHistory.TryGetValue(serializedObject.targetObject, out history);
                            if (history != null)
                            {
                                 SaveCustomQualitySettingsAsObject(history);
                            }
                            else
                            {
                                // Only keep track of custom settings for components that implement the new interface (and return not null)
                                history = SaveCustomQualitySettingsAsObject();
                                if (history != null)
                                {   
                                    s_CustomSettingsHistory.Add(serializedObject.targetObject, history);
                                }

                            }
                        }
                        LoadSettingsFromQualityPreset(pipeline.currentPlatformRenderPipelineSettings, newQualityLevel);
                    }
                }
            }
        }

        protected bool useCustomValue => m_QualitySetting.value.intValue == k_CustomQuality;
        protected bool overrideState => m_QualitySetting.overrideState.boolValue;

        /// <summary>
        /// This should be called after the user manually edits a quality setting that appears in a preset. After calling this function, the quality preset will change to Custom.
        /// </summary>
        public void QualitySettingsWereChanged() { m_QualitySetting.value.intValue = k_CustomQuality; }

        /// <summary>
        /// This function should be overriden by a volume component to load preset settings from RenderPipelineSettings
        /// </summary>
        public virtual void LoadSettingsFromQualityPreset(RenderPipelineSettings settings, int level) { }

        /// <summary>
        /// This function should be overriden by a volume component to return an opaque object (binary blob) with the custom quality settings currently in use.
        /// </summary>
        public virtual QualitySettingsBlob SaveCustomQualitySettingsAsObject(QualitySettingsBlob history = null) { return null; }

        /// <summary>
        /// This function should be overriden by a volume component to load a custom preset setting from an opaque binary blob (as returned from SaveCustomQualitySettingsAsObject)
        /// </summary>
        public virtual void LoadSettingsFromObject(QualitySettingsBlob settings) { }

    }

}
