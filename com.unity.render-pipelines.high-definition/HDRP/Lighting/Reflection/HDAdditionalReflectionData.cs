using UnityEngine.Serialization;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEngine.Experimental.Rendering
{
    [RequireComponent(typeof(ReflectionProbe), typeof(MeshFilter), typeof(MeshRenderer))]
    public class HDAdditionalReflectionData : MonoBehaviour
    {
        [HideInInspector]
        public float version = 1.0f;

        public ShapeType influenceShape;
        [FormerlySerializedAsAttribute("dimmer")]
        public float multiplier = 1.0f;
        [Range(0.0f, 1.0f)]
        public float weight = 1.0f;
        public float influenceSphereRadius = 3.0f;
        public float sphereReprojectionVolumeRadius = 1.0f;
        public bool useSeparateProjectionVolume = false;
        public Vector3 boxReprojectionVolumeSize = Vector3.one;
        public Vector3 boxReprojectionVolumeCenter = Vector3.zero;
        public float maxSearchDistance = 8.0f;
        public Texture previewCubemap;
        public Vector3 blendDistancePositive = Vector3.zero;
        public Vector3 blendDistanceNegative = Vector3.zero;
        public Vector3 blendNormalDistancePositive = Vector3.zero;
        public Vector3 blendNormalDistanceNegative = Vector3.zero;
        public Vector3 boxSideFadePositive = Vector3.one;
        public Vector3 boxSideFadeNegative = Vector3.one;

        //editor value that need to be saved for easy passing from simplified to advanced and vice et versa
        // /!\ must not be used outside editor code
        [SerializeField] private Vector3 editorAdvancedModeBlendDistancePositive;
        [SerializeField] private Vector3 editorAdvancedModeBlendDistanceNegative;
        [SerializeField] private float editorSimplifiedModeBlendDistance;
        [SerializeField] private Vector3 editorAdvancedModeBlendNormalDistancePositive;
        [SerializeField] private Vector3 editorAdvancedModeBlendNormalDistanceNegative;
        [SerializeField] private float editorSimplifiedModeBlendNormalDistance;
        [SerializeField] private bool editorAdvancedModeEnabled;

        public ReflectionProxyVolumeComponent proxyVolumeComponent;

        public Vector3 boxBlendCenterOffset { get { return (blendDistanceNegative - blendDistancePositive) * 0.5f; } }
        public Vector3 boxBlendSizeOffset { get { return -(blendDistancePositive + blendDistanceNegative); } }
        public Vector3 boxBlendNormalCenterOffset { get { return (blendNormalDistanceNegative - blendNormalDistancePositive) * 0.5f; } }
        public Vector3 boxBlendNormalSizeOffset { get { return -(blendNormalDistancePositive + blendNormalDistanceNegative); } }


        public float sphereBlendRadiusOffset { get { return -blendDistancePositive.x; } }
        public float sphereBlendNormalRadiusOffset { get { return -blendNormalDistancePositive.x; } }
    }
}
