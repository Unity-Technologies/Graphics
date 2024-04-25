using System;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class that holds settings related to texture resources.
    /// </summary>
    public class UniversalResourceData : UniversalResourceDataBase
    {
        /// <summary>
        /// The active color target ID.
        /// </summary>
        internal ActiveID activeColorID { get; set; }

        /// <summary>
        /// Returns the current active color target texture. To be referenced at RenderGraph pass recording time, not in passes render functions.
        /// </summary>
        /// <value>Returns the active color texture between the front and back buffer.</value>
        public TextureHandle activeColorTexture
        {
            get
            {
                if (!CheckAndWarnAboutAccessibility())
                    return TextureHandle.nullHandle;

                switch (activeColorID)
                {
                    case ActiveID.Camera:
                        return cameraColor;
                    case ActiveID.BackBuffer:
                        return backBufferColor;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// The active depth target ID.
        /// </summary>
        internal ActiveID activeDepthID { get; set; }

        /// <summary>
        /// Returns the current active color target texture. To be referenced at RenderGraph pass recording time, not in passes render functions.
        /// </summary>
        /// <value>TextureHandle</value>
        public TextureHandle activeDepthTexture
        {
            get
            {
                if (!CheckAndWarnAboutAccessibility())
                    return TextureHandle.nullHandle;

                switch (activeDepthID)
                {
                    case ActiveID.Camera:
                        return cameraDepth;
                    case ActiveID.BackBuffer:
                        return backBufferDepth;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// True if the current active target is the backbuffer. To be referenced at RenderGraph pass recording time, not in passes render functions.
        /// </summary>
        /// <value>Returns true if the backbuffer is currently in use and false otherwise.</value>
        public bool isActiveTargetBackBuffer
        {
            get
            {
                if (!isAccessible)
                {
                    Debug.LogError("Trying to access frameData outside of the current frame setup.");
                    return false;
                }

                return activeColorID == UniversalResourceData.ActiveID.BackBuffer;
            }
        }


        /// <summary>
        /// The backbuffer color used to render directly to screen. All passes can write to it depending on frame setup.
        /// </summary>
        public TextureHandle backBufferColor
        {
            get => CheckAndGetTextureHandle(ref _backBufferColor);
            internal set => CheckAndSetTextureHandle(ref _backBufferColor, value);
        }
        private TextureHandle _backBufferColor;


        /// <summary>
        /// The backbuffer depth used to render directly to screen. All passes can write to it depending on frame setup.
        /// </summary>
        public TextureHandle backBufferDepth
        {
            get => CheckAndGetTextureHandle(ref _backBufferDepth);
            internal set => CheckAndSetTextureHandle(ref _backBufferDepth, value);
        }
        private TextureHandle _backBufferDepth;

        // intermediate camera targets

        /// <summary>
        /// Main offscreen camera color target. All passes can write to it depending on frame setup.
        /// Can hold multiple samples if MSAA is enabled.
        /// </summary>
        public TextureHandle cameraColor
        {
            get => CheckAndGetTextureHandle(ref _cameraColor);
            set => CheckAndSetTextureHandle(ref _cameraColor, value);
        }
        private TextureHandle _cameraColor;

        /// <summary>
        /// Main offscreen camera depth target. All passes can write to it depending on frame setup.
        /// Can hold multiple samples if MSAA is enabled.
        /// </summary>
        public TextureHandle cameraDepth
        {
            get => CheckAndGetTextureHandle(ref _cameraDepth);
            set => CheckAndSetTextureHandle(ref _cameraDepth, value);
        }
        private TextureHandle _cameraDepth;

        // shadows

        /// <summary>
        /// Main shadow map.
        /// </summary>
        public TextureHandle mainShadowsTexture
        {
            get => CheckAndGetTextureHandle(ref _mainShadowsTexture);
            set => CheckAndSetTextureHandle(ref _mainShadowsTexture, value);
        }
        private TextureHandle _mainShadowsTexture;

        /// <summary>
        /// Additional shadow map.
        /// </summary>
        public TextureHandle additionalShadowsTexture
        {
            get => CheckAndGetTextureHandle(ref _additionalShadowsTexture);
            set => CheckAndSetTextureHandle(ref _additionalShadowsTexture, value);
        }
        private TextureHandle _additionalShadowsTexture;

        // GBuffer targets

        /// <summary>
        /// GBuffer. Written to by the GBuffer pass.
        /// </summary>
        public TextureHandle[] gBuffer
        {
            get => CheckAndGetTextureHandle(ref _gBuffer);
            set => CheckAndSetTextureHandle(ref _gBuffer, value);
        }
        private TextureHandle[] _gBuffer = new TextureHandle[RenderGraphUtils.GBufferSize];

        // camera opaque/depth/normal

        /// <summary>
        /// Camera opaque texture. Contains a copy of CameraColor if the CopyColor pass is executed.
        /// </summary>
        public TextureHandle cameraOpaqueTexture
        {
            get => CheckAndGetTextureHandle(ref _cameraOpaqueTexture);
            internal set => CheckAndSetTextureHandle(ref _cameraOpaqueTexture, value);
        }
        private TextureHandle _cameraOpaqueTexture;

        /// <summary>
        /// Camera depth texture. Contains the scene depth if the CopyDepth or Depth Prepass passes are executed.
        /// </summary>
        public TextureHandle cameraDepthTexture
        {
            get => CheckAndGetTextureHandle(ref _cameraDepthTexture);
            internal set => CheckAndSetTextureHandle(ref _cameraDepthTexture, value);
        }
        private TextureHandle _cameraDepthTexture;

        /// <summary>
        /// Camera normals texture. Contains the scene depth if the DepthNormals Prepass pass is executed.
        /// </summary>
        public TextureHandle cameraNormalsTexture
        {
            get => CheckAndGetTextureHandle(ref _cameraNormalsTexture);
            internal set => CheckAndSetTextureHandle(ref _cameraNormalsTexture, value);
        }
        private TextureHandle _cameraNormalsTexture;

        // motion vector

        /// <summary>
        /// Motion Vector Color. Written to by the Motion Vector passes.
        /// </summary>
        public TextureHandle motionVectorColor
        {
            get => CheckAndGetTextureHandle(ref _motionVectorColor);
            set => CheckAndSetTextureHandle(ref _motionVectorColor, value);
        }
        private TextureHandle _motionVectorColor;

        /// <summary>
        /// Motion Vector Depth. Written to by the Motion Vector passes.
        /// </summary>
        public TextureHandle motionVectorDepth
        {
            get => CheckAndGetTextureHandle(ref _motionVectorDepth);
            set => CheckAndSetTextureHandle(ref _motionVectorDepth, value);
        }
        private TextureHandle _motionVectorDepth;

        // postFx

        /// <summary>
        /// Internal Color LUT. Written to by the InternalLUT pass.
        /// </summary>
        public TextureHandle internalColorLut
        {
            get => CheckAndGetTextureHandle(ref _internalColorLut);
            set => CheckAndSetTextureHandle(ref _internalColorLut, value);
        }
        private TextureHandle _internalColorLut;

        /// <summary>
        /// Color output of post-process passes (uberPost and finalPost) when HDR debug views are enabled. It replaces
        /// the backbuffer color as standard output because the later cannot be sampled back (or may not be in HDR format).
        /// If used, DebugHandler will perform the blit from DebugScreenTexture to BackBufferColor.
        /// </summary>
        internal TextureHandle debugScreenColor
        {
            get => CheckAndGetTextureHandle(ref _debugScreenColor);
            set => CheckAndSetTextureHandle(ref _debugScreenColor, value);
        }
        internal TextureHandle _debugScreenColor;

        /// <summary>
        /// Depth output of post-process passes (uberPost and finalPost) when HDR debug views are enabled. It replaces
        /// the backbuffer depth as standard output because the later cannot be sampled back.
        /// </summary>
        internal TextureHandle debugScreenDepth
        {
            get => CheckAndGetTextureHandle(ref _debugScreenDepth);
            set => CheckAndSetTextureHandle(ref _debugScreenDepth, value);
        }
        internal TextureHandle _debugScreenDepth;

        /// <summary>
        /// After Post Process Color. Stores the contents of the main color target after the post processing passes.
        /// </summary>
        public TextureHandle afterPostProcessColor
        {
            get => CheckAndGetTextureHandle(ref _afterPostProcessColor);
            internal set => CheckAndSetTextureHandle(ref _afterPostProcessColor, value);
        }
        private TextureHandle _afterPostProcessColor;

        /// <summary>
        /// Overlay UI Texture. The DrawScreenSpaceUI pass writes to this texture when rendering off-screen.
        /// </summary>
        public TextureHandle overlayUITexture
        {
            get => CheckAndGetTextureHandle(ref _overlayUITexture);
            internal set => CheckAndSetTextureHandle(ref _overlayUITexture, value);
        }
        private TextureHandle _overlayUITexture;

        // rendering layers

        /// <summary>
        /// Rendering Layers Texture. Can be written to by the DrawOpaques pass or DepthNormals prepass based on settings.
        /// </summary>
        public TextureHandle renderingLayersTexture
        {
            get => CheckAndGetTextureHandle(ref _renderingLayersTexture);
            internal set => CheckAndSetTextureHandle(ref _renderingLayersTexture, value);
        }
        private TextureHandle _renderingLayersTexture;

        // decals

        /// <summary>
        /// DBuffer. Written to by the Decals pass.
        /// </summary>
        public TextureHandle[] dBuffer
        {
            get => CheckAndGetTextureHandle(ref _dBuffer);
            set => CheckAndSetTextureHandle(ref _dBuffer, value);
        }
        private TextureHandle[] _dBuffer = new TextureHandle[RenderGraphUtils.DBufferSize];

        /// <summary>
        /// DBufferDepth. Written to by the Decals pass.
        /// </summary>
        public TextureHandle dBufferDepth
        {
            get => CheckAndGetTextureHandle(ref _dBufferDepth);
            set => CheckAndSetTextureHandle(ref _dBufferDepth, value);
        }
        private TextureHandle _dBufferDepth;

        /// <summary>
        /// Screen Space Ambient Occlusion texture. Written to by the SSAO pass.
        /// </summary>
        public TextureHandle ssaoTexture
        {
            get => CheckAndGetTextureHandle(ref _ssaoTexture);
            internal set => CheckAndSetTextureHandle(ref _ssaoTexture, value);
        }
        private TextureHandle _ssaoTexture;

        /// <summary>
        /// STP debug visualization written to by the STP upscaler.
        /// </summary>
        internal TextureHandle stpDebugView
        {
            get => CheckAndGetTextureHandle(ref _stpDebugView);
            set => CheckAndSetTextureHandle(ref _stpDebugView, value);
        }
        private TextureHandle _stpDebugView;

        /// <inheritdoc />
        public override void Reset()
        {
            _backBufferColor = TextureHandle.nullHandle;
            _backBufferDepth = TextureHandle.nullHandle;
            _cameraColor = TextureHandle.nullHandle;
            _cameraDepth = TextureHandle.nullHandle;
            _mainShadowsTexture = TextureHandle.nullHandle;
            _additionalShadowsTexture = TextureHandle.nullHandle;
            _cameraOpaqueTexture = TextureHandle.nullHandle;
            _cameraDepthTexture = TextureHandle.nullHandle;
            _cameraNormalsTexture = TextureHandle.nullHandle;
            _motionVectorColor = TextureHandle.nullHandle;
            _motionVectorDepth = TextureHandle.nullHandle;
            _internalColorLut = TextureHandle.nullHandle;
            _debugScreenColor = TextureHandle.nullHandle;
            _debugScreenDepth = TextureHandle.nullHandle;
            _afterPostProcessColor = TextureHandle.nullHandle;
            _overlayUITexture = TextureHandle.nullHandle;
            _renderingLayersTexture = TextureHandle.nullHandle;
            _dBufferDepth = TextureHandle.nullHandle;
            _ssaoTexture = TextureHandle.nullHandle;
            _stpDebugView = TextureHandle.nullHandle;

            for (int i = 0; i < _gBuffer.Length; i++)
                _gBuffer[i] = TextureHandle.nullHandle;

            for (int i = 0; i < _dBuffer.Length; i++)
                _dBuffer[i] = TextureHandle.nullHandle;
        }
    }
}
