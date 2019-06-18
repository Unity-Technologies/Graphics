using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class DefaultSettings
    {
        static DefaultSettings()
        {
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += () =>
            {
                if (s_DefaultVolume != null && !s_DefaultVolume.Equals(null))
                {
                    Object.Destroy(s_DefaultVolume.gameObject);
                    s_DefaultVolume = null;
                }
            };
#endif
        }

        private static Volume s_DefaultVolume = null;

        public const string defaultVolumeProfileFileStem = "DefaultSettingsVolumeProfile";

        public static VolumeProfile defaultVolumeProfile => Resources.Load<VolumeProfile>(defaultVolumeProfileFileStem);

        public static Volume GetOrCreateDefaultVolume()
        {
            if (s_DefaultVolume == null || s_DefaultVolume.Equals(null))
            {
                var go = new GameObject("Default Volume") { hideFlags = HideFlags.HideAndDontSave };
                s_DefaultVolume = go.AddComponent<Volume>();
                s_DefaultVolume.isGlobal = true;
                s_DefaultVolume.priority = float.MinValue;
                s_DefaultVolume.profile = DefaultSettings.defaultVolumeProfile;
            }
            if (
                // In case the asset was deleted or the reference removed
                s_DefaultVolume.profile == null || s_DefaultVolume.profile.Equals(null)
                #if UNITY_EDITOR
                // In case the serialization recreated an empty volume profile
                || !UnityEditor.AssetDatabase.Contains(s_DefaultVolume.profile)
                #endif
            )
                s_DefaultVolume.profile = DefaultSettings.defaultVolumeProfile;

            return s_DefaultVolume;
        }
    }
}
