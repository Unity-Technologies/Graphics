#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine
{
    /// <summary>
    /// Lens Flare Data-Driven which can but added on any GameObject: SRPLensFlareOverride allow the GameObject to emit a LensFlare
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu("Rendering/SRP Lens Flare Source Override")]
    [HelpURL(Rendering.Documentation.baseURL + Rendering.Documentation.version + Rendering.Documentation.subURL + "Common/srp-lens-flare-component" + Rendering.Documentation.endURL)]
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
        /// Distance used to scale the Distance Attenuation Curve
        /// </summary>
        [Min(1e-5f)]
        public float maxAttenuationDistance = 50.0f;
        /// <summary>
        /// Distance used to scale the Scale Attenuation Curve
        /// </summary>
        [Min(1e-5f)]
        public float maxAttenuationScale = 50.0f;
        /// <summary>
        /// Attenuation by distance
        /// </summary>
        public AnimationCurve distanceAttenuationCurve = new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 0.0f));
        /// <summary>
        /// Scale by distance, use the same distance as distanceAttenuationCurve
        /// </summary>
        public AnimationCurve scaleByDistanceCurve = new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 0.0f));
        /// <summary>
        /// If component attached to a light, attenuation the lens flare per light type
        /// </summary>
        public bool attenuationByLightShape = true;
        /// <summary>
        /// Attenuation used radially, which allow for instance to enable flare only on the edge of the screen
        /// </summary>
        public AnimationCurve radialScreenAttenuationCurve = new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f));

        /// <summary>
        /// Enable Occlusion feature
        /// </summary>
        public bool useOcclusion = true;
        /// <summary>
        /// Radius around the light used to occlude the flare (value in world space)
        /// </summary>
        [Min(0)]
        public float occlusionRadius = 0.1f;
        /// <summary>
        /// Random Samples Count used inside the disk with 'occlusionRadius'
        /// </summary>
        [Range(0, 64)]
        public uint sampleCount = 32;
        /// <summary>
        /// Z Occlusion Offset allow us to offset the plane where the disc of occlusion is place (closer to camera), value on world space.
        /// Useful for instance to sample occlusion outside a light bulb if we place a flare inside the light bulb
        /// </summary>
        public float occlusionOffset = 0.05f;
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

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Camera mainCam = Camera.current;
            if (mainCam != null)
            {
                Color previousH = Handles.color;
                Color previousG = Gizmos.color;
                Handles.color = Color.red;
                Gizmos.color = Color.red;
                Vector3 dir = (mainCam.transform.position - transform.position).normalized;
                Handles.DrawWireDisc(transform.position + dir * occlusionOffset, dir, occlusionRadius, 1.0f);
                Gizmos.DrawWireSphere(transform.position, occlusionOffset);
                Gizmos.color = previousG;
                Handles.color = previousH;
            }
        }

#endif
    }
}
