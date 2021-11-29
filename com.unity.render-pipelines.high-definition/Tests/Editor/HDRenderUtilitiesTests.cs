using NUnit.Framework;
using System;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    class HDRenderUtilitiesTests
    {
        [Test]
        public void RenderThrowWhenTargetIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => HDRenderUtilities.Render(
                default(CameraSettings),
                default(CameraPositionSettings),
                null
            ));
        }

        [Test]
        public void RenderThrowWhenFrameSettingsIsNull()
        {
            // should throw error that Texture2D is no RenderTexture
            Assert.Throws<ArgumentException>(() => HDRenderUtilities.Render(
                default(CameraSettings),
                default(CameraPositionSettings),
                Texture2D.whiteTexture
            ));
        }

        [Test]
        public void RenderThrowWhenTargetIsNotARenderTextureForTex2DRendering()
        {
            Assert.Throws<ArgumentException>(() => HDRenderUtilities.Render(
                new CameraSettings
                {
                    renderingPathCustomFrameSettings = default
                },
                default(CameraPositionSettings),
                Texture2D.whiteTexture
            ));
        }
    }
}
