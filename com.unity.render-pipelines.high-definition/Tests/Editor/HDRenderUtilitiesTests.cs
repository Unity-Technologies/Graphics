using NUnit.Framework;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline.Tests
{
    public class HDRenderUtilitiesTests
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
            Assert.Throws<ArgumentNullException>(() => HDRenderUtilities.Render(
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
                        frameSettings = new FrameSettings()
                    },
                    default(CameraPositionSettings),
                    Texture2D.whiteTexture
            ));
        }
    }
}
