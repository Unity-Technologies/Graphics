
namespace UnityEngine
{
    [ExecuteInEditMode]
    [AddComponentMenu("Rendering/SRP Lens Flare Source Override")]
    public sealed class SRPLensFlareOverride : MonoBehaviour
    {
        public SRPLensFlareData lensFlareData = null;
        public bool attenuationByLight = true;
        public bool allowOffScreen = false;
        [Min(0)]
        public float occlusionRadius = 0.01f;
        [Min(1)]
        public uint samplesCount = 4;
        public AnimationCurve radialAttenuationCurve = new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f));
        public AnimationCurve distanceAttenuationCurve = new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(10.0f, 0.0f));
        public float attenuation = 1.0f;

        public SRPLensFlareOverride()
        {
        }

        public void OnEnable()
        {
            if (lensFlareData && gameObject.active)
                SRPLensFlareCommon.Instance.AddData(this);
            else
                SRPLensFlareCommon.Instance.RemoveData(this);
        }

        public void OnDisable()
        {
            if (lensFlareData)
                SRPLensFlareCommon.Instance.RemoveData(this);
        }

        public void OnDestroy()
        {
            if (lensFlareData)
                SRPLensFlareCommon.Instance.RemoveData(this);
        }

        public void Start()
        {
            if (lensFlareData != null && gameObject.active)
            {
                SRPLensFlareCommon.Instance.AddData(this);
            }
            else
            {
                SRPLensFlareCommon.Instance.RemoveData(this);
            }
        }

        public void OnValidate()
        {
            if (lensFlareData != null)
            {
                SRPLensFlareCommon.Instance.AddData(this);
            }
            else
            {
                SRPLensFlareCommon.Instance.RemoveData(this);
            }
        }

        public void Update()
        {
            if (lensFlareData == null)
            {
                SRPLensFlareCommon.Instance.RemoveData(this);
            }
        }
    }
}
