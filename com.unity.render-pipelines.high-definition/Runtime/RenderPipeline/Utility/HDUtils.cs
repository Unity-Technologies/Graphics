using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class HDUtils
    {
        public const PerObjectData k_RendererConfigurationBakedLighting = PerObjectData.LightProbe | PerObjectData.Lightmaps | PerObjectData.LightProbeProxyVolume;
        public const PerObjectData k_RendererConfigurationBakedLightingWithShadowMask = k_RendererConfigurationBakedLighting | PerObjectData.OcclusionProbe | PerObjectData.OcclusionProbeProxyVolume | PerObjectData.ShadowMask;

        static public HDAdditionalReflectionData s_DefaultHDAdditionalReflectionData { get { return ComponentSingleton<HDAdditionalReflectionData>.instance; } }
        static public HDAdditionalLightData s_DefaultHDAdditionalLightData { get { return ComponentSingleton<HDAdditionalLightData>.instance; } }
        static public HDAdditionalCameraData s_DefaultHDAdditionalCameraData { get { return ComponentSingleton<HDAdditionalCameraData>.instance; } }
        static public AdditionalShadowData s_DefaultAdditionalShadowData { get { return ComponentSingleton<AdditionalShadowData>.instance; } }

        static Texture3D m_ClearTexture3D;
        public static Texture3D clearTexture3D
        {
            get
            {
                if (m_ClearTexture3D == null)
                {
                    m_ClearTexture3D = new Texture3D(1, 1, 1, TextureFormat.ARGB32, false) { name = "Transparent Texture 3D" };
                    m_ClearTexture3D.SetPixel(0, 0, 0, Color.clear);
                    m_ClearTexture3D.Apply();
                }

                return m_ClearTexture3D;
            }
        }

        public static Material GetBlitMaterial(TextureDimension dimension)
        {
            HDRenderPipeline hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdPipeline != null)
            {
                return hdPipeline.GetBlitMaterial(dimension == TextureDimension.Tex2DArray);
            }

            return null;
        }

        public static RenderPipelineSettings hdrpSettings
        {
            get
            {
                HDRenderPipelineAsset hdPipelineAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;

                return hdPipelineAsset.currentPlatformRenderPipelineSettings;
            }
        }
        public static int debugStep => MousePositionDebug.instance.debugStep;

        static MaterialPropertyBlock s_PropertyBlock = new MaterialPropertyBlock();

        public static List<RenderPipelineMaterial> GetRenderPipelineMaterialList()
        {
            var baseType = typeof(RenderPipelineMaterial);
            var assembly = baseType.Assembly;

            var types = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(baseType))
                .Select(Activator.CreateInstance)
                .Cast<RenderPipelineMaterial>()
                .ToList();

            // Note: If there is a need for an optimization in the future of this function, user can
            // simply fill the materialList manually by commenting the code abode and returning a
            // custom list of materials they use in their game.
            //
            // return new List<RenderPipelineMaterial>
            // {
            //    new Lit(),
            //    new Unlit(),
            //    ...
            // };

            return types;
        }

        public static Matrix4x4 GetViewProjectionMatrix(Matrix4x4 worldToViewMatrix, Matrix4x4 projectionMatrix)
        {
            // The actual projection matrix used in shaders is actually massaged a bit to work across all platforms
            // (different Z value ranges etc.)
            var gpuProj = GL.GetGPUProjectionMatrix(projectionMatrix, false);
            var gpuVP = gpuProj * worldToViewMatrix * Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f)); // Need to scale -1.0 on Z to match what is being done in the camera.wolrdToCameraMatrix API.

            return gpuVP;
        }

        // Helper to help to display debug info on screen
        static float s_OverlayLineHeight = -1.0f;
        public static void ResetOverlay() => s_OverlayLineHeight = -1.0f;

        public static void NextOverlayCoord(ref float x, ref float y, float overlayWidth, float overlayHeight, HDCamera hdCamera)
        {
            x += overlayWidth;
            s_OverlayLineHeight = Mathf.Max(overlayHeight, s_OverlayLineHeight);
            // Go to next line if it goes outside the screen.
            if ( (x + overlayWidth) > hdCamera.actualWidth)
            {
                x = 0.0f;
                y -= s_OverlayLineHeight;
                s_OverlayLineHeight = -1.0f;
            }
        }

        public static Matrix4x4 ComputePixelCoordToWorldSpaceViewDirectionMatrix(float verticalFoV, Vector2 lensShift, Vector4 screenSize, Matrix4x4 worldToViewMatrix, bool renderToCubemap)
        {
            // Compose the view space version first.
            // V = -(X, Y, Z), s.t. Z = 1,
            // X = (2x / resX - 1) * tan(vFoV / 2) * ar = x * [(2 / resX) * tan(vFoV / 2) * ar] + [-tan(vFoV / 2) * ar] = x * [-m00] + [-m20]
            // Y = (2y / resY - 1) * tan(vFoV / 2)      = y * [(2 / resY) * tan(vFoV / 2)]      + [-tan(vFoV / 2)]      = y * [-m11] + [-m21]

            float tanHalfVertFoV = Mathf.Tan(0.5f * verticalFoV);
            float aspectRatio = screenSize.x * screenSize.w;

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

        public static float ComputZPlaneTexelSpacing(float planeDepth, float verticalFoV, float resolutionY)
        {
            float tanHalfVertFoV = Mathf.Tan(0.5f * verticalFoV);
            return tanHalfVertFoV * (2.0f / resolutionY) * planeDepth;
        }

        private static void SetViewportAndClear(CommandBuffer cmd, RTHandleSystem.RTHandle buffer, ClearFlag clearFlag, Color clearColor)
        {
            // Clearing a partial viewport currently does not go through the hardware clear.
            // Instead it goes through a quad rendered with a specific shader.
            // When enabling wireframe mode in the scene view, unfortunately it overrides this shader thus breaking every clears.
            // That's why in the editor we don't set the viewport before clearing (it's set to full screen by the previous SetRenderTarget) but AFTER so that we benefit from un-bugged hardware clear.
            // We consider that the small loss in performance is acceptable in the editor.
            // A refactor of wireframe is needed before we can fix this properly (with not doing anything!)
#if !UNITY_EDITOR
            SetViewport(cmd, buffer);
#endif
            CoreUtils.ClearRenderTarget(cmd, clearFlag, clearColor);
#if UNITY_EDITOR
            SetViewport(cmd, buffer);
#endif
        }

        // This set of RenderTarget management methods is supposed to be used when rendering into a camera dependent render texture.
        // This will automatically set the viewport based on the camera size and the RTHandle scaling info.
        public static void SetRenderTarget(CommandBuffer cmd, RTHandleSystem.RTHandle buffer, ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            cmd.SetRenderTarget(buffer, miplevel, cubemapFace, depthSlice);
            SetViewportAndClear(cmd, buffer, clearFlag, clearColor);
        }

        public static void SetRenderTarget(CommandBuffer cmd, RTHandleSystem.RTHandle buffer, ClearFlag clearFlag = ClearFlag.None, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
            => SetRenderTarget(cmd, buffer, clearFlag, Color.clear, miplevel, cubemapFace, depthSlice);

        public static void SetRenderTarget(CommandBuffer cmd, RTHandleSystem.RTHandle colorBuffer, RTHandleSystem.RTHandle depthBuffer, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            int cw = colorBuffer.rt.width;
            int ch = colorBuffer.rt.height;
            int dw = depthBuffer.rt.width;
            int dh = depthBuffer.rt.height;

            Debug.Assert(cw == dw && ch == dh);

            SetRenderTarget(cmd, colorBuffer, depthBuffer, ClearFlag.None, Color.clear, miplevel, cubemapFace, depthSlice);
        }

        public static void SetRenderTarget(CommandBuffer cmd, RTHandleSystem.RTHandle colorBuffer, RTHandleSystem.RTHandle depthBuffer, ClearFlag clearFlag, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            int cw = colorBuffer.rt.width;
            int ch = colorBuffer.rt.height;
            int dw = depthBuffer.rt.width;
            int dh = depthBuffer.rt.height;

            Debug.Assert(cw == dw && ch == dh);

            SetRenderTarget(cmd, colorBuffer, depthBuffer, clearFlag, Color.clear, miplevel, cubemapFace, depthSlice);
        }

        public static void SetRenderTarget(CommandBuffer cmd, RTHandleSystem.RTHandle colorBuffer, RTHandleSystem.RTHandle depthBuffer, ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
        {
            int cw = colorBuffer.rt.width;
            int ch = colorBuffer.rt.height;
            int dw = depthBuffer.rt.width;
            int dh = depthBuffer.rt.height;

            Debug.Assert(cw == dw && ch == dh);

            CoreUtils.SetRenderTarget(cmd, colorBuffer, depthBuffer, miplevel, cubemapFace, depthSlice);
            SetViewportAndClear(cmd, colorBuffer, clearFlag, clearColor);
        }

        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier[] colorBuffers, RTHandleSystem.RTHandle depthBuffer)
        {
            CoreUtils.SetRenderTarget(cmd, colorBuffers, depthBuffer, ClearFlag.None, Color.clear);
            SetViewport(cmd, depthBuffer);
        }

        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier[] colorBuffers, RTHandleSystem.RTHandle depthBuffer, ClearFlag clearFlag = ClearFlag.None)
        {
            CoreUtils.SetRenderTarget(cmd, colorBuffers, depthBuffer); // Don't clear here, viewport needs to be set before we do.
            SetViewportAndClear(cmd, depthBuffer, clearFlag, Color.clear);
        }

        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier[] colorBuffers, RTHandleSystem.RTHandle depthBuffer, ClearFlag clearFlag, Color clearColor)
        {
            cmd.SetRenderTarget(colorBuffers, depthBuffer);
            SetViewportAndClear(cmd, depthBuffer, clearFlag, clearColor);
        }

        // Scaling viewport is done for auto-scaling render targets.
        // In the context of HDRP, every auto-scaled RT is scaled against the maximum RTHandles reference size (that can only grow).
        // When we render using a camera whose viewport is smaller than the RTHandles reference size (and thus smaller than the RT actual size), we need to set it explicitly (otherwise, native code will set the viewport at the size of the RT)
        // For auto-scaled RTs (like for example a half-resolution RT), we need to scale this viewport accordingly.
        // For non scaled RTs we just do nothing, the native code will set the viewport at the size of the RT anyway.
        public static void SetViewport(CommandBuffer cmd, RTHandleSystem.RTHandle target)
        {
            if (target.useScaling)
            {
                Vector2Int scaledViewportSize = target.GetScaledSize(target.rtHandleProperties.currentViewportSize);
                cmd.SetViewport(new Rect(0.0f, 0.0f, scaledViewportSize.x, scaledViewportSize.y));
            }
        }

        public static void BlitQuad(CommandBuffer cmd, Texture source, Vector4 scaleBiasTex, Vector4 scaleBiasRT, int mipLevelTex, bool bilinear)
        {
            s_PropertyBlock.SetTexture(HDShaderIDs._BlitTexture, source);
            s_PropertyBlock.SetVector(HDShaderIDs._BlitScaleBias, scaleBiasTex);
            s_PropertyBlock.SetVector(HDShaderIDs._BlitScaleBiasRt, scaleBiasRT);
            s_PropertyBlock.SetFloat(HDShaderIDs._BlitMipLevel, mipLevelTex);
            cmd.DrawProcedural(Matrix4x4.identity, GetBlitMaterial(source.dimension), bilinear ? 3 : 2, MeshTopology.Quads, 4, 1, s_PropertyBlock);
        }

        public static void BlitTexture(CommandBuffer cmd, RTHandleSystem.RTHandle source, Vector4 scaleBias, float mipLevel, bool bilinear)
        {
            s_PropertyBlock.SetTexture(HDShaderIDs._BlitTexture, source);
            s_PropertyBlock.SetVector(HDShaderIDs._BlitScaleBias, scaleBias);
            s_PropertyBlock.SetFloat(HDShaderIDs._BlitMipLevel, mipLevel);
            cmd.DrawProcedural(Matrix4x4.identity, GetBlitMaterial(source.rt.dimension), bilinear ? 1 : 0, MeshTopology.Triangles, 3, 1, s_PropertyBlock);
        }

        // In the context of HDRP, the internal render targets used during the render loop are the same for all cameras, no matter the size of the camera.
        // It means that we can end up rendering inside a partial viewport for one of these "camera space" rendering.
        // In this case, we need to make sure than when we blit from one such camera texture to another, we only blit the necessary portion corresponding to the camera viewport.
        // Here, both source and destination are camera-scaled.
        public static void BlitCameraTexture(CommandBuffer cmd, RTHandleSystem.RTHandle source, RTHandleSystem.RTHandle destination, float mipLevel = 0.0f, bool bilinear = false)
        {
            Vector2 viewportScale = new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y);
            // Will set the correct camera viewport as well.
            SetRenderTarget(cmd, destination);
            BlitTexture(cmd, source, viewportScale, mipLevel, bilinear);
        }


        // This case, both source and destination are camera-scaled but we want to override the scale/bias parameter.
        public static void BlitCameraTexture(CommandBuffer cmd, RTHandleSystem.RTHandle source, RTHandleSystem.RTHandle destination, Vector4 scaleBias, float mipLevel = 0.0f, bool bilinear = false)
        {
            // Will set the correct camera viewport as well.
            SetRenderTarget(cmd, destination);
            BlitTexture(cmd, source, scaleBias, mipLevel, bilinear);
        }

        public static void BlitCameraTexture(CommandBuffer cmd, RTHandleSystem.RTHandle source, RTHandleSystem.RTHandle destination, Rect destViewport, float mipLevel = 0.0f, bool bilinear = false)
        {
            Vector2 viewportScale = new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y);
            SetRenderTarget(cmd, destination);
            cmd.SetViewport(destViewport);
            BlitTexture(cmd, source, viewportScale, mipLevel, bilinear);
        }

        // These method should be used to render full screen triangles sampling auto-scaling RTs.
        // This will set the proper viewport and UV scale.
        public static void DrawFullScreen(CommandBuffer commandBuffer, Material material,
            RTHandleSystem.RTHandle colorBuffer,
            MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            HDUtils.SetRenderTarget(commandBuffer, colorBuffer);
            commandBuffer.SetGlobalVector(HDShaderIDs._RTHandleScale, colorBuffer.rtHandleProperties.rtHandleScale);
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassId, MeshTopology.Triangles, 3, 1, properties);
        }

        public static void DrawFullScreen(CommandBuffer commandBuffer, Material material,
            RTHandleSystem.RTHandle colorBuffer, RTHandleSystem.RTHandle depthStencilBuffer,
            MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            HDUtils.SetRenderTarget(commandBuffer, colorBuffer, depthStencilBuffer);
            commandBuffer.SetGlobalVector(HDShaderIDs._RTHandleScale, colorBuffer.rtHandleProperties.rtHandleScale);
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassId, MeshTopology.Triangles, 3, 1, properties);
        }

        public static void DrawFullScreen(CommandBuffer commandBuffer, Material material,
            RenderTargetIdentifier[] colorBuffers, RTHandleSystem.RTHandle depthStencilBuffer,
            MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            HDUtils.SetRenderTarget(commandBuffer, colorBuffers, depthStencilBuffer);
            commandBuffer.SetGlobalVector(HDShaderIDs._RTHandleScale, depthStencilBuffer.rtHandleProperties.rtHandleScale);
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassId, MeshTopology.Triangles, 3, 1, properties);
        }

        public static void DrawFullScreen(CommandBuffer commandBuffer, RTHandleProperties rtHandleProperties, Material material,
            RenderTargetIdentifier colorBuffer,
            MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            CoreUtils.SetRenderTarget(commandBuffer, colorBuffer);
            commandBuffer.SetGlobalVector(HDShaderIDs._RTHandleScale, rtHandleProperties.rtHandleScale);
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassId, MeshTopology.Triangles, 3, 1, properties);
        }

        public static void DrawFullScreen(CommandBuffer commandBuffer, Rect viewport, Material material, RenderTargetIdentifier destination, MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            CoreUtils.SetRenderTarget(commandBuffer, destination, ClearFlag.None, 0, CubemapFace.Unknown, -1);
            commandBuffer.SetViewport(viewport);
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassId, MeshTopology.Triangles, 3, 1, properties);
        }

        // Returns mouse coordinates: (x,y) in pixels and (z,w) normalized inside the render target (not the viewport)
        public static Vector4 GetMouseCoordinates(HDCamera camera)
        {
            // We request the mouse post based on the type of the camera
            Vector2 mousePixelCoord = MousePositionDebug.instance.GetMousePosition(camera.screenSize.y, camera.camera.cameraType == CameraType.SceneView);
            return new Vector4(mousePixelCoord.x, mousePixelCoord.y, RTHandles.rtHandleProperties.rtHandleScale.x * mousePixelCoord.x / camera.screenSize.x, RTHandles.rtHandleProperties.rtHandleScale.y * mousePixelCoord.y / camera.screenSize.y);
        }

        // Returns mouse click coordinates: (x,y) in pixels and (z,w) normalized inside the render target (not the viewport)
        public static Vector4 GetMouseClickCoordinates(HDCamera camera)
        {
            Vector2 mousePixelCoord = MousePositionDebug.instance.GetMouseClickPosition(camera.screenSize.y);
            return new Vector4(mousePixelCoord.x, mousePixelCoord.y, RTHandles.rtHandleProperties.rtHandleScale.x * mousePixelCoord.x / camera.screenSize.x, RTHandles.rtHandleProperties.rtHandleScale.y * mousePixelCoord.y / camera.screenSize.y);
        }

        // This function check if camera is a CameraPreview, then check if this preview is a regular preview (i.e not a preview from the camera editor)
        public static bool IsRegularPreviewCamera(Camera camera)
        {
            var additionalCameraData = camera.GetComponent<HDAdditionalCameraData>();
            return camera.cameraType == CameraType.Preview && ((additionalCameraData == null) || (additionalCameraData && !additionalCameraData.isEditorCameraPreview));
        }

        // We need these at runtime for RenderPipelineResources upgrade
        public static string GetHDRenderPipelinePath()
            => "Packages/com.unity.render-pipelines.high-definition/";

        public static string GetCorePath()
            => "Packages/com.unity.render-pipelines.core/";

        public struct PackedMipChainInfo
        {
            public Vector2Int textureSize;
            public int mipLevelCount;
            public Vector2Int[] mipLevelSizes;
            public Vector2Int[] mipLevelOffsets;

            private bool m_OffsetBufferWillNeedUpdate;

            public void Allocate()
            {
                mipLevelOffsets = new Vector2Int[15];
                mipLevelSizes = new Vector2Int[15];
                m_OffsetBufferWillNeedUpdate = true;
            }

            // We pack all MIP levels into the top MIP level to avoid the Pow2 MIP chain restriction.
            // We compute the required size iteratively.
            // This function is NOT fast, but it is illustrative, and can be optimized later.
            public void ComputePackedMipChainInfo(Vector2Int viewportSize)
            {
                textureSize = viewportSize;
                mipLevelSizes[0] = viewportSize;
                mipLevelOffsets[0] = Vector2Int.zero;

                int mipLevel = 0;
                Vector2Int mipSize = viewportSize;

                do
                {
                    mipLevel++;

                    // Round up.
                    mipSize.x = Math.Max(1, (mipSize.x + 1) >> 1);
                    mipSize.y = Math.Max(1, (mipSize.y + 1) >> 1);

                    mipLevelSizes[mipLevel] = mipSize;

                    Vector2Int prevMipBegin = mipLevelOffsets[mipLevel - 1];
                    Vector2Int prevMipEnd = prevMipBegin + mipLevelSizes[mipLevel - 1];

                    Vector2Int mipBegin = new Vector2Int();

                    if ((mipLevel & 1) != 0) // Odd
                    {
                        mipBegin.x = prevMipBegin.x;
                        mipBegin.y = prevMipEnd.y;
                    }
                    else // Even
                    {
                        mipBegin.x = prevMipEnd.x;
                        mipBegin.y = prevMipBegin.y;
                    }

                    mipLevelOffsets[mipLevel] = mipBegin;

                    textureSize.x = Math.Max(textureSize.x, mipBegin.x + mipSize.x);
                    textureSize.y = Math.Max(textureSize.y, mipBegin.y + mipSize.y);

                } while ((mipSize.x > 1) || (mipSize.y > 1));

                mipLevelCount = mipLevel + 1;
                m_OffsetBufferWillNeedUpdate = true;
            }

            public ComputeBuffer GetOffsetBufferData(ComputeBuffer mipLevelOffsetsBuffer)
            {

                if (m_OffsetBufferWillNeedUpdate)
                {
                    mipLevelOffsetsBuffer.SetData(mipLevelOffsets);
                    m_OffsetBufferWillNeedUpdate = false;
                }

                return mipLevelOffsetsBuffer;
            }
        }

        public static int DivRoundUp(int x, int y) => (x + y - 1) / y;

        public static bool IsQuaternionValid(Quaternion q)
            => (q[0] * q[0] + q[1] * q[1] + q[2] * q[2] + q[3] * q[3]) > float.Epsilon;

        // Note: If you add new platform in this function, think about adding support in IsSupportedBuildTarget() function below
        public static bool IsSupportedGraphicDevice(GraphicsDeviceType graphicDevice)
        {
            return (graphicDevice == GraphicsDeviceType.Direct3D11 ||
                    graphicDevice == GraphicsDeviceType.Direct3D12 ||
                    graphicDevice == GraphicsDeviceType.PlayStation4 ||
                    graphicDevice == GraphicsDeviceType.XboxOne ||
                    graphicDevice == GraphicsDeviceType.XboxOneD3D12 ||
                    graphicDevice == GraphicsDeviceType.Metal ||
                    graphicDevice == GraphicsDeviceType.Vulkan ||
                    graphicDevice == (GraphicsDeviceType)22 /*GraphicsDeviceType.Switch*/);
        }

        public static void CheckRTCreated(RenderTexture rt)
        {
            // In some cases when loading a project for the first time in the editor, the internal resource is destroyed.
            // When used as render target, the C++ code will re-create the resource automatically. Since here it's used directly as an UAV, we need to check manually
            if (!rt.IsCreated())
                rt.Create();
        }

        public static Vector4 ComputeUvScaleAndLimit(Vector2Int viewportResolution, Vector2Int bufferSize)
        {
            Vector2 rcpBufferSize = new Vector2(1.0f / bufferSize.x, 1.0f / bufferSize.y);

            // vp_scale = vp_dim / tex_dim.
            Vector2 uvScale = new Vector2(viewportResolution.x * rcpBufferSize.x,
                                          viewportResolution.y * rcpBufferSize.y);

            // clamp to (vp_dim - 0.5) / tex_dim.
            Vector2 uvLimit = new Vector2((viewportResolution.x - 0.5f) * rcpBufferSize.x,
                                          (viewportResolution.y - 0.5f) * rcpBufferSize.y);

            return new Vector4(uvScale.x, uvScale.y, uvLimit.x, uvLimit.y);
        }

