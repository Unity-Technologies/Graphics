using System;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class that holds settings related to texture resources.
    /// </summary>
    internal class Universal2DResourceData : UniversalResourceDataBase
    {
        TextureHandle[][] CheckAndGetTextureHandle(ref TextureHandle[][] handle)
        {
            if (!CheckAndWarnAboutAccessibility())
                return new TextureHandle[][] { new TextureHandle[] { TextureHandle.nullHandle } };

            return handle;
        }

        void CheckAndSetTextureHandle(ref TextureHandle[][] handle, TextureHandle[][] newHandle)
        {
            if (!CheckAndWarnAboutAccessibility())
                return;

            if (handle == null || handle.Length != newHandle.Length)
                handle = new TextureHandle[newHandle.Length][];

            for (int i = 0; i < newHandle.Length; i++)
                handle[i] = newHandle[i];
        }

        internal TextureHandle[][] lightTextures
        {
            get => CheckAndGetTextureHandle(ref _lightTextures);
            set => CheckAndSetTextureHandle(ref _lightTextures, value);
        }
        private TextureHandle[][] _lightTextures = new TextureHandle[0][];

        internal TextureHandle[] normalsTexture
        {
            get => CheckAndGetTextureHandle(ref _cameraNormalsTexture);
            set => CheckAndSetTextureHandle(ref _cameraNormalsTexture, value);
        }
        private TextureHandle[] _cameraNormalsTexture = new TextureHandle[0];

        internal TextureHandle[][] shadowTextures
        {
            get => CheckAndGetTextureHandle(ref _shadowTextures);
            set => CheckAndSetTextureHandle(ref _shadowTextures, value);
        }
        private TextureHandle[][] _shadowTextures = new TextureHandle[0][];

        internal TextureHandle shadowDepth
        {
            get => CheckAndGetTextureHandle(ref _shadowDepth);
            set => CheckAndSetTextureHandle(ref _shadowDepth, value);
        }
        private TextureHandle _shadowDepth;

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

        /// <inheritdoc />
        public override void Reset()
        {
            _shadowDepth = TextureHandle.nullHandle;
            _upscaleTexture = TextureHandle.nullHandle;
            _cameraSortingLayerTexture = TextureHandle.nullHandle;

            for (int i = 0; i < _cameraNormalsTexture.Length; i++)
                _cameraNormalsTexture[i] = TextureHandle.nullHandle;

            for (int i = 0; i < _shadowTextures.Length; i++)
                for (int j = 0; j < _shadowTextures[i].Length; j++)
                    _shadowTextures[i][j] = TextureHandle.nullHandle;

            for (int i = 0; i < _lightTextures.Length; i++)
                for (int j = 0; j < _lightTextures[i].Length; j++)
                    _lightTextures[i][j] = TextureHandle.nullHandle;
        }
    }
}
