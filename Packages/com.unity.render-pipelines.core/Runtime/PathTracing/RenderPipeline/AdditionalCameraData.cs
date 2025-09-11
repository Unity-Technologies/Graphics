#if ENABLE_PATH_TRACING_SRP
using UnityEngine.Rendering.Denoising;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.LiveGI
{
    internal class AdditionalCameraData : MonoBehaviour
    {
        [Header("Viewport Settings")]
        [Range(50, 100)]
        public uint scale = 100;

        [HideInInspector]
        public Matrix4x4 previousViewProjection;

        [HideInInspector]
        public int frameIndex;

        [HideInInspector]
        public RTHandle rayTracingOutput = null;

        [HideInInspector]
        public RTHandle normals = null;

        [HideInInspector]
        public RTHandle motionVectors = null;

        [HideInInspector]
        public RTHandle debugOutput = null;

        public CommandBufferDenoiser denoiser = null;

        // TODO: Use 32-bit float formats because they are required by the neural network denoising backends.
        GraphicsFormat bufferFormat = GraphicsFormat.R32G32B32A32_SFloat;
        //GraphicsFormat bufferFormat = GraphicsFormat.R16G16B16A16_SFloat;

        // Start is called before the first frame update
        void Start()
        {
            frameIndex = 0;
            previousViewProjection = Matrix4x4.identity;
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void UpdateCameraDataPostRender(Camera camera)
        {
            previousViewProjection = camera.projectionMatrix * camera.worldToCameraMatrix;
            frameIndex++;
        }

        public void GetScaledViewport(Camera camera, out int scaledWidth, out int scaledHeight)
        {
            scaledWidth = Mathf.RoundToInt(camera.pixelWidth * scale / 100.0f);
            scaledHeight = Mathf.RoundToInt(camera.pixelHeight * scale / 100.0f);

            // Make sure the scaled viewport size is a multiple of 8
            //scaledWidth = (scaledWidth / 8) * 8;
            //scaledHeight = (scaledHeight / 8) * 8;
        }

        public void CreatePersistentResources(Camera camera, DenoiserType requestedDenoiser)
        {
            int scaledWidth = 0;
            int scaledHeight = 0;
            GetScaledViewport(camera, out scaledWidth, out scaledHeight);

            RTHandles.SetReferenceSize(scaledWidth, scaledHeight);

            if (rayTracingOutput == null)
            {
                RTHandles.ResetReferenceSize(scaledWidth, scaledHeight);
                rayTracingOutput    = RTHandles.Alloc(Vector2.one, 1, DepthBits.None, bufferFormat, FilterMode.Point, TextureWrapMode.Repeat, TextureDimension.Tex2D, true, name: "Path tracing render target");
                normals             = RTHandles.Alloc(Vector2.one, 1, DepthBits.None, bufferFormat, FilterMode.Point, TextureWrapMode.Repeat, TextureDimension.Tex2D, true, name: "Path tracing normal AOV");
                motionVectors       = RTHandles.Alloc(Vector2.one, 1, DepthBits.None, bufferFormat, FilterMode.Point, TextureWrapMode.Repeat, TextureDimension.Tex2D, true, name: "Path tracing motion vectors");
                debugOutput         = RTHandles.Alloc(Vector2.one, 1, DepthBits.None, bufferFormat, FilterMode.Point, TextureWrapMode.Repeat, TextureDimension.Tex2D, true, name: "Path tracing debug output");

                // when we (re)create the output buffer, reset the iteration for the camera
                frameIndex = 0;

                // for the first frame we don't have view projection history, so use current camera
                previousViewProjection = camera.projectionMatrix * camera.worldToCameraMatrix;
            }

            if (denoiser == null || denoiser.type != requestedDenoiser)
            {
                denoiser = new CommandBufferDenoiser();
            }

            denoiser.Init(requestedDenoiser, scaledWidth, scaledHeight);

        }

        public void ReleaseRTHandles()
        {
            if (rayTracingOutput != null)
            {
                rayTracingOutput.Release();
                rayTracingOutput = null;
            }

            if (normals != null)
            {
                normals.Release();
                normals = null;
            }

            if (motionVectors != null)
            {
                motionVectors.Release();
                motionVectors = null;
            }
        }

        void OnDestroy()
        {
            ReleaseRTHandles();
        }
    }
}


#endif
