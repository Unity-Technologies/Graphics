
namespace UnityEngine
{
    /// <summary>
    /// SRPLensFlareOverride allow the GameObject to emit a LensFlare
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu("Rendering/SRP Lens Flare Source Override")]
    public sealed class SRPLensFlareOverride : MonoBehaviour
    {
        /// <summary>
        /// Lens flare asset used on this component
        /// </summary>
        public SRPLensFlareData lensFlareData = null;
        /// <summary>
        /// Intensity
        /// </summary>
        public float intensity = 1.0f;
        /// <summary>
        /// Attenuation by distance, uses world space values
        /// </summary>
        public AnimationCurve distanceAttenuationCurve = new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(10.0f, 0.0f));
        /// <summary>
        /// If component attached to a light, attenuation the lens flare per light type
        /// </summary>
        public bool attenuationByLightShape = true;
        /// <summary>
        /// Attenuation used radially, which allow for instance to enable flare only on the edge of the screen
        /// </summary>
        public AnimationCurve radialScreenAttenuationCurve = new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f));

        /// <summary>
        /// Radius around the light used to occlude the flare (value in world space)
        /// </summary>
        [Min(0)]
        public float occlusionRadius = 0.01f;
        /// <summary>
        /// Random Samples Count used inside the disk with 'occlusionRadius'
        /// </summary>
        [Range(0, 64)]
        public uint sampleCount = 4;
        /// <summary>
        /// Z Occlusion Offset allow us to offset the plane where the disc of occlusion is place (closer to camera), value on world space.
        /// Useful for instance to sample occlusion outside a light bulb if we place a flare inside the light bulb
        /// </summary>
        public float occlusionOffset = 0.0f;
        /// <summary>
        /// If allowOffScreen is true then If the lens flare is outside the screen we still emit the flare on screen
        /// </summary>
        public bool allowOffScreen = false;

        /// <summary>
        /// Add or remove the lens flare to the queue of PostProcess
        /// </summary>
        public void OnEnable()
        {
            if (lensFlareData && (gameObject.activeInHierarchy || gameObject.activeSelf))
                SRPLensFlareCommon.Instance.AddData(this);
            else
                SRPLensFlareCommon.Instance.RemoveData(this);
        }

        /// <summary>
        /// Remove the lens flare to the queue of PostProcess
        /// </summary>
        public void OnDisable()
        {
            if (lensFlareData)
                SRPLensFlareCommon.Instance.RemoveData(this);
        }

        /// <summary>
        /// Remove the lens flare to the queue of PostProcess
        /// </summary>
        public void OnDestroy()
        {
            if (lensFlareData)
                SRPLensFlareCommon.Instance.RemoveData(this);
        }

        /// <summary>
        /// Add or remove the lens flare to the queue of PostProcess
        /// </summary>
        public void Start()
        {
            if (lensFlareData != null && (gameObject.activeInHierarchy || gameObject.activeSelf))
            {
                SRPLensFlareCommon.Instance.AddData(this);
            }
            else
            {
                SRPLensFlareCommon.Instance.RemoveData(this);
            }
        }

        /// <summary>
        /// Add or remove the lens flare to the queue of PostProcess
        /// </summary>
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

        /// <summary>
        /// Add or remove the lens flare to the queue of PostProcess
        /// </summary>
        public void Update()
        {
            if (lensFlareData == null)
            {
                SRPLensFlareCommon.Instance.RemoveData(this);
            }
        }
    }
}