#if UNITY_EDITOR
        // This function can't be in HDEditorUtils because we need it in HDRenderPipeline.cs (and HDEditorUtils is in an editor asmdef)
        public static bool IsSupportedBuildTarget(UnityEditor.BuildTarget buildTarget)
        {
            return (buildTarget == UnityEditor.BuildTarget.StandaloneWindows ||
                    buildTarget == UnityEditor.BuildTarget.StandaloneWindows64 ||
                    buildTarget == UnityEditor.BuildTarget.StandaloneLinux64 ||
#if !UNITY_2019_2_OR_NEWER
                    buildTarget == UnityEditor.BuildTarget.StandaloneLinuxUniversal ||
#endif
#if UNITY_2019_3_OR_NEWER
                    buildTarget == UnityEditor.BuildTarget.Stadia ||
#endif
                    buildTarget == UnityEditor.BuildTarget.StandaloneOSX ||
                    buildTarget == UnityEditor.BuildTarget.WSAPlayer ||
                    buildTarget == UnityEditor.BuildTarget.XboxOne ||
                    buildTarget == UnityEditor.BuildTarget.PS4 ||
                    buildTarget == UnityEditor.BuildTarget.iOS ||
                    buildTarget == UnityEditor.BuildTarget.Switch);
        }

        public static bool AreGraphicsAPIsSupported(UnityEditor.BuildTarget target, out GraphicsDeviceType unsupportedGraphicDevice)
        {
            unsupportedGraphicDevice = GraphicsDeviceType.Null;

            foreach (var graphicAPI in UnityEditor.PlayerSettings.GetGraphicsAPIs(target))
            {
                if (!HDUtils.IsSupportedGraphicDevice(graphicAPI))
                {
                    unsupportedGraphicDevice = graphicAPI;
                    return false;
                }
            }
            return true;
        }

        public static OperatingSystemFamily BuildTargetToOperatingSystemFamily(UnityEditor.BuildTarget target)
        {
            switch (target)
            {
                case UnityEditor.BuildTarget.StandaloneOSX:
                    return OperatingSystemFamily.MacOSX;
                case UnityEditor.BuildTarget.StandaloneWindows:
                case UnityEditor.BuildTarget.StandaloneWindows64:
                    return OperatingSystemFamily.Windows;
                case UnityEditor.BuildTarget.StandaloneLinux64:
#if !UNITY_2019_2_OR_NEWER
                case UnityEditor.BuildTarget.StandaloneLinuxUniversal:
#endif
#if UNITY_2019_3_OR_NEWER
                case UnityEditor.BuildTarget.Stadia:
#endif
                    return OperatingSystemFamily.Linux;
                default:
                    return OperatingSystemFamily.Other;
            }
        }

