using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the Screen Space Refraction effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Lighting/Screen Space Refraction")]
    public class ScreenSpaceRefraction : VolumeComponent
    {
        internal enum RefractionModel
        {
            None = 0,
            Box = 1,
            Sphere = 2,
            Thin = 3
        };

        int m_InvScreenFadeDistanceID;

        /// <summary>
        /// Controls the distance at which HDRP fades out Screen Space Refraction near the edge of the screen. A value near 0 indicates a small fade distance at the edges,
        /// while increasing the value towards one will start the fade closer to the center of the screen.
        /// </summary>
        public ClampedFloatParameter screenFadeDistance = new ClampedFloatParameter(0.1f, 0.001f, 1.0f);

        static ScreenSpaceRefraction s_Default = null;

        internal static ScreenSpaceRefraction defaultInstance
        {
            get
            {
                if (s_Default == null)
                {
                    s_Default = ScriptableObject.CreateInstance<ScreenSpaceRefraction>();
                    s_Default.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_Default;
            }
        }

        internal virtual void PushShaderParameters(CommandBuffer cmd)
        {
            cmd.SetGlobalFloat(m_InvScreenFadeDistanceID, 1.0f / screenFadeDistance.value);
        }

        void FetchIDs(
            out int invScreenWeightDistanceID)
        {
            invScreenWeightDistanceID = HDShaderIDs._SSRefractionInvScreenWeightDistance;
        }

        void Awake()
        {
            FetchIDs(
                out m_InvScreenFadeDistanceID
                );
        }

    }
}
