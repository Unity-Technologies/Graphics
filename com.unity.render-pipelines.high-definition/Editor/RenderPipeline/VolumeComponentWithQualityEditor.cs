using System.Runtime.CompilerServices;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    internal abstract class VolumeComponentWithQualityEditor : VolumeComponentEditor
    {
        // Quality settings
        SerializedDataParameter m_QualitySetting;
        static ConditionalWeakTable<UnityEngine.Object, object> s_CustomSettingsHistory = new ConditionalWeakTable<UnityEngine.Object, object>();

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

            // When the quality changes, we want to detect and reflect the changes in the UI
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.Update();
                int newQualityLevel = m_QualitySetting.value.intValue;

                
                if (newQualityLevel == k_CustomQuality && prevQualityLevel != k_CustomQuality)
                {
                    // If custom quality was selected, then load the last custom quality settings the user has used in this volume
                    object history = null;
                    s_CustomSettingsHistory.TryGetValue(serializedObject.targetObject, out history);
                    if (history != null)
                    {
                        LoadSettingsFromObject(history);
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
                            int key = serializedObject.targetObject.GetInstanceID();
                            object history = null;
                            s_CustomSettingsHistory.TryGetValue(serializedObject.targetObject, out history);
                            if (history != null)
                            {
                                 SaveCustomQualitySettingsAsObject(history);
                            }
                            else
                            {
                                s_CustomSettingsHistory.Add(serializedObject.targetObject, SaveCustomQualitySettingsAsObject());
                            }
                        }
                        LoadSettingsFromQualityPreset(pipeline.currentPlatformRenderPipelineSettings, newQualityLevel);
                    }
                }
            }
        }

        protected bool useCustomValue => m_QualitySetting.value.intValue == k_CustomQuality;

        public void QualitySettingsWereChanged() { m_QualitySetting.value.intValue = k_CustomQuality; }
        public virtual void LoadSettingsFromQualityPreset(RenderPipelineSettings settings, int level) { }
        public virtual void LoadSettingsFromObject(object settings) { }
        public virtual object SaveCustomQualitySettingsAsObject(object history = null) { return null;}

    }

}
