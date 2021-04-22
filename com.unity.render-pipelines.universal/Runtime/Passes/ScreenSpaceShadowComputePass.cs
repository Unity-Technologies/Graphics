using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.VoxelizedShadows;

namespace UnityEngine.Rendering.Universal
{
    public class ScreenSpaceShadowComputePass : ScriptableRenderPass
    {
        private static class ShaderIDs
        {
            public static int _ShadowData = Shader.PropertyToID("_MainLightShadowData");

            public static int _InvViewProjMatrix = Shader.PropertyToID("_InvViewProjMatrix");
            public static int _ScreenSize = Shader.PropertyToID("_ScreenSize");
            public static int _VxShadowMapParameters = Shader.PropertyToID("_VxShadowMapParameters");

            public static int _VxShadowMapsBuffer = Shader.PropertyToID("_VxShadowMapsBuffer");
            public static int _CameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");
            public static int _ScreenSpaceShadowOutput = Shader.PropertyToID("_ScreenSpaceShadowOutput");
        }

        static readonly int TileSize = 8;

        private ComputeShader _screenSpaceVxShadowCS;

        private RenderTextureFormat _shadowFormat;
        private RenderTargetHandle _cameraDepthTexture;
        private RenderTargetHandle _mainLightShadowmapTexture;
        private RenderTargetHandle _screenSpaceShadowmapTexture;
        private RenderTextureDescriptor _cameraDepthDescriptor;
        private RenderTextureDescriptor _screenSpaceShadowmapDescriptor;

        private bool _mainLightDynamicShadows = true;

        private const int kDepthBufferBits = 32;
        private const string k_CollectShadowsTag = "Collect Shadows";

        public static DirectionalVxShadowMap MainDirVxShadowMap = null;

        public ScreenSpaceShadowComputePass(RenderPassEvent evt, ComputeShader screenSpaceVxShadowCS)
        {
            _screenSpaceVxShadowCS = screenSpaceVxShadowCS;

            bool R8_UNorm = SystemInfo.IsFormatSupported(GraphicsFormat.R8_UNorm, FormatUsage.LoadStore);
            bool R8_SNorm = SystemInfo.IsFormatSupported(GraphicsFormat.R8_SNorm, FormatUsage.LoadStore);
            bool R8_UInt = SystemInfo.IsFormatSupported(GraphicsFormat.R8_UInt, FormatUsage.LoadStore);
            bool R8_SInt = SystemInfo.IsFormatSupported(GraphicsFormat.R8_SInt, FormatUsage.LoadStore);

            bool R8 = R8_UNorm || R8_SNorm || R8_UInt || R8_SInt;

            _shadowFormat = R8 ? RenderTextureFormat.R8 : RenderTextureFormat.RFloat;
            _cameraDepthTexture.Init("_CameraDepthTexture");
            _mainLightShadowmapTexture.Init("_MainLightShadowmapTexture");
            _screenSpaceShadowmapTexture.Init("_ScreenSpaceShadowmapTexture");

            renderPassEvent = evt;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            _cameraDepthDescriptor = cameraTextureDescriptor;
            _cameraDepthDescriptor.colorFormat = RenderTextureFormat.Depth;
            _cameraDepthDescriptor.depthBufferBits = kDepthBufferBits;
            _cameraDepthDescriptor.msaaSamples = 1;

            _screenSpaceShadowmapDescriptor = cameraTextureDescriptor;
            _screenSpaceShadowmapDescriptor.autoGenerateMips = false;
            _screenSpaceShadowmapDescriptor.useMipMap = false;
            _screenSpaceShadowmapDescriptor.sRGB = false;
            _screenSpaceShadowmapDescriptor.enableRandomWrite = true;
            _screenSpaceShadowmapDescriptor.depthBufferBits = 0;
            _screenSpaceShadowmapDescriptor.colorFormat = _shadowFormat;

            cmd.GetTemporaryRT(_cameraDepthTexture.id, _cameraDepthDescriptor, FilterMode.Point);
            cmd.GetTemporaryRT(_screenSpaceShadowmapTexture.id, _screenSpaceShadowmapDescriptor, FilterMode.Point);

            ConfigureClear(ClearFlag.None, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var shadowLightIndex = renderingData.lightData.mainLightIndex;
            var shadowLight = renderingData.lightData.visibleLights[shadowLightIndex];
            var light = shadowLight.light;

            var shadowData = renderingData.shadowData;
            bool useCascades = shadowData.mainLightShadowCascadesCount > 1;
            bool softShadows = shadowLight.light.shadows == LightShadows.Soft && shadowData.supportsSoftShadows;

            int kernel = GetComputeShaderKernel(useCascades, softShadows);
            if (kernel == -1)
                return;

            CommandBuffer cmd = CommandBufferPool.Get(k_CollectShadowsTag);

            var camera = renderingData.cameraData.camera;
            bool stereo = renderingData.cameraData.isStereoEnabled;

            SetupMainLightVxShadowReceiverConstants(cmd, kernel, camera, light);

            int screenSizeX = camera.pixelWidth;
            int screenSizeY = camera.pixelHeight;

            int kernelX = (screenSizeX + TileSize - 1) / TileSize;
            int kernelY = (screenSizeY + TileSize - 1) / TileSize;

            cmd.DispatchCompute(_screenSpaceVxShadowCS, kernel, kernelX, kernelY, 1);

            if (stereo)
            {
                context.StartMultiEye(camera);
                context.ExecuteCommandBuffer(cmd);
                context.StopMultiEye(camera);
            }
            else
            {
                context.ExecuteCommandBuffer(cmd);
            }
            CommandBufferPool.Release(cmd);

            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadows, false);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowCascades, false);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowScreen, true);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new System.ArgumentNullException("cmd");

