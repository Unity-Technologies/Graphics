using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Component containing additional mesh rendering features for HDRP.
    /// </summary>
    [AddComponentMenu("Mesh/Mesh Renderer Extension")]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    [ExecuteAlways]
    public class HDAdditionalMeshRendererSettings : MonoBehaviour, ILineRenderer
    {
        private MeshRenderer m_MeshRenderer;
        private MeshFilter   m_MeshFilter;

        // This is guaranteed to be the identifier since it is what the sub target will emit.
        private const string kVertexSetupComputeAssetIdentifier = "VertexSetup";
        private const string kOffscreenShadingPassIdentifier    = "LineRenderingOffscreenShading";

        [SerializeField]
        private bool m_EnableHighQualityLineRendering;

        /// <summary>
        /// Set the enablement of high quality line rendering for this mesh renderer.
        /// </summary>
        public bool enableHighQualityLineRendering
        {
            get
            {
                return m_EnableHighQualityLineRendering;
            }
            set
            {
                m_EnableHighQualityLineRendering = value;
                OnValidate();
            }
        }

        [SerializeField]
        private LineRendering.RendererGroup m_RendererGroup = LineRendering.RendererGroup.None;

        /// <summary>
        /// Sets the high quality line rendering merge group for this mesh renderer.
        /// </summary>
        public LineRendering.RendererGroup rendererGroup
        {
            get
            {
                return m_RendererGroup;
            }
            set
            {
                m_RendererGroup = value;
            }
        }

        [SerializeField]
        private LineRendering.RendererLODMode m_RendererLODMode = LineRendering.RendererLODMode.None;

        /// <summary>
        /// Sets the high quality line rendering level of detail mode for this mesh renderer.
        /// </summary>
        public LineRendering.RendererLODMode rendererLODMode
        {
            get
            {
                return m_RendererLODMode;
            }
            set
            {
                m_RendererLODMode = value;
            }
        }

        [SerializeField, Range(0f, 1f)]
        private float m_RendererLODFixed = 1.0f;

        /// <summary>
        /// Sets the high quality line rendering fixed level of detail for this mesh renderer.
        /// </summary>
        public float rendererLODFixed
        {
            get
            {
                return m_RendererLODFixed;
            }
            set
            {
                m_RendererLODFixed = value;
            }
        }

        [SerializeField]
        private AnimationCurve m_RendererLODCameraDistanceCurve = AnimationCurve.EaseInOut(1f, 1f, 10f, 0.05f);

        /// <summary>
        /// Sets the high quality line rendering level of detail curve for this mesh renderer.
        /// </summary>
        public AnimationCurve rendererLODCameraDistanceCurve
        {
            get
            {
                return m_RendererLODCameraDistanceCurve;
            }
            set
            {
                m_RendererLODCameraDistanceCurve = value;
            }
        }

        [FormerlySerializedAs("m_RendererLODCameraCoverageCurve")] [SerializeField]
        private AnimationCurve m_RendererLODScreenCoverageCurve = AnimationCurve.EaseInOut(0f, 0.01f, 1f, 1.0f);

        /// <summary>
        /// Sets the high quality line rendering level of detail curve for this mesh renderer.
        /// </summary>
        public AnimationCurve rendererLODScreenCoverageCurve
        {
            get
            {
                return m_RendererLODScreenCoverageCurve;
            }
            set
            {
                m_RendererLODScreenCoverageCurve = value;
            }
        }

        [SerializeField, Range(0.001f, 1f)]
        private float m_ShadingSampleFraction = 1.0f;

        /// <summary>
        /// Sets the high quality line rendering shading rate for this mesh renderer.
        /// </summary>
        public float shadingSampleFraction
        {
            get
            {
                return m_ShadingSampleFraction;
            }
            set
            {
                m_ShadingSampleFraction = value;
            }
        }

        [SerializeField]
        private uint m_IndexCount;

        [SerializeField]
        private uint m_SegmentsPerLine;

        [SerializeField]
        private uint m_LineCount;

        [SerializeField]
        private ComputeShader m_VertexSetupCompute;

        [NonSerialized]
        internal Matrix4x4 m_PreviousLocalToWorldMatrix = Matrix4x4.identity;

        [NonSerialized]
        private Material m_OldMaterial = null;

        [NonSerialized]
        private GraphicsBuffer m_LODBuffer;

        [NonSerialized] private GraphicsBuffer m_IndexBuffer;

        private bool TryGetDependentComponents() => TryGetComponent(out m_MeshRenderer) && TryGetComponent(out m_MeshFilter);
        private bool ComponentsValid() => m_MeshRenderer != null && m_MeshFilter != null;

        private void ForceSetBaseMeshRendererPasses(bool enabled)
        {
            if (!ComponentsValid())
                return;

            if (m_MeshRenderer.sharedMaterial == null)
                return;

            // TODO: Currently the material inspector overrides this in the editor...

            // Use render queue mechanism to disable the normal depth/motion/color passes.
            // This is a more reliable way than Material.SetShaderPassEnabled which seems to not work for motion vectors.
            // We do it this way instead of overriding MeshRenderer.shadowCastingMode to ShadowsOnly for less confusing UI.
            m_MeshRenderer.sharedMaterial.renderQueue = !enabled ? (int)HDRenderQueue.Priority.LineRendering : -1;
        }

        private static bool CameraSupportsLineRendering(Camera camera)
        {
            return HDRenderPipeline.LineRenderingIsEnabled(HDCamera.GetOrCreate(camera), out var unused1);
        }

        private void SetLineRenderingEnabled(bool enabled)
        {
            if (enabled)
            {
                LineRendering.AddRenderer(this);
                ForceSetBaseMeshRendererPasses(false);
            }
            else
            {
                LineRendering.RemoveRenderer(this);
                ForceSetBaseMeshRendererPasses(true);
            }
        }

        private void CallbackUpdatePreviousLocalToWorldMatrix(ScriptableRenderContext unused0, List<Camera> unused1) => m_PreviousLocalToWorldMatrix = transform.localToWorldMatrix;

        void CallbackForceRendererDisable(ScriptableRenderContext unused0, Camera camera)
        {
            if (!m_EnableHighQualityLineRendering)
                return;

            if (!CameraSupportsLineRendering(camera))
            {
                ForceSetBaseMeshRendererPasses(true);
            }
        }

        void CallbackForceRendererEnable(ScriptableRenderContext unused0, Camera camera)
        {
            if (!m_EnableHighQualityLineRendering)
                return;

            if (!CameraSupportsLineRendering(camera))
            {
                ForceSetBaseMeshRendererPasses(false);
            }
        }

        private void RegisterCallbacks()
        {
            RenderPipelineManager.beginCameraRendering += CallbackForceRendererDisable;
            RenderPipelineManager.endCameraRendering   += CallbackForceRendererEnable;
            RenderPipelineManager.endContextRendering  += CallbackUpdatePreviousLocalToWorldMatrix;
        }

        private void DeRegisterCallbacks()
        {
            RenderPipelineManager.beginCameraRendering -= CallbackForceRendererDisable;
            RenderPipelineManager.endCameraRendering   -= CallbackForceRendererEnable;
            RenderPipelineManager.endContextRendering  -= CallbackUpdatePreviousLocalToWorldMatrix;
        }

        private void OnEnable()
        {
        #if UNITY_EDITOR
            // Need to do this here for similar reasons as we do in OnValidate.
            s_FirstOnValidateCalled = false;
        #endif

            RegisterCallbacks();
            TryGetDependentComponents();

            if (m_EnableHighQualityLineRendering)
            {
                SetLineRenderingEnabled(true);
            }
        }

        private void OnDisable()
        {
            DeRegisterCallbacks();

            if (m_EnableHighQualityLineRendering)
            {
                CoreUtils.SafeRelease(m_LODBuffer);
                m_LODBuffer = null;

                CoreUtils.SafeRelease(m_IndexBuffer);
                m_IndexBuffer = null;

                SetLineRenderingEnabled(false);
            }
        }

        private void OnDestroy()
        {
            DeRegisterCallbacks();

            if (m_EnableHighQualityLineRendering)
            {
                CoreUtils.SafeRelease(m_LODBuffer);
                m_LODBuffer = null;

                CoreUtils.SafeRelease(m_IndexBuffer);
                m_IndexBuffer = null;

                SetLineRenderingEnabled(false);
            }
        }

        private void LateUpdate()
        {
            if (m_EnableHighQualityLineRendering)
            {
                if (LineRendererIsValid())
                {
                    ForceSetBaseMeshRendererPasses(false);
                }
                else
                {
                    ForceSetBaseMeshRendererPasses(true);
                }
            }
        }

#if UNITY_EDITOR
        // We need this editor workaround to skip nullifying the compute asset during the import OnValidate,
        // because the AssetDatabase is not set up yet and IsLineRenderingMaterial will always fail.
        [NonSerialized]
        private bool s_FirstOnValidateCalled = false;
#endif

        internal void OnValidate()
        {
            if (!ComponentsValid())
                return;

            if (!m_EnableHighQualityLineRendering)
            {
                SetLineRenderingEnabled(false);
                return;
            }

            SetLineRenderingEnabled(true);

            var material = m_MeshRenderer.sharedMaterial;

            if (m_OldMaterial != material)
            {
#if UNITY_EDITOR
                if (material != null)
                {
                    if (s_FirstOnValidateCalled && !IsLineRenderingMaterial(material))
                    {
                        m_VertexSetupCompute = null;
                    }
                    else if (material.shader != null)
                    {
                        var shaderPath = AssetDatabase.GetAssetPath(material.shader);
                        var shaderAssets = AssetDatabase.LoadAllAssetsAtPath(shaderPath);

                        foreach (var asset in shaderAssets)
                        {
                            if (asset is ComputeShader shader)
                            {
                                if (shader.name.Contains(kVertexSetupComputeAssetIdentifier))
                                    m_VertexSetupCompute = shader;
                            }
                        }
                    }
                }
                if (!s_FirstOnValidateCalled)
                    s_FirstOnValidateCalled = true;
#else
                if (!GraphicsSettings.TryGetRenderPipelineSettings<HDRenderPipelineRuntimeAssets>(out var assets))
                {
                    Debug.LogError("Failed to load runtime resources.");
                    return;
                }

                if (!assets.computeMaterialLibrary)
                {
                    Debug.LogError("Failed to load compute material library.");
                    return;
                }

                if (!assets.computeMaterialLibrary.Get(material.shader, out m_VertexSetupCompute))
                {
                    Debug.LogError("Failed to load compute material for the given shader.");
                    return;
                }
#endif
                m_OldMaterial = material;
            }
        }

        internal static bool IsLineRenderingMaterial(Material mat)
        {
#if UNITY_EDITOR
            // Try to get the compute shader.
            if (mat != null && mat.shader != null)
            {
                var shaderPath = AssetDatabase.GetAssetPath(mat.shader);
                var shaderAssets = AssetDatabase.LoadAllAssetsAtPath(shaderPath);

                var materialHasRequiredComputeAssets = true;
                {
                    materialHasRequiredComputeAssets &= shaderAssets.Any(o => o.name.Contains(kVertexSetupComputeAssetIdentifier));
                }
                return materialHasRequiredComputeAssets && mat.FindPass(kOffscreenShadingPassIdentifier) != -1;
            }

            return false;
#else
            return false;
#endif
        }

        internal SphericalHarmonicsL2 GetLightProbe()
        {
            if (m_MeshRenderer.lightProbeUsage == LightProbeUsage.CustomProvided)
                return new SphericalHarmonicsL2(); // TODO

            SphericalHarmonicsL2 sh;
            {
                if (m_MeshRenderer.lightProbeUsage == LightProbeUsage.BlendProbes)
                {
                    // Use the supporting renderer to fetch the light probe.
                    // (Can be null renderer too, but provided one speeds up the search).
                    var position = m_MeshRenderer.probeAnchor ? m_MeshRenderer.probeAnchor.position : transform.position;
                    LightProbes.GetInterpolatedProbe(position, m_MeshRenderer, out sh);
                }
                else
                {
                    sh = new SphericalHarmonicsL2();
                    sh.AddAmbientLight(Color.black);
                }
            }
            return sh;
        }

        internal void ComputeLODDataIfNeeded()
        {
            var mesh = m_MeshFilter.sharedMesh;

            if (mesh == null)
                return;

            var currentIndexCount = mesh.GetIndexCount(0);

            // TODO: More than one sub-mesh.
            if (m_IndexCount == currentIndexCount && m_LODBuffer != null)
                return;

            // Compute # segments-per-line.
            m_SegmentsPerLine = 0;
            {
                var meshIndices = mesh.GetIndices(0);

                for (uint i = 0; i < meshIndices.Length; i += 2u)
                {
                    if (m_SegmentsPerLine != meshIndices[i])
                        break;

                    m_SegmentsPerLine++;
                }
            }

            // Compute # lines.
            m_LineCount = (currentIndexCount / 2) / m_SegmentsPerLine;

            // Create the LOD buffer.
            {
                CoreUtils.SafeRelease(m_LODBuffer);

                // TODO: Make this faster.

                var nums = Enumerable.Range(0, (int)m_LineCount).ToArray();

                // Hard-coded seed for stable CI.
                var rnd = new System.Random(1337);

                for (int i = 0;i < nums.Length;++i)
                {
                    int randomIndex = rnd.Next(nums.Length);
                    (nums[randomIndex], nums[i]) = (nums[i], nums[randomIndex]);
                }

                m_LODBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, (int)m_LineCount, sizeof(uint));
                m_LODBuffer.SetData(nums);
            }

            m_IndexCount = currentIndexCount;
        }

        /// <summary>
        /// Determines if this mesh renderer is valid for high quality line rendering.
        /// </summary>
        /// <returns></returns>
        public bool LineRendererIsValid()
        {
            if (!ComponentsValid())
                return false;

            var mesh     = m_MeshFilter.sharedMesh;
            var material = m_MeshRenderer.sharedMaterial;

            if (material == null || mesh == null || m_VertexSetupCompute == null)
                return false;

            if (m_MeshRenderer.shadowCastingMode == ShadowCastingMode.ShadowsOnly)
                return false;

            return mesh.GetTopology(0) == MeshTopology.Lines && material.FindPass(kOffscreenShadingPassIdentifier) != -1;
        }

        /// <summary>
        /// Extracts the required rendering data for line rendering.
        /// </summary>
        /// <param name="renderGraph">Render Graph</param>
        /// <param name="camera">The camera for which the line rendering is being gathered.</param>
        /// <returns></returns>
        public LineRendering.RendererData GetLineRendererData(RenderGraph renderGraph, Camera camera)
        {
            CoreUtils.SafeRelease(m_IndexBuffer);

            // Required for binding internal buffer resources of the mesh in the rasterization.
            m_MeshFilter.sharedMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
            m_IndexBuffer = m_MeshFilter.sharedMesh.GetIndexBuffer();

            // Re-compute various data for LOD if there was a topological change to the mesh.
            ComputeLODDataIfNeeded();

            // Override
            var strandLOD = m_RendererLODFixed;

            if (m_RendererLODMode == LineRendering.RendererLODMode.CameraDistance && m_RendererLODCameraDistanceCurve != null)
            {
                var distanceToCamera = Vector3.Distance(camera.transform.position, m_MeshRenderer.bounds.center);
                strandLOD = m_RendererLODCameraDistanceCurve.Evaluate(distanceToCamera);
            }
            else if (m_RendererLODMode == LineRendering.RendererLODMode.ScreenCoverage)
            {
                Vector3 min = m_MeshRenderer.bounds.min;
                Vector3 max = m_MeshRenderer.bounds.max;

                var boundsCorners = new Vector3[]
                {
                    new (min.x, min.y, min.z),
                    new (max.x, min.y, min.z),
                    new (min.x, max.y, min.z),
                    new (max.x, max.y, min.z),
                    new (min.x, min.y, max.z),
                    new (max.x, min.y, max.z),
                    new (min.x, max.y, max.z),
                    new (max.x, max.y, max.z),
                };

                Vector2 ComputeScreenSpacePoint(Vector3 p)
                {
                    p = camera.WorldToScreenPoint(p);
                    return new Vector2(p.x, p.y);
                }

                var minP = Vector2.positiveInfinity;
                var maxP = Vector2.negativeInfinity;

                foreach (var corner in boundsCorners)
                {
                    var p = ComputeScreenSpacePoint(corner);

                    minP = Vector2.Min(p, minP);
                    maxP = Vector2.Max(p, maxP);
                }

                // Determine the area of the bounds in screen space.
                var length  = maxP.x - minP.x;
                var width   = maxP.y - minP.y;
                var area    = length * width;

                // Screen coverage is the ratio between projected bounds and viewport size.
                var screenCoverage = area / (Screen.width * Screen.height);

                // Need to implement our own smoothstep since Mathf.SmoothStep is not the same
                // behaviour as the HLSL intrinsic.
                float Smoothstep(float edge0, float edge1, float x)
                {
                    // Scale, bias and saturate x to 0..1 range
                    x = Mathf.Clamp01((x - edge0) / (edge1 - edge0));

                    // Evaluate polynomial
                    return x * x * (3 - 2 * x);
                }

                if (m_RendererLODScreenCoverageCurve != null)
                {
                    strandLOD = m_RendererLODScreenCoverageCurve.Evaluate(screenCoverage);
                }
                else
                {
                    // Remap the coverage s.t. we control when the min and max lod occur.
                    screenCoverage = Smoothstep(0.01f, 0.1f, screenCoverage);
                    // Minimum screen coverage LOD is 1% of strands.
                    strandLOD = Mathf.Lerp(0.1f, 1.0f, screenCoverage);
                }

            }

            return new LineRendering.RendererData
            {
                probe                = GetLightProbe(),
                mesh                 = m_MeshFilter.sharedMesh,
                matrixW              = transform.localToWorldMatrix,
                group                = m_RendererGroup,
                matrixWP             = m_PreviousLocalToWorldMatrix,
                material             = m_MeshRenderer.sharedMaterial,
                motionVectorParams   = new Vector4(0, m_MeshRenderer.motionVectorGenerationMode == MotionVectorGenerationMode.ForceNoMotion ? 0 : 1, 0, m_MeshRenderer.motionVectorGenerationMode == MotionVectorGenerationMode.Camera ? 1 : 0),
                vertexSetupCompute   = m_VertexSetupCompute,
                offscreenShadingPass = m_MeshRenderer.sharedMaterial.FindPass(HDShaderPassNames.s_LineRenderingOffscreenShading),
                renderingLayerMask   = m_MeshRenderer.renderingLayerMask,
                indexBuffer          = renderGraph.ImportBuffer(m_IndexBuffer),
                distanceToCamera     = Vector3.Distance(transform.position, camera.transform.position),
                bounds               = m_MeshRenderer.bounds,

                // LOD
                segmentsPerLine = (int)m_SegmentsPerLine,
                lineCount       = (int)m_LineCount,
                lodBuffer       = renderGraph.ImportBuffer(m_LODBuffer),
                lodMode         = m_RendererLODMode,
                lod             = strandLOD,
                shadingFraction = m_ShadingSampleFraction,
                hash            = GetInstanceID()
            };
        }
    }
}
