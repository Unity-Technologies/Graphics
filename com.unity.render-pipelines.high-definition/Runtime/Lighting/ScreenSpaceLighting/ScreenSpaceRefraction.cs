using System;

namespace UnityEngine.Rendering.HighDefinition
{

    [Serializable, VolumeComponentMenu("Lighting/Screen Space Refraction")]
    public class ScreenSpaceRefraction : VolumeComponent
    {
        public enum RefractionModel
        {
            None = 0,
            Box = 1,
            Sphere = 2,
            Thin = 3
        };

        int m_InvScreenFadeDistanceID;

        public ClampedFloatParameter screenFadeDistance = new ClampedFloatParameter(0.1f, 0.001f, 1.0f);

        static ScreenSpaceRefraction s_Default = null;

        [Obsolete("Since 2019.3, use ScreenSpaceRefraction.DefaultInstance instead.")]
        public static readonly ScreenSpaceRefraction @default = default;
        public static ScreenSpaceRefraction defaultInstance
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

        public virtual void PushShaderParameters(CommandBuffer cmd)
        {
            cmd.SetGlobalFloat(m_InvScreenFadeDistanceID, 1.0f / screenFadeDistance.value);
        }

        protected void FetchIDs(
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
