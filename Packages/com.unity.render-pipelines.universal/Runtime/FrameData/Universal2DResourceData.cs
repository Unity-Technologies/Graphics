using System;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class that holds settings related to texture resources.
    /// </summary>
    public class Universal2DResourceData : UniversalResourceDataBase
    {
        /// <summary>
        /// The active color target ID.
        /// </summary>
        public ActiveID activeColorID { get; internal set; }

        /// <summary>
        /// Returns the current active color target texture. To be referenced at RenderGraph pass recording time, not in passes render functions.
        /// </summary>
        /// <returns>TextureHandle</returns>
        internal TextureHandle activeColorTexture
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
        public ActiveID activeDepthID { get; internal set; }

        /// <summary>
        /// Returns the current active color target texture. To be referenced at RenderGraph pass recording time, not in passes render functions.
        /// </summary>
        /// <returns>TextureHandle</returns>
        internal TextureHandle activeDepthTexture
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
        /// The backbuffer color used to render directly to screen. All passes can write to it depending on frame setup.
        /// </summary>
        internal TextureHandle backBufferColor
        {
            get => CheckAndGetTextureHandle(ref _backBufferColor);
            set => CheckAndSetTextureHandle(ref _backBufferColor, value);
        }
        private TextureHandle _backBufferColor;


        /// <summary>
        /// The backbuffer depth used to render directly to screen. All passes can write to it depending on frame setup.
        /// </summary>
        internal TextureHandle backBufferDepth
        {
            get => CheckAndGetTextureHandle(ref _backBufferDepth);
            set => CheckAndSetTextureHandle(ref _backBufferDepth, value);
        }
        private TextureHandle _backBufferDepth;

        internal TextureHandle cameraColor
        {
            get => CheckAndGetTextureHandle(ref _cameraColor);
            set => CheckAndSetTextureHandle(ref _cameraColor, value);
        }
        private TextureHandle _cameraColor;

        internal TextureHandle cameraDepth
        {
            get => CheckAndGetTextureHandle(ref _cameraDepth);
            set => CheckAndSetTextureHandle(ref _cameraDepth, value);
        }
        private TextureHandle _cameraDepth;

        internal TextureHandle intermediateDepth
        {
            get => CheckAndGetTextureHandle(ref _intermediateDepth);
            set => CheckAndSetTextureHandle(ref _intermediateDepth, value);
        }
        private TextureHandle _intermediateDepth;

        internal TextureHandle[] lightTextures
        {
            get => CheckAndGetTextureHandle(ref _lightTextures);
            set => CheckAndSetTextureHandle(ref _lightTextures, value);
        }
        private TextureHandle[] _lightTextures = new TextureHandle[RenderGraphUtils.LightTextureSize];


        internal TextureHandle normalsTexture
        {
            get => CheckAndGetTextureHandle(ref _cameraNormalsTexture);
            set => CheckAndSetTextureHandle(ref _cameraNormalsTexture, value);
        }
        private TextureHandle _cameraNormalsTexture;

        internal TextureHandle shadowsTexture
        {
            get => CheckAndGetTextureHandle(ref _shadowsTexture);
            set => CheckAndSetTextureHandle(ref _shadowsTexture, value);
        }
        private TextureHandle _shadowsTexture;

        internal TextureHandle upscaleTexture
        {
            get => CheckAndGetTextureHandle(ref _upscaleTexture);
            set => CheckAndSetTextureHandle(ref _upscaleTexture, value);
        }
        private TextureHandle _upscaleTexture;

        internal TextureHandle cameraSortingLayerTexture
        {
            get => CheckAndGetTextureHandle(ref _cameraSortingLayerTexture);
            set => CheckAndSetTextureHandle(ref _cameraSortingLayerTexture, value);
        }
        private TextureHandle _cameraSortingLayerTexture;

        internal TextureHandle internalColorLut
        {
            get => CheckAndGetTextureHandle(ref _internalColorLut);
            set => CheckAndSetTextureHandle(ref _internalColorLut, value);
        }
        private TextureHandle _internalColorLut;

        internal TextureHandle afterPostProcessColor
        {
            get => CheckAndGetTextureHandle(ref _afterPostProcessColor);
            set => CheckAndSetTextureHandle(ref _afterPostProcessColor, value);
        }
        private TextureHandle _afterPostProcessColor;

        internal TextureHandle debugScreenColor
        {
            get => CheckAndGetTextureHandle(ref _debugScreenColor);
            set => CheckAndSetTextureHandle(ref _debugScreenColor, value);
        }
        private TextureHandle _debugScreenColor;

        internal TextureHandle debugScreenDepth
        {
            get => CheckAndGetTextureHandle(ref _debugScreenDepth);
            set => CheckAndSetTextureHandle(ref _debugScreenDepth, value);
        }
        private TextureHandle _debugScreenDepth;

        /// <summary>
        /// Overlay UI Texture. The DrawScreenSpaceUI pass writes to this texture when rendering off-screen.
        /// </summary>
        internal TextureHandle overlayUITexture
        {
            get => CheckAndGetTextureHandle(ref _overlayUITexture);
            set => CheckAndSetTextureHandle(ref _overlayUITexture, value);
        }
        private TextureHandle _overlayUITexture;


        /// <inheritdoc />
        public override void Reset()
        {
            _backBufferColor = TextureHandle.nullHandle;
            _backBufferDepth = TextureHandle.nullHandle;
            _cameraColor = TextureHandle.nullHandle;
            _cameraDepth = TextureHandle.nullHandle;
            _cameraNormalsTexture = TextureHandle.nullHandle;
            _shadowsTexture = TextureHandle.nullHandle;
            _upscaleTexture = TextureHandle.nullHandle;
            _cameraSortingLayerTexture = TextureHandle.nullHandle;
            _internalColorLut = TextureHandle.nullHandle;
            _debugScreenDepth = TextureHandle.nullHandle;
            _afterPostProcessColor = TextureHandle.nullHandle;
            _overlayUITexture = TextureHandle.nullHandle;

            for (int i = 0; i < _lightTextures.Length; i++)
                _lightTextures[i] = TextureHandle.nullHandle;
        }


    }
}
