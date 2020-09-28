using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal
{
    struct CachedSkyContext
    {
        public Type                 type;
        public SkyRenderingContext  renderingContext;
        public int                  hash;
        public int                  refCount;

        public void Reset()
        {
            // We keep around the renderer and the rendering context to avoid useless allocation if they get reused.
            hash = 0;
            refCount = 0;
            if (renderingContext != null)
                renderingContext.ClearAmbientProbe();
        }

        public void Cleanup()
        {
            Reset();

            if (renderingContext != null)
            {
                renderingContext.Cleanup();
                renderingContext = null;
            }
        }
    }

    class SkyManager
    {
        // TODO: Add to pipeline/renderer settings or update from RenderSettings.defaultReflectionResolution?
        const int k_Resolution = 128;

        Material m_StandardSkyboxMaterial; // This is the Unity standard skybox material. Used to pass the correct cubemap to Enlighten.
        Texture2D m_ConvolveGgxIblSampleData;
        Material m_ConvolveMaterial;
        SphericalHarmonicsL2 m_BlackAmbientProbe = new SphericalHarmonicsL2();
        RTHandle m_SkyboxBSDFCubemapIntermediate;
        Matrix4x4[] m_FaceWorldToViewMatrixMatrices = new Matrix4x4[6];
        Matrix4x4[] m_FacePixelCoordToViewDirMatrices = new Matrix4x4[6];

        Dictionary<Camera, SkyUpdateContext> m_Cameras = new Dictionary<Camera, SkyUpdateContext>();
        List<Camera> m_CamerasToCleanup = new List<Camera>(); // Recycled to reduce GC pressure

        private static readonly ProfilingSampler m_ProfilingSamplerSkyCubemap = new ProfilingSampler("Sky Cubemap");
        private static readonly ProfilingSampler m_ProfilingSamplerSkyConvolution = new ProfilingSampler("Sky Convolution");
        private static readonly ProfilingSampler m_ProfilingSamplerSkyCopy = new ProfilingSampler("Sky Copy");

        // TODO: Increase initial size to 2 when static sky is implemented.
        DynamicArray<CachedSkyContext> m_CachedSkyContexts = new DynamicArray<CachedSkyContext>(1);

        // Look for any camera that hasn't been used in the last frame and remove them from the pool.
        public void CleanUnusedCameras()
        {
            foreach (var camera in m_Cameras.Keys)
            {
                // Unfortunately, the scene view camera is always isActiveAndEnabled==false so we can't rely on this. For this reason we never release it (which should be fine in the editor)
                if (camera != null && camera.cameraType == CameraType.SceneView)
                    continue;

                // We keep preview camera around as they are generally disabled/enabled every frame. They will be destroyed later when camera.camera is null
                if (camera == null || (!camera.isActiveAndEnabled && camera.cameraType != CameraType.Preview))
                    m_CamerasToCleanup.Add(camera);
            }

            foreach (var camera in m_CamerasToCleanup)
            {
                var skyUpdateContext = m_Cameras[camera];
                ReleaseCachedContext(skyUpdateContext.cachedSkyRenderingContextId);
                skyUpdateContext.Cleanup();
                m_Cameras.Remove(camera);
            }

            m_CamerasToCleanup.Clear();
        }

        public void UpdateCurrentSkySettings(ref CameraData cameraData)
        {
            // TODO Editor preview camera

            var volumeStack = VolumeManager.instance.stack;

            cameraData.skyAmbientMode = volumeStack.GetComponent<VisualEnvironment>().skyAmbientMode.value;

            if (!m_Cameras.TryGetValue(cameraData.camera, out var visualSky))
            {
                visualSky = new SkyUpdateContext();
                m_Cameras.Add(cameraData.camera, visualSky);
            }
            visualSky.skySettings = GetSkySettings(volumeStack);
            cameraData.visualSky = visualSky;

            // TODO Lighting override
            cameraData.lightingSky = visualSky;
        }

        SkySettings GetSkySettings(VolumeStack stack)
        {
            var visualEnvironmet = stack.GetComponent<VisualEnvironment>();
            int skyID = visualEnvironmet.skyType.value;

            Type skyType;
            if (SkyTypesCatalog.skyTypesDict.TryGetValue(skyID, out skyType))
            {
                return stack.GetComponent(skyType) as SkySettings;
            }

            return null;
        }

        public void Build()
        {
            // TODO: Get the shader not from ForwardRenderer. SkyManager is managed by the pipeline.
            var urpRendererData = UniversalRenderPipeline.asset.scriptableRendererData;
            if (urpRendererData is ForwardRendererData forwardRendererData)
            {
                m_StandardSkyboxMaterial = CoreUtils.CreateEngineMaterial(forwardRendererData.shaders.skyboxCubemapPS);
                m_ConvolveMaterial = CoreUtils.CreateEngineMaterial(forwardRendererData.shaders.GGXConvolvePS);
            }

            InitializeGgxIblSampleData();

            m_SkyboxBSDFCubemapIntermediate = RTHandles.Alloc(k_Resolution, k_Resolution,
                colorFormat: Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat,
                dimension: TextureDimension.Cube,
                useMipMap: true,
                autoGenerateMips: false,
                filterMode: FilterMode.Trilinear,
                name: "SkyboxBSDFIntermediate");

            var cubemapScreenSize = new Vector4(k_Resolution, k_Resolution, 1.0f / k_Resolution, 1.0f / k_Resolution);
            for (int i = 0; i < 6; ++i)
            {
                var lookAt = Matrix4x4.LookAt(Vector3.zero, CoreUtils.lookAtList[i], CoreUtils.upVectorList[i]);
                m_FaceWorldToViewMatrixMatrices[i] = lookAt * Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f)); // Need to scale -1.0 on Z to match what is being done in the camera.wolrdToCameraMatrix API. ...
                m_FacePixelCoordToViewDirMatrices[i] = ComputePixelCoordToWorldSpaceViewDirectionMatrix(0.5f * Mathf.PI, Vector2.zero, cubemapScreenSize, m_FaceWorldToViewMatrixMatrices[i], true);
            }
        }

        private void InitializeGgxIblSampleData()
        {
            m_ConvolveGgxIblSampleData = new Texture2D(34, 6, TextureFormat.RGBAHalf, false, true);
            m_ConvolveGgxIblSampleData.filterMode = FilterMode.Point;
            m_ConvolveGgxIblSampleData.name = "GGXIblSampleData_34x6";

            byte[] sampleBytes = new byte[]
            {
                0xc8, 0x1a, 0x00, 0x00, 0xff, 0x3b, 0x85, 0x04, 0x6c, 0x9c,
                0x0d, 0x9c, 0xff, 0x3b, 0x00, 0x05, 0x87, 0x11, 0xe0, 0x1f,
                0xff, 0x3b, 0x8c, 0x05, 0xd5, 0x1d, 0x9c, 0x9f, 0xff, 0x3b,
                0x31, 0x06, 0x83, 0xa1, 0xce, 0x17, 0xff, 0x3b, 0xf8, 0x06,
                0x64, 0x21, 0xdc, 0x1e, 0xff, 0x3b, 0xe6, 0x07, 0x75, 0x9b,
                0xf0, 0xa2, 0xff, 0x3b, 0x83, 0x08, 0x5f, 0x9f, 0x19, 0x23,
                0xff, 0x3b, 0x35, 0x09, 0x27, 0x24, 0x11, 0x9e, 0xff, 0x3b,
                0x12, 0x0a, 0x81, 0xa4, 0x71, 0x9f, 0xff, 0x3b, 0x2d, 0x0b,
                0x8b, 0x20, 0xdb, 0x24, 0xff, 0x3b, 0x4c, 0x0c, 0x10, 0x1f,
                0xa1, 0xa5, 0xff, 0x3b, 0x41, 0x0d, 0xa0, 0xa5, 0x85, 0x22,
                0xff, 0x3b, 0x91, 0x0e, 0x06, 0x27, 0x2d, 0x1e, 0xff, 0x3b,
                0x37, 0x10, 0x9b, 0xa4, 0x8d, 0xa6, 0xfe, 0x3b, 0x9c, 0x11,
                0xa0, 0x9c, 0x76, 0x28, 0xfe, 0x3b, 0xd6, 0x13, 0xd9, 0x27,
                0x9d, 0xa6, 0xfe, 0x3b, 0xda, 0x15, 0xfc, 0xa9, 0xec, 0x97,
                0xfd, 0x3b, 0xd6, 0x18, 0x2a, 0x29, 0x24, 0x29, 0xfc, 0x3b,
                0xbc, 0x1c, 0x22, 0x9b, 0xd2, 0xac, 0xfa, 0x3b, 0x91, 0x22,
                0x79, 0xad, 0x8f, 0x2e, 0xed, 0x3b, 0x58, 0x2f, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0xc9, 0x21, 0x00, 0x00, 0xff, 0x3b, 0xae, 0x12,
                0x81, 0xa3, 0xe0, 0xa2, 0xff, 0x3b, 0x18, 0x13, 0xab, 0x18,
                0xa6, 0x26, 0xff, 0x3b, 0x8d, 0x13, 0xe2, 0x24, 0x5e, 0xa6,
                0xfe, 0x3b, 0x07, 0x14, 0x8e, 0xa8, 0x72, 0x1e, 0xfe, 0x3b,
                0x4e, 0x14, 0x63, 0x28, 0x95, 0x25, 0xfe, 0x3b, 0x9c, 0x14,
                0xfa, 0xa1, 0x8f, 0xa9, 0xfd, 0x3b, 0xf3, 0x14, 0xce, 0xa5,
                0x97, 0x29, 0xfd, 0x3b, 0x54, 0x15, 0x6b, 0x2a, 0xb0, 0xa4,
                0xfd, 0x3b, 0xc1, 0x15, 0xd0, 0xaa, 0xa0, 0xa5, 0xfc, 0x3b,
                0x3c, 0x16, 0xb5, 0x26, 0x2a, 0x2b, 0xfc, 0x3b, 0xc6, 0x16,
                0x10, 0x25, 0x09, 0xac, 0xfb, 0x3b, 0x63, 0x17, 0xce, 0xab,
                0x86, 0x28, 0xfa, 0x3b, 0x0b, 0x18, 0xb0, 0x2c, 0x1f, 0x24,
                0xfa, 0x3b, 0x72, 0x18, 0xdd, 0xa9, 0x2c, 0xac, 0xf9, 0x3b,
                0xe9, 0x18, 0x90, 0xa1, 0x5e, 0x2d, 0xf8, 0x3b, 0x75, 0x19,
                0x63, 0x2c, 0x66, 0xab, 0xf7, 0x3b, 0x18, 0x1a, 0x15, 0xae,
                0x06, 0x9c, 0xf6, 0x3b, 0xda, 0x1a, 0x93, 0x2c, 0x8d, 0x2c,
                0xf5, 0x3b, 0xc3, 0x1b, 0x10, 0x9d, 0xd7, 0xae, 0xf4, 0x3b,
                0x6e, 0x1c, 0xa9, 0xac, 0x96, 0x2d, 0xf2, 0x3b, 0x1b, 0x1d,
                0xac, 0x2f, 0x21, 0xa4, 0xf0, 0x3b, 0xf4, 0x1d, 0xc6, 0xae,
                0xb6, 0xac, 0xee, 0x3b, 0x07, 0x1f, 0xbe, 0x27, 0x4d, 0x30,
                0xec, 0x3b, 0x35, 0x20, 0xb4, 0x2c, 0x1a, 0xb0, 0xe9, 0x3b,
                0x22, 0x21, 0xdb, 0xb0, 0x32, 0x2a, 0xe5, 0x3b, 0x67, 0x22,
                0x04, 0x31, 0xa2, 0x2c, 0xe1, 0x3b, 0x1a, 0x24, 0xa9, 0xac,
                0x91, 0xb1, 0xdb, 0x3b, 0x72, 0x25, 0x83, 0xac, 0x46, 0x32,
                0xd3, 0x3b, 0x93, 0x27, 0x9e, 0x32, 0xf5, 0xae, 0xc7, 0x3b,
                0xa0, 0x29, 0x26, 0xb4, 0x60, 0xac, 0xb4, 0x3b, 0x9b, 0x2c,
                0x89, 0x31, 0x4e, 0x34, 0x94, 0x3b, 0x71, 0x30, 0x76, 0x2c,
                0x7e, 0xb6, 0x4a, 0x3b, 0xf1, 0x35, 0x30, 0xb8, 0x7d, 0x36,
                0xfd, 0x39, 0x99, 0x41, 0x55, 0x27, 0x00, 0x00, 0xff, 0x3b,
                0x5b, 0x1d, 0xc0, 0xa8, 0x5a, 0xa8, 0xfd, 0x3b, 0xaf, 0x1d,
                0xe8, 0x1d, 0x34, 0x2c, 0xfb, 0x3b, 0x0b, 0x1e, 0x2c, 0x2a,
                0x07, 0xac, 0xf9, 0x3b, 0x71, 0x1e, 0xc2, 0xad, 0x13, 0x24,
                0xf7, 0x3b, 0xe1, 0x1e, 0x8c, 0x2d, 0x0e, 0x2b, 0xf5, 0x3b,
                0x5d, 0x1f, 0x8d, 0xa7, 0x05, 0xaf, 0xf2, 0x3b, 0xe6, 0x1f,
                0x54, 0xab, 0x0e, 0x2f, 0xf0, 0x3b, 0x3f, 0x20, 0x0c, 0x30,
                0xea, 0xa9, 0xed, 0x3b, 0x94, 0x20, 0x4b, 0xb0, 0x17, 0xab,
                0xea, 0x3b, 0xf4, 0x20, 0x39, 0x2c, 0x83, 0x30, 0xe7, 0x3b,
                0x60, 0x21, 0x60, 0x2a, 0x15, 0xb1, 0xe3, 0x3b, 0xdb, 0x21,
                0xe9, 0xb0, 0xb1, 0x2d, 0xdf, 0x3b, 0x66, 0x22, 0xe4, 0x31,
                0x2e, 0x29, 0xdb, 0x3b, 0x06, 0x23, 0x5d, 0xaf, 0x3d, 0xb1,
                0xd6, 0x3b, 0xbf, 0x23, 0xfa, 0xa6, 0xbb, 0x32, 0xd1, 0x3b,
                0x4a, 0x24, 0x7f, 0x31, 0xa2, 0xb0, 0xcb, 0x3b, 0xc8, 0x24,
                0x9b, 0xb3, 0x08, 0xa1, 0xc5, 0x3b, 0x5d, 0x25, 0xb7, 0x31,
                0xb0, 0x31, 0xbd, 0x3b, 0x0e, 0x26, 0x50, 0xa2, 0x44, 0xb4,
                0xb5, 0x3b, 0xe4, 0x26, 0xcd, 0xb1, 0xf4, 0x32, 0xac, 0x3b,
                0xea, 0x27, 0xc3, 0x34, 0x20, 0xa9, 0xa1, 0x3b, 0x97, 0x28,
                0x31, 0xb4, 0xd6, 0xb1, 0x94, 0x3b, 0x63, 0x29, 0xc7, 0x2c,
                0x4f, 0x35, 0x85, 0x3b, 0x6a, 0x2a, 0xc9, 0x31, 0x0c, 0xb5,
                0x73, 0x3b, 0xc4, 0x2b, 0xf1, 0xb5, 0x96, 0x2f, 0x5d, 0x3b,
                0xcc, 0x2c, 0x1a, 0x36, 0xa4, 0x31, 0x42, 0x3b, 0x13, 0x2e,
                0xa1, 0xb1, 0xba, 0xb6, 0x1e, 0x3b, 0xf1, 0x2f, 0x65, 0xb1,
                0x80, 0x37, 0xef, 0x3a, 0x69, 0x31, 0xcc, 0x37, 0x19, 0xb4,
                0xad, 0x3a, 0xce, 0x33, 0xc8, 0xb8, 0x0a, 0xb1, 0x49, 0x3a,
                0x1d, 0x36, 0x23, 0x36, 0xc6, 0x38, 0xa3, 0x39, 0x75, 0x39,
                0x8e, 0x30, 0xa1, 0xba, 0x54, 0x38, 0x32, 0x3e, 0x51, 0xba,
                0xe4, 0x38, 0x47, 0x29, 0x5d, 0x46, 0x57, 0x2b, 0x00, 0x00,
                0xfc, 0x3b, 0x5d, 0x25, 0xbf, 0xac, 0x59, 0xac, 0xf5, 0x3b,
                0xa8, 0x25, 0xe3, 0x21, 0x31, 0x30, 0xee, 0x3b, 0xfa, 0x25,
                0x24, 0x2e, 0x01, 0xb0, 0xe6, 0x3b, 0x54, 0x26, 0xb6, 0xb1,
                0x0a, 0x28, 0xde, 0x3b, 0xb5, 0x26, 0x7c, 0x31, 0xfa, 0x2e,
                0xd5, 0x3b, 0x20, 0x27, 0x72, 0xab, 0xec, 0xb2, 0xcb, 0x3b,
                0x95, 0x27, 0x34, 0xaf, 0xf0, 0x32, 0xc1, 0x3b, 0x0a, 0x28,
                0xf0, 0x33, 0xcc, 0xad, 0xb7, 0x3b, 0x51, 0x28, 0x31, 0xb4,
                0xec, 0xae, 0xab, 0x3b, 0x9f, 0x28, 0x1c, 0x30, 0x64, 0x34,
                0x9f, 0x3b, 0xf6, 0x28, 0x2d, 0x2e, 0xec, 0xb4, 0x92, 0x3b,
                0x57, 0x29, 0xbc, 0xb4, 0x7c, 0x31, 0x84, 0x3b, 0xc3, 0x29,
                0xa7, 0x35, 0xf9, 0x2c, 0x75, 0x3b, 0x3d, 0x2a, 0x08, 0xb3,
                0x00, 0xb5, 0x64, 0x3b, 0xc7, 0x2a, 0x9f, 0xaa, 0x63, 0x36,
                0x52, 0x3b, 0x63, 0x2b, 0x2f, 0x35, 0x5e, 0xb4, 0x3e, 0x3b,
                0x0a, 0x2c, 0x1f, 0xb7, 0xb6, 0xa4, 0x29, 0x3b, 0x70, 0x2c,
                0x4f, 0x35, 0x48, 0x35, 0x11, 0x3b, 0xe6, 0x2c, 0xd0, 0xa5,
                0xdb, 0xb7, 0xf7, 0x3a, 0x70, 0x2d, 0x4a, 0xb5, 0x57, 0x36,
                0xda, 0x3a, 0x11, 0x2e, 0x4b, 0x38, 0x9f, 0xac, 0xb9, 0x3a,
                0xd1, 0x2e, 0x78, 0xb7, 0x32, 0xb5, 0x94, 0x3a, 0xb6, 0x2f,
                0x32, 0x30, 0xa9, 0x38, 0x6a, 0x3a, 0x65, 0x30, 0xfe, 0x34,
                0x5b, 0xb8, 0x3a, 0x3a, 0x0f, 0x31, 0x07, 0xb9, 0x6b, 0x32,
                0x02, 0x3a, 0xe2, 0x31, 0x0b, 0x39, 0xa8, 0x34, 0xc1, 0x39,
                0xee, 0x32, 0x84, 0xb4, 0x65, 0xb9, 0x74, 0x39, 0x24, 0x34,
                0x2d, 0xb4, 0xce, 0x39, 0x17, 0x39, 0x09, 0x35, 0xc3, 0x39,
                0x0f, 0xb6, 0xa5, 0x38, 0x41, 0x36, 0xa7, 0xba, 0x03, 0xb3,
                0x15, 0x38, 0xfa, 0x37, 0xdb, 0x37, 0x1b, 0x3a, 0xb5, 0x36,
                0x42, 0x39, 0x2d, 0x31, 0x87, 0xbb, 0xbd, 0x34, 0x41, 0x3b,
                0x47, 0xba, 0xdc, 0x38, 0xc6, 0x2f, 0x52, 0x3d, 0x95, 0x2e,
                0x00, 0x00, 0xf5, 0x3b, 0x4f, 0x2c, 0x3c, 0xb0, 0xc2, 0xaf,
                0xde, 0x3b, 0x7b, 0x2c, 0x38, 0x25, 0x70, 0x33, 0xc7, 0x3b,
                0xa9, 0x2c, 0x69, 0x31, 0x0f, 0xb3, 0xaf, 0x3b, 0xdb, 0x2c,
                0x00, 0xb5, 0x14, 0x2b, 0x95, 0x3b, 0x0f, 0x2d, 0xc6, 0x34,
                0x12, 0x32, 0x7b, 0x3b, 0x47, 0x2d, 0x6f, 0xae, 0xfb, 0xb5,
                0x60, 0x3b, 0x83, 0x2d, 0x2e, 0xb2, 0xf3, 0x35, 0x43, 0x3b,
                0xc3, 0x2d, 0xc1, 0x36, 0xef, 0xb0, 0x25, 0x3b, 0x07, 0x2e,
                0x14, 0xb7, 0xd8, 0xb1, 0x06, 0x3b, 0x50, 0x2e, 0xe0, 0x32,
                0x59, 0x37, 0xe5, 0x3a, 0x9e, 0x2e, 0x1f, 0x31, 0x15, 0xb8,
                0xc2, 0x3a, 0xf3, 0x2e, 0xc6, 0xb7, 0x81, 0x34, 0x9e, 0x3a,
                0x4d, 0x2f, 0x98, 0x38, 0x0a, 0x30, 0x78, 0x3a, 0xaf, 0x2f,
                0xa7, 0xb5, 0x05, 0xb8, 0x4f, 0x3a, 0x0c, 0x30, 0x43, 0xad,
                0x13, 0x39, 0x25, 0x3a, 0x45, 0x30, 0x12, 0x38, 0xdc, 0xb6,
                0xf8, 0x39, 0x83, 0x30, 0x84, 0xb9, 0x4d, 0xa7, 0xc9, 0x39,
                0xc7, 0x30, 0x0e, 0x38, 0x09, 0x38, 0x97, 0x39, 0x10, 0x31,
                0x5f, 0xa8, 0xe9, 0xb9, 0x62, 0x39, 0x60, 0x31, 0xd4, 0xb7,
                0xb1, 0x38, 0x29, 0x39, 0xb9, 0x31, 0x3e, 0x3a, 0xb9, 0xae,
                0xed, 0x38, 0x19, 0x32, 0x53, 0xb9, 0x69, 0xb7, 0xad, 0x38,
                0x84, 0x32, 0xdc, 0x31, 0x83, 0x3a, 0x68, 0x38, 0xfa, 0x32,
                0xd1, 0x36, 0xf3, 0xb9, 0x1e, 0x38, 0x7d, 0x33, 0xb3, 0xba,
                0x46, 0x34, 0x9f, 0x37, 0x07, 0x34, 0x8a, 0x3a, 0x0b, 0x36,
                0xf5, 0x36, 0x59, 0x34, 0xb0, 0xb5, 0xcb, 0xba, 0x3c, 0x36,
                0xb5, 0x34, 0x17, 0xb5, 0x13, 0x3b, 0x74, 0x35, 0x1c, 0x35,
                0xc8, 0x3a, 0x20, 0xb7, 0x9b, 0x34, 0x92, 0x35, 0x87, 0xbb,
                0xf0, 0xb3, 0x5b, 0x33, 0x18, 0x36, 0x44, 0x38, 0xa2, 0x3a,
                0x53, 0x31, 0xb2, 0x36, 0x65, 0x31, 0xd8, 0xbb, 0x2c, 0x2e,
                0x64, 0x37, 0x52, 0xba, 0xe5, 0x38, 0xde, 0x24, 0x19, 0x38,
                0x77, 0x31, 0x00, 0x00, 0xe1, 0x3b, 0xe9, 0x31, 0xee, 0xb2,
                0x59, 0xb2, 0xa5, 0x3b, 0xe9, 0x31, 0x36, 0x28, 0xff, 0x35,
                0x69, 0x3b, 0xe9, 0x31, 0x4d, 0x34, 0x9c, 0xb5, 0x2d, 0x3b,
                0xe9, 0x31, 0xd5, 0xb7, 0x8a, 0x2d, 0xf0, 0x3a, 0xe9, 0x31,
                0x5c, 0x37, 0xae, 0x34, 0xb4, 0x3a, 0xe9, 0x31, 0xe2, 0xb0,
                0x8b, 0xb8, 0x78, 0x3a, 0xe9, 0x31, 0x9e, 0xb4, 0x72, 0x38,
                0x3c, 0x3a, 0xe9, 0x31, 0xf8, 0x38, 0x42, 0xb3, 0xff, 0x39,
                0xe9, 0x31, 0x20, 0xb9, 0x3b, 0xb4, 0xc3, 0x39, 0xe9, 0x31,
                0xe6, 0x34, 0x3c, 0x39, 0x87, 0x39, 0xe9, 0x31, 0x2e, 0x33,
                0xb9, 0xb9, 0x4b, 0x39, 0xe9, 0x31, 0x5c, 0xb9, 0x37, 0x36,
                0x0f, 0x39, 0xe9, 0x31, 0x3b, 0x3a, 0x7b, 0x31, 0xd2, 0x38,
                0xe9, 0x31, 0x89, 0xb7, 0x5c, 0xb9, 0x96, 0x38, 0xe9, 0x31,
                0xe6, 0xae, 0xa8, 0x3a, 0x5a, 0x38, 0xe9, 0x31, 0x3e, 0x39,
                0x6b, 0xb8, 0x1e, 0x38, 0xe9, 0x31, 0xfd, 0xba, 0x9f, 0xa8,
                0xc3, 0x37, 0xe9, 0x31, 0x0c, 0x39, 0x05, 0x39, 0x4b, 0x37,
                0xe9, 0x31, 0x58, 0xa9, 0x3a, 0xbb, 0xd2, 0x36, 0xe9, 0x31,
                0xb4, 0xb8, 0xa3, 0x39, 0x5a, 0x36, 0xe9, 0x31, 0x5f, 0x3b,
                0xef, 0xaf, 0xe1, 0x35, 0xe9, 0x31, 0x2e, 0xba, 0x4c, 0xb8,
                0x69, 0x35, 0xe9, 0x31, 0xae, 0x32, 0x6c, 0x3b, 0xf0, 0x34,
                0xe9, 0x31, 0xa3, 0x37, 0xaa, 0xba, 0x78, 0x34, 0xe9, 0x31,
                0x61, 0xbb, 0xb5, 0x34, 0x00, 0x34, 0xe9, 0x31, 0x15, 0x3b,
                0x8b, 0x36, 0x0f, 0x33, 0xe9, 0x31, 0x10, 0xb6, 0x3e, 0xbb,
                0x1e, 0x32, 0xe9, 0x31, 0x58, 0xb5, 0x6d, 0x3b, 0x2d, 0x31,
                0xe9, 0x31, 0x04, 0x3b, 0x60, 0xb7, 0x3c, 0x30, 0xe9, 0x31,
                0xb1, 0xbb, 0x0e, 0xb4, 0x96, 0x2e, 0xe9, 0x31, 0x50, 0x38,
                0xb5, 0x3a, 0xb4, 0x2c, 0xe9, 0x31, 0x6a, 0x31, 0xe0, 0xbb,
                0xa5, 0x29, 0xe9, 0x31, 0x53, 0xba, 0xe5, 0x38, 0x87, 0x23,
                0xe9, 0x31
            };
            m_ConvolveGgxIblSampleData.LoadRawTextureData(sampleBytes);
            m_ConvolveGgxIblSampleData.Apply();
        }

        // TODO: It's copied from HDUtils. Move both to CoreUtils?
        Matrix4x4 ComputePixelCoordToWorldSpaceViewDirectionMatrix(float verticalFoV, Vector2 lensShift, Vector4 screenSize, Matrix4x4 worldToViewMatrix, bool renderToCubemap, float aspectRatio = -1)
        {
            aspectRatio = aspectRatio < 0 ? screenSize.x * screenSize.w : aspectRatio;

            // Compose the view space version first.
            // V = -(X, Y, Z), s.t. Z = 1,
            // X = (2x / resX - 1) * tan(vFoV / 2) * ar = x * [(2 / resX) * tan(vFoV / 2) * ar] + [-tan(vFoV / 2) * ar] = x * [-m00] + [-m20]
            // Y = (2y / resY - 1) * tan(vFoV / 2)      = y * [(2 / resY) * tan(vFoV / 2)]      + [-tan(vFoV / 2)]      = y * [-m11] + [-m21]

            float tanHalfVertFoV = Mathf.Tan(0.5f * verticalFoV);

            // Compose the matrix.
            float m21 = (1.0f - 2.0f * lensShift.y) * tanHalfVertFoV;
            float m11 = -2.0f * screenSize.w * tanHalfVertFoV;

            float m20 = (1.0f - 2.0f * lensShift.x) * tanHalfVertFoV * aspectRatio;
            float m00 = -2.0f * screenSize.z * tanHalfVertFoV * aspectRatio;

            if (renderToCubemap)
            {
                // Flip Y.
                m11 = -m11;
                m21 = -m21;
            }

            var viewSpaceRasterTransform = new Matrix4x4(new Vector4(m00, 0.0f, 0.0f, 0.0f),
                new Vector4(0.0f, m11, 0.0f, 0.0f),
                new Vector4(m20, m21, -1.0f, 0.0f),
                new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

            // Remove the translation component.
            var homogeneousZero = new Vector4(0, 0, 0, 1);
            worldToViewMatrix.SetColumn(3, homogeneousZero);

            // Flip the Z to make the coordinate system left-handed.
            worldToViewMatrix.SetRow(2, -worldToViewMatrix.GetRow(2));

            // Transpose for HLSL.
            return Matrix4x4.Transpose(worldToViewMatrix.transpose * viewSpaceRasterTransform);
        }

        public void Cleanup()
        {
            CoreUtils.Destroy(m_StandardSkyboxMaterial);
            CoreUtils.Destroy(m_ConvolveMaterial);

            RTHandles.Release(m_SkyboxBSDFCubemapIntermediate);

            foreach (var skyUpdateContext in m_Cameras.Values)
                skyUpdateContext.Cleanup();
            m_Cameras.Clear();

            for (int i = 0; i < m_CachedSkyContexts.size; ++i)
                m_CachedSkyContexts[i].Cleanup();
        }

        SphericalHarmonicsL2 GetAmbientProbe(SkyUpdateContext skyContext)
        {
            if (skyContext.IsValid() && IsCachedContextValid(skyContext))
            {
                ref var context = ref m_CachedSkyContexts[skyContext.cachedSkyRenderingContextId];
                return context.renderingContext.ambientProbe;
            }
            else
            {
                return m_BlackAmbientProbe;
            }
        }

        Texture GetSkyCubemap(SkyUpdateContext skyContext)
        {
            if (skyContext.IsValid() && IsCachedContextValid(skyContext))
            {
                ref var context = ref m_CachedSkyContexts[skyContext.cachedSkyRenderingContextId];
                return context.renderingContext.skyboxCubemapRT;
            }
            else
            {
                return CoreUtils.blackCubeTexture;
            }
        }

        Cubemap GetReflectionTexture(SkyUpdateContext skyContext)
        {
            if (skyContext.IsValid() && IsCachedContextValid(skyContext))
            {
                ref var context = ref m_CachedSkyContexts[skyContext.cachedSkyRenderingContextId];
                return context.renderingContext.skyboxCubemap;
            }
            else
            {
                return CoreUtils.blackCubeTexture;
            }
        }

        public void SetupAmbientProbe(ref CameraData cameraData)
        {
            // Working around GI current system
            // When using baked lighting, setting up the ambient probe should be sufficient => We only need to update RenderSettings.ambientProbe with either the static or visual sky ambient probe
            // When using real time GI. Enlighten will pull sky information from Skybox material. So in order for dynamic GI to work, we update the skybox material texture and then set the ambient mode to SkyBox
            // Problem: We can't check at runtime if realtime GI is enabled so we need to take extra care (see useRealtimeGI usage below)

            // Don't overwrite settings from the built-in sky if no lightingSky set.
            var skyContext = cameraData.lightingSky;
            if (!skyContext.IsValid())
                return;

            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientProbe = GetAmbientProbe(cameraData.lightingSky);

            // TODO: Use static sky for realtime GI
            m_StandardSkyboxMaterial.SetTexture(SkyShaderConstants._Tex, GetSkyCubemap(cameraData.lightingSky));
            RenderSettings.skybox = m_StandardSkyboxMaterial; // Setup this material to be used by GI

            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflection = GetReflectionTexture(cameraData.lightingSky);
        }

        void RenderSkyToCubemap(ref CameraData cameraData, CommandBuffer cmd)
        {
            var skyContext = cameraData.lightingSky;

            var renderingContext = m_CachedSkyContexts[skyContext.cachedSkyRenderingContextId].renderingContext;
            var renderer = skyContext.skyRenderer;

            var faceCameraData = cameraData;

            for (int i = 0; i < 6; ++i)
            {
                faceCameraData.pixelCoordToViewDirMatrix = m_FacePixelCoordToViewDirMatrices[i];

                CoreUtils.SetRenderTarget(cmd, renderingContext.skyboxCubemapRT, ClearFlag.None, 0, (CubemapFace)i);
                renderer.RenderSky(ref faceCameraData, cmd);
            }

            // Generate mipmap for our cubemap
            Debug.Assert(renderingContext.skyboxCubemapRT.rt.autoGenerateMips == false);
            cmd.GenerateMips(renderingContext.skyboxCubemapRT);
        }

        void RenderCubemapGGXConvolution(ref CameraData cameraData, CommandBuffer cmd)
        {
            var skyContext = cameraData.lightingSky;

            var renderingContext = m_CachedSkyContexts[skyContext.cachedSkyRenderingContextId].renderingContext;
            var renderer = skyContext.skyRenderer;

            using (new ProfilingScope(cmd, m_ProfilingSamplerSkyConvolution))
            {
                const int CONVOLUTION_MIP_COUNT = 7;

                Texture source = renderingContext.skyboxCubemapRT;
                RenderTexture target = m_SkyboxBSDFCubemapIntermediate;

                int mipCount = 1 + (int)Mathf.Log(source.width, 2.0f);
                if (mipCount < CONVOLUTION_MIP_COUNT)
                {
                    Debug.LogWarning("RenderCubemapGGXConvolution: Cubemap size is too small for GGX convolution, needs at least " + CONVOLUTION_MIP_COUNT + " mip levels");
                    return;
                }

                // Copy the first mip
                for (int f = 0; f < 6; f++)
                {
                    cmd.CopyTexture(source, f, 0, target, f, 0);
                }

                // Solid angle associated with a texel of the cubemap.
                float invOmegaP = (6.0f * source.width * source.width) / (4.0f * Mathf.PI);

                if (!m_ConvolveGgxIblSampleData)
                {
                    InitializeGgxIblSampleData();
                }

                var props = new MaterialPropertyBlock();
                props.SetTexture("_MainTex", source);
                props.SetFloat("_InvOmegaP", invOmegaP);
                props.SetTexture("_GgxIblSamples", m_ConvolveGgxIblSampleData);

                for (int mip = 1; mip < CONVOLUTION_MIP_COUNT; ++mip)
                {
                    props.SetFloat("_Level", mip);

                    var faceSize = new Vector4(source.width >> mip, source.height >> mip, 1.0f / (source.width >> mip), 1.0f / (source.height >> mip));
                    for (int face = 0; face < 6; ++face)
                    {
                        var transform = ComputePixelCoordToWorldSpaceViewDirectionMatrix(0.5f * Mathf.PI, Vector2.zero, faceSize, m_FaceWorldToViewMatrixMatrices[face], true);
                        props.SetMatrix("_PixelCoordToViewDirWS", transform);

                        CoreUtils.SetRenderTarget(cmd, target, ClearFlag.None, mip, (CubemapFace)face);
                        CoreUtils.DrawFullScreen(cmd, m_ConvolveMaterial, props);
                    }
                }
            }

            // Finally, copy results to the cubemap array
            // TODO Why can't skyboxCubemap just be a RT so we don't need an intermediate?
            using (new ProfilingScope(cmd, m_ProfilingSamplerSkyCopy))
            {
                for (int i = 0; i < 6; ++i)
                {
                    cmd.CopyTexture(m_SkyboxBSDFCubemapIntermediate, i, renderingContext.skyboxCubemap, i);
                }
            }
        }

        // We do our own hash here because Unity does not provide correct hash for builtin types
        // Moreover, we don't want to test every single parameters of the light so we filter them here in this specific function.
        int GetSunLightHashCode(Light light)
        {
            int hash = 13;

            unchecked
            {
                // Sun could influence the sky (like for procedural sky). We need to handle this possibility. If sun property change, then we need to update the sky
                hash = hash * 23 + light.transform.position.GetHashCode();
                hash = hash * 23 + light.transform.rotation.GetHashCode();
                hash = hash * 23 + light.color.GetHashCode();
                hash = hash * 23 + light.colorTemperature.GetHashCode();
                hash = hash * 23 + light.intensity.GetHashCode();

                // TODO Extra per-RP light parameters
            }

            return hash;
        }

        void AllocateNewRenderingContext(SkyUpdateContext skyContext, int slot, int newHash, in SphericalHarmonicsL2 previousAmbientProbe, string name)
        {
            Debug.Assert(m_CachedSkyContexts[slot].hash == 0);
            ref var context = ref m_CachedSkyContexts[slot];
            context.hash = newHash;
            context.refCount = 1;
            context.type = skyContext.skySettings.GetSkyRendererType();

            if (context.renderingContext == null)
                context.renderingContext = new SkyRenderingContext(k_Resolution, previousAmbientProbe, name);
            else
                context.renderingContext.UpdateAmbientProbe(previousAmbientProbe);
            skyContext.cachedSkyRenderingContextId = slot;
        }

        // Returns whether or not the data should be updated
        bool AcquireSkyRenderingContext(SkyUpdateContext updateContext, int newHash, string name = "", bool supportConvolution = true)
        {
            SphericalHarmonicsL2 cachedAmbientProbe = new SphericalHarmonicsL2();
            // Release the old context if needed.
            if (IsCachedContextValid(updateContext))
            {
                ref var cachedContext = ref m_CachedSkyContexts[updateContext.cachedSkyRenderingContextId];
                if (newHash != cachedContext.hash || updateContext.skySettings.GetSkyRendererType() != cachedContext.type)
                {
                    // When a sky just changes hash without changing renderer, we need to keep previous ambient probe to avoid flickering transition through a default black probe
                    if (updateContext.skySettings.GetSkyRendererType() == cachedContext.type)
                    {
                        cachedAmbientProbe = cachedContext.renderingContext.ambientProbe;
                    }

                    ReleaseCachedContext(updateContext.cachedSkyRenderingContextId);
                }
                else
                {
                    // If the hash hasn't changed, keep it.
                    return false;
                }
            }

            // Else allocate a new one
            int firstFreeContext = -1;
            for (int i = 0; i < m_CachedSkyContexts.size; ++i)
            {
                // Try to find a matching slot
                if (m_CachedSkyContexts[i].hash == newHash)
                {
                    m_CachedSkyContexts[i].refCount++;
                    updateContext.cachedSkyRenderingContextId = i;
                    updateContext.skyParametersHash = newHash;
                    return false;
                }

                // Find the first available slot in case we don't find a matching one.
                if (firstFreeContext == -1 && m_CachedSkyContexts[i].hash == 0)
                    firstFreeContext = i;
            }

            if (name == "")
                name = "SkyboxCubemap";

            if (firstFreeContext != -1)
            {
                AllocateNewRenderingContext(updateContext, firstFreeContext, newHash, cachedAmbientProbe, name);
            }
            else
            {
                int newContextId = m_CachedSkyContexts.Add(new CachedSkyContext());
                AllocateNewRenderingContext(updateContext, newContextId, newHash, cachedAmbientProbe, name);
            }

            return true;
        }

        void ReleaseCachedContext(int id)
        {
            if (id == -1)
                return;

            ref var cachedContext = ref m_CachedSkyContexts[id];

            // This can happen if 2 cameras use the same context and release it in the same frame.
            // The first release the context but the next one will still have this id.
            if (cachedContext.refCount == 0)
            {
                Debug.Assert(cachedContext.renderingContext == null); // Context should already have been cleaned up.
                return;
            }

            cachedContext.refCount--;
            if (cachedContext.refCount == 0)
                cachedContext.Cleanup();
        }

        bool IsCachedContextValid(SkyUpdateContext skyContext)
        {
            if (skyContext.skySettings == null) // Sky set to None
                return false;

            int id = skyContext.cachedSkyRenderingContextId;
            // When the renderer changes, the cached context is no longer valid so we sometimes need to check that.
            return id != -1 && (skyContext.skySettings.GetSkyRendererType() == m_CachedSkyContexts[id].type) && (m_CachedSkyContexts[id].hash != 0);
        }

        int ComputeSkyHash(SkyUpdateContext skyContext)
        {
            int skyHash = skyContext.skySettings.GetHashCode();

            // TODO: Use GetSunLightHashCode when any SkyRenderer starts to rely on the sun.

            return skyHash;
        }

        public void UpdateEnvironment(ref CameraData cameraData,
                                      CommandBuffer  cmd)
        {
            var skyContext = cameraData.lightingSky;

            if (skyContext.IsValid())
            {
                skyContext.currentUpdateTime += Time.deltaTime; // TODO: Does URP have its own time to use?

                int skyHash = ComputeSkyHash(skyContext);
                bool forceUpdate = false;

                // Acquire the rendering context, if the context was invalid or the hash has changed, this will request for an update.
                forceUpdate |= AcquireSkyRenderingContext(skyContext, skyHash, "SkyboxCubemap");

                ref CachedSkyContext cachedContext = ref m_CachedSkyContexts[skyContext.cachedSkyRenderingContextId];
                var renderingContext = cachedContext.renderingContext;

                if (forceUpdate ||
                    (skyContext.skySettings.updateMode.value == EnvironmentUpdateMode.OnChanged && skyHash != skyContext.skyParametersHash) ||
                    (skyContext.skySettings.updateMode.value == EnvironmentUpdateMode.Realtime && skyContext.currentUpdateTime > skyContext.skySettings.updatePeriod.value))
                {
                    using (new ProfilingScope(cmd, m_ProfilingSamplerSkyCubemap))
                    {
                        RenderSkyToCubemap(ref cameraData, cmd);
                    }

                    renderingContext.UpdateAmbientProbe(skyContext.skyRenderer.GetAmbientProbe(ref cameraData));

                    RenderCubemapGGXConvolution(ref cameraData, cmd);
 
                    skyContext.skyParametersHash = skyHash;
                    skyContext.currentUpdateTime = 0.0f;

#if UNITY_EDITOR
                    // In the editor when we change the sky we want to make the GI dirty so when baking again the new sky is taken into account.
                    // Changing the hash of the rendertarget allow to say that GI is dirty
                    renderingContext.skyboxCubemapRT.rt.imageContentsHash = new Hash128((uint)skyHash, 0, 0, 0);
#endif
                }
            }
            else
            {
                if (skyContext.cachedSkyRenderingContextId != -1)
                {
                    ReleaseCachedContext(skyContext.cachedSkyRenderingContextId);
                    skyContext.cachedSkyRenderingContextId = -1;
                }
            }
        }

        public static void PrerenderSky(ref CameraData cameraData, CommandBuffer cmd)
        {
            var skyContext = cameraData.visualSky;
            if (skyContext.IsValid())
            {
                // TODO
                skyContext.skyRenderer.PrerenderSky(ref cameraData, cmd);
            }
        }

        public static void RenderSky(ref CameraData cameraData, CommandBuffer cmd)
        {
            var skyContext = cameraData.visualSky;
            if (skyContext.IsValid())
            {
                // TODO
                skyContext.skyRenderer.RenderSky(ref cameraData, cmd);
            }
        }
    }

    public static class SkyShaderConstants
    {
        public static readonly int _Tex = Shader.PropertyToID("_Tex");
        public static readonly int _SkyIntensity = Shader.PropertyToID("_SkyIntensity");
        public static readonly int _PixelCoordToViewDirWS = Shader.PropertyToID("_PixelCoordToViewDirWS");
    }
}
