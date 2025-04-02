using NUnit.Framework;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Tests
{
    partial class RenderGraphTests
    {
        class RegularMethodInRegularClass
        {
            public void RenderFunc(RenderGraphTestPassData data, RenderGraphContext context)
            {
            }
        }

        static class StaticMethodInsideStaticClass
        {
            public static void RenderFunc(RenderGraphTestPassData data, RenderGraphContext context)
            {
            }
        }

        class StaticMethodInsideRegularClass
        {
            public static void RenderFunc(RenderGraphTestPassData data, RenderGraphContext context)
            {
            }

            public static void RenderFunc2(RenderGraphTestPassData data, RenderGraphContext context)
            {
            }
        }

        class StaticMethodInsideRegularClass2
        {
            public static void RenderFunc(RenderGraphTestPassData data, RenderGraphContext context)
            {
            }
        }

        void ClearCompiledGraphAndHash()
        {
            m_RenderGraph.ClearCurrentCompiledGraph();
            DelegateHashCodeUtils.ClearCache();
        }

        [Test]
        public void ComputeGraphHash_WhenCalledMultipleTimes_CacheForDelegatesIsNotGrowingBetweenComputes()
        {
            //Method of the class instance
            TextureHandle texture0 = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.UseColorBuffer(texture0, 0);
                var firstInstance = new RegularMethodInRegularClass();
                builder.SetRenderFunc<RenderGraphTestPassData>(firstInstance.RenderFunc);
            }

            //Static method of the static class
            TextureHandle texture1 = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.UseColorBuffer(texture1, 0);
                builder.SetRenderFunc<RenderGraphTestPassData>(StaticMethodInsideStaticClass.RenderFunc);
            }

            //Lambdas with captured variable
            TextureHandle texture2 = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.UseColorBuffer(texture2, 0);
                builder.SetRenderFunc<RenderGraphTestPassData>((data, context) => { Debug.Log(texture2.GetHashCode()); });
            }

            //Local method with captured variable
            TextureHandle texture3 = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass3", out var passData))
            {
                builder.UseColorBuffer(texture3, 0);
                builder.SetRenderFunc<RenderGraphTestPassData>(LocalMethod);
            }

            void LocalMethod(RenderGraphTestPassData data, RenderGraphContext renderGraphContext)
            {
                Debug.Log(texture3.GetHashCode());
            }

            //Static method of the regular class
            TextureHandle texture4 = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass4", out var passData))
            {
                builder.UseColorBuffer(texture4, 0);
                builder.SetRenderFunc<RenderGraphTestPassData>(StaticMethodInsideRegularClass.RenderFunc);
            }

            //Calculate delegate cache first time
            m_RenderGraph.ComputeGraphHash();
            var initialCacheSize = DelegateHashCodeUtils.GetTotalCacheCount();

            //Trigger multiple hash recalculations
            m_RenderGraph.ComputeGraphHash();
            m_RenderGraph.ComputeGraphHash();
            m_RenderGraph.ComputeGraphHash();
            var cacheAfterMultipleCalculations = DelegateHashCodeUtils.GetTotalCacheCount();

            Assert.That(initialCacheSize, Is.EqualTo(cacheAfterMultipleCalculations));
        }

        [Test]
        public void ComputeGraphHash_WhenDifferentObjectsUsed_HashcodeIsDifferent()
        {
            RecordRenderGraph(m_RenderGraph, new RegularMethodInRegularClass());

            var hash0 = m_RenderGraph.ComputeGraphHash();
            ClearCompiledGraphAndHash();

            RecordRenderGraph(m_RenderGraph, new RegularMethodInRegularClass());

            var hash1 = m_RenderGraph.ComputeGraphHash();
            ClearCompiledGraphAndHash();

            Assert.AreNotEqual(hash0, hash1);

            void RecordRenderGraph(RenderGraph renderGraph, RegularMethodInRegularClass instance)
            {
                using var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData);
                builder.SetRenderFunc<RenderGraphTestPassData>(instance.RenderFunc);
            }
        }

        [Test]
        public void ComputeGraphHash_WhenDifferentStaticMethodsWithTheSameNameUsed_HashcodeIsDifferent()
        {
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
                builder.SetRenderFunc<RenderGraphTestPassData>(StaticMethodInsideRegularClass.RenderFunc);

            var hash0 = m_RenderGraph.ComputeGraphHash();
            ClearCompiledGraphAndHash();

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
                builder.SetRenderFunc<RenderGraphTestPassData>(StaticMethodInsideRegularClass2.RenderFunc);

            var hash1 = m_RenderGraph.ComputeGraphHash();
            ClearCompiledGraphAndHash();

            Assert.AreNotEqual(hash0, hash1);
        }

        [Test]
        public void ComputeGraphHash_WhenManyDifferentPassesUsed_HashcodeIsDifferent()
        {
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
            }

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
            }

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
            }

            var hash0 = m_RenderGraph.ComputeGraphHash();
            ClearCompiledGraphAndHash();

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
            }

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
            }

            var hash1 = m_RenderGraph.ComputeGraphHash();
            ClearCompiledGraphAndHash();

            Assert.AreNotEqual(hash0, hash1);
        }

        static TestCaseData[] s_TextureParametersCases =
        {
            new TestCaseData(new TextureDesc(Vector2.zero) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm },
                    new TextureDesc(Vector2.zero) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm },
                    true)
                .SetName("All the Texture parameters are the same."),
            new TestCaseData(new TextureDesc(Vector2.zero) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, msaaSamples = MSAASamples.None },
                    new TextureDesc(Vector2.zero) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, msaaSamples = MSAASamples.MSAA4x },
                    false)
                .SetName("The msaaSamples parameter is different."),
            new TestCaseData(new TextureDesc(256, 256) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm },
                    new TextureDesc(512, 512) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm },
                    false)
                .SetName("The resolution parameter is different."),
            new TestCaseData(new TextureDesc(Vector2.zero) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm },
                    new TextureDesc(Vector2.zero) { colorFormat = GraphicsFormat.R16G16B16_SInt },
                    false)
                .SetName("The colorFormat parameter is different."),
        };


        [Test]
        [TestCaseSource(nameof(s_TextureParametersCases))]
        public void ComputeGraphHash_WithTextureParameters(TextureDesc first, TextureDesc second, bool hashCodeEquality)
        {
            RecordRenderGraph(m_RenderGraph, first);

            var hash0 = m_RenderGraph.ComputeGraphHash();
            ClearCompiledGraphAndHash();

            RecordRenderGraph(m_RenderGraph, second);

            var hash1 = m_RenderGraph.ComputeGraphHash();
            ClearCompiledGraphAndHash();

            Assert.That(hash0 == hash1, Is.EqualTo(hashCodeEquality));

            void RecordRenderGraph(RenderGraph renderGraph, TextureDesc desc)
            {
                var texture0 = renderGraph.CreateTexture(desc);
                using var builder = renderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData);
                builder.UseColorBuffer(texture0, 0);
            }
        }

        //Lambda hashcode depends on the position in the code as they are generated by the compiler.
        //They will be treated as separate methods in this case.
        [Test]
        public void ComputeGraphHash_WhenUsedLambdasDiffer_HashcodeIsDifferent()
        {
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
                builder.SetRenderFunc<RenderGraphTestPassData>((_, _) => { });

            var hash0 = m_RenderGraph.ComputeGraphHash();
            ClearCompiledGraphAndHash();

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
                builder.SetRenderFunc<RenderGraphTestPassData>((_, _) => { });

            var hash1 = m_RenderGraph.ComputeGraphHash();
            ClearCompiledGraphAndHash();

            Assert.AreNotEqual(hash0, hash1);
        }

        [Test]
        public void ComputeGraphHash_WhenUsedStaticMethodsDiffer_HashcodeIsDifferent()
        {
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
                builder.SetRenderFunc<RenderGraphTestPassData>(StaticMethodInsideRegularClass.RenderFunc);

            var hash0 = m_RenderGraph.ComputeGraphHash();
            ClearCompiledGraphAndHash();

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
                builder.SetRenderFunc<RenderGraphTestPassData>(StaticMethodInsideRegularClass.RenderFunc2);

            var hash1 = m_RenderGraph.ComputeGraphHash();
            ClearCompiledGraphAndHash();

            Assert.AreNotEqual(hash0, hash1);
        }

        [Test]
        public void ComputeGraphHashForTheSameSetup_WhenSamePassesUsed_HashcodeIsSame()
        {
            RecordRenderGraph(m_RenderGraph);
            var hash0 = m_RenderGraph.ComputeGraphHash();
            ClearCompiledGraphAndHash();

            RecordRenderGraph(m_RenderGraph);
            var hash1 = m_RenderGraph.ComputeGraphHash();
            ClearCompiledGraphAndHash();

            Assert.AreEqual(hash0, hash1);

            void RecordRenderGraph(RenderGraph renderGraph)
            {
                using (var builder = renderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
                {
                }

                using (var builder = renderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
                {
                }

                using (var builder = renderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
                {
                }
            }
        }

        [Test]
        public void ComputeGraphHashForTheSameSetup_WhenStaticsInStaticClassUsed_HashcodeIsSame()
        {
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
                builder.SetRenderFunc<RenderGraphTestPassData>(StaticMethodInsideStaticClass.RenderFunc);
            var hash0 = m_RenderGraph.ComputeGraphHash();
            ClearCompiledGraphAndHash();

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
                builder.SetRenderFunc<RenderGraphTestPassData>(StaticMethodInsideStaticClass.RenderFunc);
            var hash1 = m_RenderGraph.ComputeGraphHash();
            ClearCompiledGraphAndHash();

            Assert.AreEqual(hash0, hash1);
        }

        [Test]
        public void ComputeGraphHashForTheSameSetup_WhenStaticsInRegularClassUsed_HashcodeIsSame()
        {
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
                builder.SetRenderFunc<RenderGraphTestPassData>(StaticMethodInsideRegularClass.RenderFunc);
            var hash0 = m_RenderGraph.ComputeGraphHash();
            ClearCompiledGraphAndHash();

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
                builder.SetRenderFunc<RenderGraphTestPassData>(StaticMethodInsideRegularClass.RenderFunc);
            var hash1 = m_RenderGraph.ComputeGraphHash();
            ClearCompiledGraphAndHash();

            Assert.AreEqual(hash0, hash1);
        }

        [Test]
        public void ComputeGraphHashForTheSameSetup_WhenLambdasUsed_HashcodeIsSame()
        {
            RecordRenderGraph(m_RenderGraph);

            var hash0 = m_RenderGraph.ComputeGraphHash();
            ClearCompiledGraphAndHash();

            RecordRenderGraph(m_RenderGraph);

            var hash1 = m_RenderGraph.ComputeGraphHash();
            ClearCompiledGraphAndHash();

            Assert.AreEqual(hash0, hash1);

            static void RecordRenderGraph(RenderGraph renderGraph)
            {
                using (var builder = renderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
                    builder.SetRenderFunc<RenderGraphTestPassData>((p, c) => { });

                using (var builder = renderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
                    builder.SetRenderFunc<RenderGraphTestPassData>((p, c) => { });

                using (var builder = renderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
                    builder.SetRenderFunc<RenderGraphTestPassData>((p, c) => { });
            }
        }

        [Test]
        public void ComputeGraphHashForTheSameSetup_WhenLambdasWithCapturedVariablesUsed_HashcodeIsSame()
        {
            TextureHandle texture0 = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });

            RecordRenderGraph(m_RenderGraph, texture0);

            var hash0 = m_RenderGraph.ComputeGraphHash();
            ClearCompiledGraphAndHash();

            RecordRenderGraph(m_RenderGraph, texture0);

            var hash1 = m_RenderGraph.ComputeGraphHash();
            ClearCompiledGraphAndHash();

            Assert.AreEqual(hash0, hash1);

            void RecordRenderGraph(RenderGraph renderGraph, TextureHandle handle)
            {
                using var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData);
                builder.SetRenderFunc<RenderGraphTestPassData>((data, context) =>
                {
                    if (!handle.IsValid())
                        return;
                    Debug.Log(handle.GetHashCode());
                });
            }
        }

        [Test]
        public void ComputeGraphHashForTheSameSetup_WhenLocalMethodsUsed_HashcodeIsSame()
        {
            RecordRenderGraph(m_RenderGraph);

            var hash0 = m_RenderGraph.ComputeGraphHash();
            ClearCompiledGraphAndHash();

            RecordRenderGraph(m_RenderGraph);
            var hash1 = m_RenderGraph.ComputeGraphHash();
            ClearCompiledGraphAndHash();

            Assert.AreEqual(hash0, hash1);

            void RecordRenderGraph(RenderGraph renderGraph)
            {
                using (var builder = renderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
                    builder.SetRenderFunc<RenderGraphTestPassData>(LocalRenderFunc);

                using (var builder = renderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
                    builder.SetRenderFunc<RenderGraphTestPassData>(LocalRenderFunc);

                using (var builder = renderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
                    builder.SetRenderFunc<RenderGraphTestPassData>(LocalRenderFunc);
            }

            void LocalRenderFunc(RenderGraphTestPassData data, RenderGraphContext renderGraphContext)
            {
            }
        }

        [Test]
        public void ComputeGraphHashForTheSameSetup_WhenLocalMethodsWithCapturedVariablesUsed_HashcodeIsSame()
        {
            RecordRenderGraph(m_RenderGraph);

            var hash0 = m_RenderGraph.ComputeGraphHash();
            ClearCompiledGraphAndHash();

            RecordRenderGraph(m_RenderGraph);

            var hash1 = m_RenderGraph.ComputeGraphHash();
            ClearCompiledGraphAndHash();

            Assert.AreEqual(hash0, hash1);

            void RecordRenderGraph(RenderGraph renderGraph)
            {
                var outerScopeVariable = "1";
                using (var builder = renderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
                    builder.SetRenderFunc<RenderGraphTestPassData>(LocalRenderFunc);

                outerScopeVariable = "2";
                using (var builder = renderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
                    builder.SetRenderFunc<RenderGraphTestPassData>(LocalRenderFunc);

                outerScopeVariable = "3";
                using (var builder = renderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
                    builder.SetRenderFunc<RenderGraphTestPassData>(LocalRenderFunc);

                void LocalRenderFunc(RenderGraphTestPassData data, RenderGraphContext renderGraphContext)
                    => Debug.Log(outerScopeVariable);
            }
        }
    }
}
