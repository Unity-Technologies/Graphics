using NUnit.Framework;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
#endif

namespace UnityEngine.Rendering.Tests
{
    partial class RenderGraphTests
    {
        class TestBlitResources
        {
            public TextureHandle[] textures = new TextureHandle[2];
            public Material material;
            public RenderGraphUtils.BlitMaterialParameters blitParameters;
        };

        TestBlitResources CreateBlitResources(RenderGraph g)
        {
            TestBlitResources result = new TestBlitResources();

            result.material = new Material(Shader.Find("Hidden/BlitCopy"));

            for (int i = 0; i < result.textures.Length; i++)
            {
                result.textures[i] = g.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
            }

            result.blitParameters = new(result.textures[0], result.textures[1], result.material, 0);

            return result;
        }

        [Test]
        public void RenderPassAddBlitReturnBuilder()
        {
            var resources = CreateBlitResources(m_RenderGraph);

            var builderNull = m_RenderGraph.AddBlitPass(resources.blitParameters, "Test Pass", false);
            Assert.IsNull(builderNull);

            var builder = m_RenderGraph.AddBlitPass(resources.blitParameters, "Test Pass", true);
            Assert.IsNotNull(builder);
            builder.Dispose();

            builderNull = m_RenderGraph.AddBlitPass(resources.textures[0], resources.textures[1], Vector2.one, Vector2.zero, passName: "Test Pass", returnBuilder: false);
            Assert.IsNull(builderNull);

            builder = m_RenderGraph.AddBlitPass(resources.textures[0], resources.textures[1], Vector2.one, Vector2.zero, passName: "Test Pass", returnBuilder: true);
            Assert.IsNotNull(builder);
            builder.Dispose();
        }

        [Test]
        public void RenderPassAddBlitSetGlobal()
        {
            int texture0ID = 0;
            int texture1ID = 1;
            var resources = CreateBlitResources(m_RenderGraph);

            using (var builder = m_RenderGraph.AddBlitPass(resources.blitParameters, "Test Pass", true))
            {
                builder.SetGlobalTextureAfterPass(resources.textures[0], texture0ID);
            }
            Assert.IsTrue(m_RenderGraph.IsGlobal(texture0ID));

            using (var builder = m_RenderGraph.AddBlitPass(resources.textures[0], resources.textures[1], Vector2.one, Vector2.zero, passName: "Test Pass", returnBuilder: true))
            {
                builder.SetGlobalTextureAfterPass(resources.textures[1], texture1ID);
            }
            Assert.IsTrue(m_RenderGraph.IsGlobal(texture1ID));
        }


        [Test]
        public void RenderPassAddBlitUseTexture()
        {
            var resources = CreateBlitResources(m_RenderGraph);
            var canUseCopyPass = RenderGraphUtils.CanAddCopyPass(m_RenderGraph, resources.blitParameters.source, resources.blitParameters.destination);

            // Writing to the texture blitting is the same as writing the same texture twice, is not allowed.
            using (var builder = m_RenderGraph.AddBlitPass(resources.blitParameters, "BlitPass", true))
            {
                Assert.Throws<System.InvalidOperationException>(() =>
                {
                    builder.UseTexture(resources.textures[1], AccessFlags.Write);
                });
            }

            // Writing to the texture blitting is the same as writing the same texture twice, is not allowed.
            using (var builder = m_RenderGraph.AddBlitPass(resources.textures[0], resources.textures[1], Vector2.one, Vector2.zero, passName: "Test Pass", returnBuilder: true))
            {
                // The CopyPass should throw the following exception:
                // "<System.ArgumentException: Trying to UseTexture on a texture that is already used through SetRenderAttachment. Consider updating your code. (pass BlitPass resource)."
                if (canUseCopyPass)
                {
                    Assert.Throws<System.ArgumentException>(() =>
                    {
                        builder.UseTexture(resources.textures[1], AccessFlags.Write);
                    });
                }
                // The BlitPass should throw the following exception:
                // "<System.InvalidOperationException: Trying to write a resource twice in a pass. You can only write the same resource once within a pass (pass BlitPass resource)."
                else
                {
                    Assert.Throws<System.InvalidOperationException>(() =>
                    {
                        builder.UseTexture(resources.textures[1], AccessFlags.Write);
                    });
                }
            }

            // Reading the same texture twice is allowed
            using (var builder = m_RenderGraph.AddBlitPass(resources.blitParameters, "BlitPass", true))
            {
                builder.UseTexture(resources.textures[0], AccessFlags.Read);
            }

            // Reading the same texture twice is allowed
            using (var builder = m_RenderGraph.AddBlitPass(resources.textures[0], resources.textures[1], Vector2.one, Vector2.zero, passName: "Test Pass", returnBuilder: true))
            {
                builder.UseTexture(resources.textures[0], AccessFlags.Read);
            }
        }

        [Test]
        public void RenderPassAddBlitNullSourceSupport()
        {
            var resources = CreateBlitResources(m_RenderGraph);
            resources.blitParameters.source = TextureHandle.nullHandle;

            // Using a Null Source support using a custom shader is allowed
            Assert.DoesNotThrow(delegate { m_RenderGraph.AddBlitPass(resources.blitParameters, "BlitPassNullSource"); });

            // Using a Null Source support using a internal blit shader is not allowed and throws an exception
            Assert.Throws<System.ArgumentException>(() =>
            {
                m_RenderGraph.AddBlitPass(resources.blitParameters.source, resources.blitParameters.destination, Vector2.one, Vector2.zero, passName: "BlitPass2");
            });
        }

        [Test]
        public void RenderPassAddBlitBackbufferTarget()
        {
            var resources = CreateBlitResources(m_RenderGraph);
            resources.blitParameters.destination = m_RenderGraph.ImportBackbuffer(0);
            
            // Using a backbuffer as destination is allowed
            Assert.DoesNotThrow(delegate { m_RenderGraph.AddBlitPass(resources.blitParameters, "BlitPassBackbufferTarget0"); });
            Assert.DoesNotThrow(delegate { m_RenderGraph.AddBlitPass(resources.blitParameters.source, resources.blitParameters.destination, Vector2.one, Vector2.zero, passName: "BlitPassBackbufferTarget1"); });
            
            resources.blitParameters.destination = resources.textures[1];
            resources.blitParameters.source = m_RenderGraph.ImportBackbuffer(0);
            
            // Using a backbuffer as source is not allowed and throws an exception
            Assert.Throws<System.ArgumentException>(() =>
            {
                m_RenderGraph.AddBlitPass(resources.blitParameters, "BlitPassBackbufferSource0");
            });
            Assert.Throws<System.ArgumentException>(() =>
            {
                m_RenderGraph.AddBlitPass(resources.blitParameters.source, resources.blitParameters.destination, Vector2.one, Vector2.zero, passName: "BlitPassBackbufferSource1");
            });
        }
    }
}