            cmd.ReleaseTemporaryRT(_cameraDepthTexture.id);
            cmd.ReleaseTemporaryRT(_screenSpaceShadowmapTexture.id);
        }

        private int GetComputeShaderKernel(bool useCascades, bool softShadows)
        {
            if (_screenSpaceVxShadowCS == null)
                return -1;

            int kernel = -1;

            string blendModeName;
            if (_mainLightDynamicShadows && useCascades)
                blendModeName = "BlendCascadeShadows";
            else if (_mainLightDynamicShadows)
                blendModeName = "BlendDynamicShadows";
            else
                blendModeName = "NoBlend";

            string filteringName = "Nearest";
            filteringName = softShadows ? "Soft" : "Hard";

            string kernelName = blendModeName + filteringName;

            kernel = _screenSpaceVxShadowCS.FindKernel(kernelName);

            return kernel;
        }

        private void SetupMainLightVxShadowReceiverConstants(CommandBuffer cmd, int kernel, Camera camera, Light light)
        {
            float screenSizeX = (float)camera.pixelWidth;
            float screenSizeY = (float)camera.pixelHeight;
            float invScreenSizeX = 1.0f / screenSizeX;
            float invScreenSizeY = 1.0f / screenSizeY;

            var gpuView = camera.worldToCameraMatrix;
            var gpuProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);

            var viewMatrix = gpuView;
            var projMatrix = gpuProj;
            var viewProjMatrix = projMatrix * viewMatrix;

            var vxShadowMapsContainer = VxShadowMapsManager.Instance.Container;

            int beginOffset = (int)(MainDirVxShadowMap.bitset & 0x3FFFFFFF);
            int voxelZBias = vxShadowMapsContainer.ZBias;
            float voxelUpBias = 1 * (MainDirVxShadowMap.VolumeScale / MainDirVxShadowMap.VoxelResolutionInt);

            var shadowData = new Vector4(light.shadowStrength, 0.0f, 0.0f, 0.0f);
            var screenSize = new Vector4(screenSizeX, screenSizeY, invScreenSizeX, invScreenSizeY);
            var parameters = new int[] { beginOffset, voxelZBias, 0, 0, };

            var vxShadowMapsBuffer = VxShadowMapsManager.Instance.VxShadowMapsBuffer;

            cmd.SetComputeVectorParam(_screenSpaceVxShadowCS, ShaderIDs._ShadowData, shadowData);

            cmd.SetComputeMatrixParam(_screenSpaceVxShadowCS, ShaderIDs._InvViewProjMatrix, viewProjMatrix.inverse);
            cmd.SetComputeVectorParam(_screenSpaceVxShadowCS, ShaderIDs._ScreenSize, screenSize);
            cmd.SetComputeIntParams(_screenSpaceVxShadowCS, ShaderIDs._VxShadowMapParameters, parameters);

            var cameraDepthTextureId = _cameraDepthTexture.Identifier();
            var screenSpaceShadowOutputId = _screenSpaceShadowmapTexture.Identifier();

            cmd.SetComputeBufferParam(_screenSpaceVxShadowCS, kernel, ShaderIDs._VxShadowMapsBuffer, vxShadowMapsBuffer);
            cmd.SetComputeTextureParam(_screenSpaceVxShadowCS, kernel, ShaderIDs._CameraDepthTexture, cameraDepthTextureId);
            cmd.SetComputeTextureParam(_screenSpaceVxShadowCS, kernel, ShaderIDs._ScreenSpaceShadowOutput, screenSpaceShadowOutputId);
        }

        internal bool Setup(ScreenSpaceShadowsSettings featureSettings)
        {
            ConfigureInput(ScriptableRenderPassInput.Depth);
            return true;
        }

        public static bool ComputeShadowsInScreenSpace(LightData lightData)
        {
            if (VxShadowMapsManager.Instance.Container == null)
                return false;

            DirectionalVxShadowMap dirVxShadowMap = null;

            var shadowLightIndex = lightData.mainLightIndex;
            if (shadowLightIndex != -1)
            {
                var shadowLight = lightData.visibleLights[shadowLightIndex];
                var light = shadowLight.light;

                dirVxShadowMap = light.GetComponent<DirectionalVxShadowMap>();
                if (dirVxShadowMap == null || dirVxShadowMap.enabled == false)
                    dirVxShadowMap = null;
            }

            MainDirVxShadowMap = dirVxShadowMap;

            return dirVxShadowMap != null ? true : false;
        }
    }
}