#endif

        public static bool IsOperatingSystemSupported(string os)
        {
            // Metal support depends on OS version:
            // macOS 10.11.x doesn't have tessellation / earlydepthstencil support, early driver versions were buggy in general
            // macOS 10.12.x should usually work with AMD, but issues with Intel/Nvidia GPUs. Regardless of the GPU, there are issues with MTLCompilerService crashing with some shaders
            // macOS 10.13.x is expected to work, and if it's a driver/shader compiler issue, there's still hope on getting it fixed to next shipping OS patch release
            //
            // Has worked experimentally with iOS in the past, but it's not currently supported
            //

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
            {
                if (os.StartsWith("Mac"))
                {
                    // TODO: Expose in C# version number, for now assume "Mac OS X 10.10.4" format with version 10 at least
                    int startIndex = os.LastIndexOf(" ");
                    var parts = os.Substring(startIndex + 1).Split('.');
                    int a = Convert.ToInt32(parts[0]);
                    int b = Convert.ToInt32(parts[1]);
                    // In case in the future there's a need to disable specific patch releases
                    // int c = Convert.ToInt32(parts[2]);

                    if (a < 10 || b < 13)
                        return false;
                }
            }

            return true;
        }

        public static void GetScaleAndBiasForLinearDistanceFade(float fadeDistance, out float scale, out float bias)
        {
            // Fade with distance calculation is just a linear fade from 90% of fade distance to fade distance. 90% arbitrarily chosen but should work well enough.
            float distanceFadeNear = 0.9f * fadeDistance;
            scale = 1.0f / (fadeDistance - distanceFadeNear);
            bias = -distanceFadeNear / (fadeDistance - distanceFadeNear);
        }
        public static float ComputeLinearDistanceFade(float distanceToCamera, float fadeDistance)
        {
            float scale;
            float bias;
            GetScaleAndBiasForLinearDistanceFade(fadeDistance, out scale, out bias);

            return 1.0f - Mathf.Clamp01(distanceToCamera * scale + bias);
        }

        public static bool PostProcessIsFinalPass()
        {
            // Post process pass is the final blit only when not in developer mode.
            // In developer mode, we support a range of debug rendering that needs to occur after post processes.
            // In order to simplify writing them, we don't Y-flip in the post process pass but add a final blit at the end of the frame.
            return !Debug.isDebugBuild;
        }

        // These two convertion functions are used to store GUID assets inside materials,
        // a unity asset GUID is exactly 16 bytes long which is also a Vector4 so by adding a
        // Vector4 field inside the shader we can store references of an asset inside the material
        // which is actually used to store the reference of the diffusion profile asset
        public static Vector4 ConvertGUIDToVector4(string guid)
        {
            Vector4 vector;
            byte[]  bytes = new byte[16];

            for (int i = 0; i < 16; i++)
                bytes[i] = byte.Parse(guid.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);

            unsafe
            {
                fixed (byte * b = bytes)
                    vector = *(Vector4 *)b;
            }

            return vector;
        }

        public static string ConvertVector4ToGUID(Vector4 vector)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            unsafe
            {
                byte * v = (byte *)&vector;
                for (int i = 0; i < 16; i++)
                    sb.Append(v[i].ToString("x2"));
                var guidBytes = new byte[16];
                System.Runtime.InteropServices.Marshal.Copy((IntPtr)v, guidBytes, 0, 16);
            }

            return sb.ToString();
        }

        public static Color NormalizeColor(Color color)
        {
            Vector4 ldrColor = Vector4.Max(color, Vector4.one * 0.0001f);
            color = (ldrColor / ColorUtils.Luminance(ldrColor));
            color.a = 1;

            return color;
        }

        public static void DrawRendererList(ScriptableRenderContext renderContext, CommandBuffer cmd, RendererList rendererList)
        {
            if (!rendererList.isValid)
                throw new ArgumentException("Invalid renderer list provided to DrawRendererList");

            // This is done here because DrawRenderers API lives outside command buffers so we need to make call this before doing any DrawRenders or things will be executed out of order
            renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            if (rendererList.stateBlock == null)
                renderContext.DrawRenderers(rendererList.cullingResult, ref rendererList.drawSettings, ref rendererList.filteringSettings);
            else
            {
                var renderStateBlock = rendererList.stateBlock.Value;
                renderContext.DrawRenderers(rendererList.cullingResult, ref rendererList.drawSettings, ref rendererList.filteringSettings, ref renderStateBlock);
            }
        }
    }
}
