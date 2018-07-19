using UnityEngine.Serialization;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    [RequireComponent(typeof(ReflectionProbe))]
    public class HDAdditionalReflectionData : HDProbe, ISerializationCallbackReceiver
    {
        const int currentVersion = 3;

        [SerializeField, FormerlySerializedAs("version")]
        int m_Version;

        ReflectionProbe m_LegacyProbe;
        ReflectionProbe legacyProbe { get { return m_LegacyProbe ?? (m_LegacyProbe = GetComponent<ReflectionProbe>()); } }

        //data only kept for migration, to be removed in future version
        [SerializeField, System.Obsolete("influenceShape is deprecated, use influenceVolume parameters instead")]
        Shape influenceShape;
        [SerializeField, System.Obsolete("influenceSphereRadius is deprecated, use influenceVolume parameters instead")]
        float influenceSphereRadius = 3.0f;
        [SerializeField, System.Obsolete("blendDistancePositive is deprecated, use influenceVolume parameters instead")]
        Vector3 blendDistancePositive = Vector3.zero;
        [SerializeField, System.Obsolete("blendDistanceNegative is deprecated, use influenceVolume parameters instead")]
        Vector3 blendDistanceNegative = Vector3.zero;
        [SerializeField, System.Obsolete("blendNormalDistancePositive is deprecated, use influenceVolume parameters instead")]
        Vector3 blendNormalDistancePositive = Vector3.zero;
        [SerializeField, System.Obsolete("blendNormalDistanceNegative is deprecated, use influenceVolume parameters instead")]
        Vector3 blendNormalDistanceNegative = Vector3.zero;
        [SerializeField, System.Obsolete("boxSideFadePositive is deprecated, use influenceVolume parameters instead")]
        Vector3 boxSideFadePositive = Vector3.one;
        [SerializeField, System.Obsolete("boxSideFadeNegative is deprecated, use influenceVolume parameters instead")]
        Vector3 boxSideFadeNegative = Vector3.one;

        bool needMigrateToHDProbeChild = false;

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            if (m_Version != currentVersion)
            {
                // Add here data migration code
                if (m_Version < 2)
                {
                    needMigrateToHDProbeChild = true;
                }
                else
                {
                    if (m_Version < 3)
                    {
                        MigrateToUseInfluanceVolume();
                    }
                    m_Version = currentVersion;
                }
            }
        }

        private void OnEnable()
        {
            if (needMigrateToHDProbeChild)
                MigrateToUseInfluanceVolume();
        }

        void MigrateToHDProbeChild()
        {
            mode = legacyProbe.mode;
            refreshMode = legacyProbe.refreshMode;
            m_Version = 2;
            OnAfterDeserialize();   //continue migrating if needed
        }

        void MigrateToUseInfluanceVolume()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            influenceVolume.shape = influenceShape;
            influenceVolume.sphereRadius = influenceSphereRadius;
            influenceVolume.boxBlendDistancePositive = blendDistancePositive;
            influenceVolume.boxBlendDistanceNegative = blendDistanceNegative;
            influenceVolume.boxBlendNormalDistancePositive = blendNormalDistancePositive;
            influenceVolume.boxBlendNormalDistanceNegative = blendNormalDistanceNegative;
            influenceVolume.boxSideFadePositive = boxSideFadePositive;
            influenceVolume.boxSideFadeNegative = boxSideFadeNegative;
#pragma warning restore CS0618 // Type or member is obsolete

            //Note: former editor parameters will be recreated as if non existent.
            //User will lose parameters corresponding to non used mode between simplified and advanced
        }

        public override ReflectionProbeMode mode
        {
            set
            {
                base.mode = value;
                legacyProbe.mode = value; //ensure compatibility till we capture without the legacy component
            }
        }

        public override ReflectionProbeRefreshMode refreshMode
        {
            set
            {
                base.refreshMode = value;
                legacyProbe.refreshMode = value; //ensure compatibility till we capture without the legacy component
            }
        }
    }
}
