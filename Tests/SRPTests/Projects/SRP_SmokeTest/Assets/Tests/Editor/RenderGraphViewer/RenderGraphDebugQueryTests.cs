using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEditor.Rendering.Tests
{
    /// <summary>
    /// Tests for RenderGraphDebugQuery using a serialized debug session fixture.
    /// Tests that the query API correctly retrieves data from RenderGraphDebugSession.
    /// Validates against known data from the fixture file.
    /// </summary>
    [TestFixture]
    internal class RenderGraphDebugQueryTests
    {
        const string k_FixturePath = "Assets/Tests/Editor/RenderGraphViewer/RenderGraph/RenderGraphDebugSessionFixture.json";

        // Expected values from the fixture
        const string k_ExpectedGraphName = "URPRenderGraph";
        const string k_ExpectedExecutionName = "Main Camera";
        const int k_ExpectedPassCount = 17;
        const int k_ExpectedCulledPassCount = 0;
        const int k_ExpectedUnmergedPassCount = 13;
        const int k_ExpectedResourceCount = 18; // 16 textures + 1 buffer + 1 acceleration structure
        const int k_ExpectedTextureCount = 16;
        const int k_ExpectedBufferCount = 1;
        const int k_ExpectedAccelerationStructureCount = 1;
        const int k_ExpectedImportedResourceCount = 8;
        const long k_ExpectedTotalMemoryBytes = 68872572L; // ~65.7 MB

        // Known pass names from fixture
        static readonly string[] k_ExpectedPassNames = new[]
        {
            "Initialize Frame",
            "Setup Forward Lights",
            "Setup Debug Properties",
            "Setup Camera Properties",
            "Process VFXCamera Command",
            "Draw Main Light Shadowmap",
            "Setup Camera Properties", // Duplicate name
            "Draw Depth Normal Prepass",
            "Copy DBuffer Depth",
            "Draw DBuffer",
            "Draw Opaque Objects",
            "Draw Decal Forward Emissive",
            "Draw Skybox",
            "Draw Transparent Objects",
            "Blit Final To Back Buffer",
            "Draw UIToolkit/u GUI Overlay",
            "Draw IMGUI/Software Cursor Overlay"
        };

        // Known resource names from fixture
        static readonly string[] k_ExpectedTextureNames = new[]
        {
            "<null>",
            "UnityBlack",
            "UnityWhite",
            "DefaultShadowTexture",
            "Backbuffer color",
            "Backbuffer depth",
            "_CameraTargetAttachment",
            "_CameraDepthAttachment",
            "_CameraDepthTexture",
            "_CameraNormalsTexture",
            "_MotionVectorTexture",
            "_MotionVectorDepthTexture",
            "DBufferDepth",
            "_DBufferTexture0",
            "_DBufferTexture1",
            "_DBufferTexture2"
        };

        [OneTimeSetUp]
        public void SetUp()
        {
            // Load the serialized session fixture
            Assert.IsTrue(System.IO.File.Exists(k_FixturePath), $"Test fixture not found at: {k_FixturePath}");

            var serializedSession = SerializedRenderGraphDebugSession.LoadFromFile(k_FixturePath);
            RenderGraphDebugSession.BeginSession(serializedSession);

            Assert.IsTrue(RenderGraphDebugSession.hasActiveDebugSession, "Session should be active after loading fixture");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            RenderGraphDebugSession.EndSession();
        }

        #region Session

        [Test]
        public void Capture_CreatesValidQuery()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                Assert.IsNotNull(query, "Capture should return a query instance");
                Assert.IsNotNull(query.session, "Query should have session info");
                Assert.IsFalse(query.session.isPlaying, "Must not be playing");
                Assert.IsTrue(query.session.isEditor, "Is editor must be true");
            }
        }

        // Helper to get the actual execution item for tests
        // Returns a hardcoded execution item with known ID and name for consistent testing
        private RenderGraph.DebugExecutionItem GetExecutionItem()
        {
            // Return hardcoded execution item for predictable tests
            return new RenderGraph.DebugExecutionItem(EntityId.FromULong(1099511677449), "Main Camera");
        }

        [Test]
        public void GetGraphs_ReturnsCorrectGraphName()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var result = query.GetGraphs();
                Assert.IsTrue(result.TryGetValue(out var graphs), "GetGraphs should succeed");

                Assert.AreEqual(1, graphs.graphs.Length, "Should have exactly one graph");
                Assert.AreEqual(k_ExpectedGraphName, graphs.graphs[0].name, "Graph name should match fixture");
            }
        }

        [Test]
        public void GetExecutions_ReturnsCorrectExecutionInfo()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var result = query.Graph(k_ExpectedGraphName).GetExecutions();
                Assert.IsTrue(result.TryGetValue(out var executions), "GetExecutions should succeed");

                Assert.AreEqual(1, executions.executions.Length, "Should have exactly one execution");
                Assert.AreEqual(k_ExpectedGraphName, executions.executions[0].graphName, "Graph name should match");
                Assert.AreEqual(k_ExpectedExecutionName, executions.executions[0].executionItem.name, "Execution name should match fixture");
            }
        }

        #endregion

        #region Pass Query Tests

        [Test]
        public void GetPasses_ReturnsCorrectPassCount()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetPasses();
                Assert.IsTrue(result.TryGetValue(out var passes), "GetPasses should succeed");

                Assert.AreEqual(k_ExpectedPassCount, passes.passCount, $"Should have exactly {k_ExpectedPassCount} passes");
                Assert.AreEqual(k_ExpectedPassCount, passes.passes.Length, "Passes array length should match pass count");
                Assert.AreEqual(k_ExpectedCulledPassCount, passes.culledCount, "Culled count should match fixture");
            }
        }

        [Test]
        public void GetPasses_ReturnsCorrectPassNames()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetPasses();
                Assert.IsTrue(result.TryGetValue(out var passes));

                // Verify each pass name matches
                for (int i = 0; i < k_ExpectedPassNames.Length; i++)
                {
                    Assert.AreEqual(k_ExpectedPassNames[i], passes.passes[i].name,
                        $"Pass {i} name should match fixture");
                    Assert.AreEqual(i, passes.passes[i].index,
                        $"Pass {i} index should be correct");
                }
            }
        }

        [Test]
        public void GetPassByName_ReturnsCorrectPass()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();

                // Test getting a specific pass by name
                const string testPassName = "Draw Opaque Objects";
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetPassByName(testPassName);
                Assert.IsTrue(result.TryGetValue(out var passInfo), "GetPassByName should succeed");

                Assert.AreEqual(testPassName, passInfo.pass.name, "Pass name should match");
                Assert.AreEqual(10, passInfo.pass.index, "Draw Opaque Objects should be at index 10");
                Assert.IsFalse(passInfo.pass.culled, "Pass should not be culled");
            }
        }

        [Test]
        public void GetPass_ByIndex_ReturnsCorrectPass()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();

                // Test getting pass by index
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetPass(0);
                Assert.IsTrue(result.TryGetValue(out var passInfo), "GetPass should succeed");

                Assert.AreEqual("Initialize Frame", passInfo.pass.name, "First pass should be Initialize Frame");
                Assert.AreEqual(0, passInfo.pass.index, "Pass index should be 0");
            }
        }

        [Test]
        public void GetCulledPasses_ReturnsCorrectCount()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetCulledPasses();
                Assert.IsTrue(result.TryGetValue(out var culled), "GetCulledPasses should succeed");

                Assert.AreEqual(k_ExpectedCulledPassCount, culled.culledPasses.Length, "Culled pass count should match fixture");
            }
        }

        [Test]
        public void GetUnmergedPasses_ReturnsCorrectCount()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetUnmergedPasses();
                Assert.IsTrue(result.TryGetValue(out var unmerged), "GetUnmergedPasses should succeed");

                Assert.AreEqual(k_ExpectedUnmergedPassCount, unmerged.unmergedPasses.Length, "Unmerged pass count should match fixture");

                // Verify first few unmerged passes have break reasons
                if (unmerged.unmergedPasses.Length > 0)
                {
                    Assert.IsNotEmpty(unmerged.unmergedPasses[0].breakReason, "First unmerged pass should have a break reason");
                    Assert.IsNotEmpty(unmerged.unmergedPasses[0].nextPassName, "First unmerged pass should have next pass name");
                }
            }
        }

        #endregion

        #region Resource Query Tests

        [Test]
        public void GetResources_ReturnsCorrectResourceCount()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetResources();
                Assert.IsTrue(result.TryGetValue(out var resources), "GetResources should succeed");

                Assert.AreEqual(k_ExpectedResourceCount, resources.resourceCount, $"Should have {k_ExpectedResourceCount} total resources");
                Assert.AreEqual(k_ExpectedResourceCount, resources.resources.Length, "Resources array length should match count");
            }
        }

        [Test]
        public void GetResources_FilterByType_Texture()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetResources("Texture");
                Assert.IsTrue(result.TryGetValue(out var resources), "GetResources should succeed");

                Assert.AreEqual(k_ExpectedTextureCount, resources.resourceCount,
                    $"Should have exactly {k_ExpectedTextureCount} texture resources");

                // Verify all are textures
                foreach (var res in resources.resources)
                {
                    Assert.AreEqual("Texture", res.type, "All filtered resources should be textures");
                }
            }
        }

        [Test]
        public void GetResources_VerifyTextureNames()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetResources("Texture");
                Assert.IsTrue(result.TryGetValue(out var resources));

                // Verify texture names match fixture
                for (int i = 0; i < k_ExpectedTextureNames.Length && i < resources.resources.Length; i++)
                {
                    Assert.AreEqual(k_ExpectedTextureNames[i], resources.resources[i].name,
                        $"Texture {i} name should match fixture");
                }
            }
        }

        [Test]
        public void GetResourceByName_ReturnsCorrectResource()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();

                // Test getting a specific resource by name
                const string testResourceName = "_CameraDepthTexture";
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetResourceByName(testResourceName);
                Assert.IsTrue(result.TryGetValue(out var resourceInfo), "GetResourceByName should succeed");

                Assert.AreEqual(testResourceName, resourceInfo.resource.name, "Resource name should match");
                Assert.AreEqual("Texture", resourceInfo.resource.type, "Resource should be a texture");
                Assert.IsFalse(resourceInfo.resource.imported, "_CameraDepthTexture is not imported");

                // Verify it has properties
                Assert.Greater(resourceInfo.resource.width, 0, "Texture should have width");
                Assert.Greater(resourceInfo.resource.height, 0, "Texture should have height");
            }
        }

        [Test]
        public void GetResourceByName_ImportedResource()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();

                // Test an imported resource
                const string testResourceName = "DefaultShadowTexture";
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetResourceByName(testResourceName);
                Assert.IsTrue(result.TryGetValue(out var resourceInfo));

                Assert.AreEqual(testResourceName, resourceInfo.resource.name, "Resource name should match");
                Assert.IsTrue(resourceInfo.resource.imported, "DefaultShadowTexture should be imported");
            }
        }

        [Test]
        public void GetResourceByName_VerifyConsumerPasses()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();

                // _CameraDepthAttachment is used by multiple passes (from fixture analysis)
                const string testResourceName = "_CameraDepthAttachment";
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetResourceByName(testResourceName);
                Assert.IsTrue(result.TryGetValue(out var resourceInfo));

                Assert.IsNotNull(resourceInfo.resource.consumerPasses, "Should have consumer passes");
                Assert.Greater(resourceInfo.resource.consumerPasses.Length, 0,
                    "Resource should be consumed by at least one pass");

                // Verify consumer passes are valid indices
                foreach (var passIndex in resourceInfo.resource.consumerPasses)
                {
                    Assert.GreaterOrEqual(passIndex, 0, "Pass index should be non-negative");
                    Assert.Less(passIndex, k_ExpectedPassCount, "Pass index should be within pass count");
                }
            }
        }

        #endregion

        #region Fluent API Tests

        [Test]
        public void FluentAPI_GraphExecution_GetPasses()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();

                // Test fluent API: Graph().Execution().GetPasses()
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetPasses();
                Assert.IsTrue(result.TryGetValue(out var passes), "Fluent API should succeed");

                Assert.AreEqual(k_ExpectedPassCount, passes.passCount, "Should return correct pass count");
            }
        }

        [Test]
        public void FluentAPI_GraphExecution_GetResources()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();

                // Test fluent API: Graph().Execution().GetResources()
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetResources();
                Assert.IsTrue(result.TryGetValue(out var resources), "Fluent API should succeed");

                Assert.AreEqual(18, resources.resourceCount, "Should return correct resource count");
            }
        }

        [Test]
        public void FluentAPI_GraphExecution_GetPassByName()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();

                const string testPassName = "Draw Skybox";
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetPassByName(testPassName);
                Assert.IsTrue(result.TryGetValue(out var passInfo), "Fluent API should succeed");

                Assert.AreEqual(testPassName, passInfo.pass.name, "Should return correct pass");
            }
        }

        #endregion

        #region Capture Info Tests

        [Test]
        public void ExecutionInfo_IncludesCaptureInfo()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var result = query.Graph(k_ExpectedGraphName).Execution(GetExecutionItem());
                var captureInfo = result.GetCaptureInfo();
                Assert.IsTrue(captureInfo.TryGetValue(out var value));
                Assert.IsNotNull(value.captureTimestamp);
                Assert.IsNotNull(value.captureSourceString);
            }
        }

        [Test]
        public void GetCaptureInfo_ReturnsValidOutput()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetCaptureInfo();
                Assert.IsTrue(result.TryGetValue(out var captureInfo), "GetCaptureInfo should succeed");

                // Verify execution name
                Assert.AreEqual(k_ExpectedExecutionName, captureInfo.executionName, "Execution name should match");

                // Verify capture source info exists
                Assert.IsNotNull(captureInfo.captureTimestamp, "Capture timestamp should be included in ExecutionInfo");
            }
        }

        [Test]
        public void SessionInfo_DoesNotContainCaptureInfo()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var session = query.session;

                // Verify SessionInfo contains only session-level data
                Assert.IsNotNull(session.capturedAt, "Should have capture timestamp");
                Assert.IsTrue(session.isPaused, "Session should be paused during query");

                // Verify it's session-level, not execution-level
                // (no captureSourceInfo field should exist)
                var sessionType = session.GetType();
                var captureInfoField = sessionType.GetField("captureSourceInfo");
                Assert.IsNull(captureInfoField, "SessionInfo should not have captureSourceInfo field - use GetCaptureInfo() on ExecutionScope instead");
            }
        }

        #endregion

        #region Graph Metrics Tests

        [Test]
        public void GetGraphMetrics_ReturnsValidOutput()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetGraphMetrics();
                Assert.IsTrue(result.TryGetValue(out var insights), "GetGraphMetrics should succeed");

                // Verify execution name
                Assert.AreEqual(k_ExpectedExecutionName, insights.executionName, "Execution name should match");

                // Verify pass counts
                Assert.AreEqual(k_ExpectedPassCount, insights.totalPasses, "Total passes should match fixture");
                Assert.AreEqual(k_ExpectedCulledPassCount, insights.culledPasses, "Culled passes should match fixture");
                Assert.AreEqual(k_ExpectedUnmergedPassCount, insights.unmergedPasses, "Unmerged passes should match fixture");

                // Verify resource counts
                Assert.AreEqual(k_ExpectedResourceCount, insights.totalResources, "Total resources should match fixture");
                Assert.AreEqual(k_ExpectedTextureCount, insights.textureCount, "Texture count should match fixture");
                Assert.AreEqual(k_ExpectedBufferCount, insights.bufferCount, "Buffer count should match fixture");

                // Verify insights array
                Assert.IsNotNull(insights.insights, "Insights array should not be null");
                Assert.AreEqual(8, insights.insights.Length, "Should have exactly 8 insights");

                // Verify each insight in order
                // [0] Culling insight
                Assert.AreEqual(InsightCategory.Culling, insights.insights[0].category);
                Assert.AreEqual(InsightSeverity.Info, insights.insights[0].severity);
                Assert.AreEqual("Culling: 0/17 passes culled (0.0%), 17 active", insights.insights[0].message);

                // [1] Pass distribution insight
                Assert.AreEqual(InsightCategory.PassDistribution, insights.insights[1].category);
                Assert.AreEqual(InsightSeverity.Info, insights.insights[1].severity);
                Assert.AreEqual("Active passes: 17 total (13 raster, 0 compute, 4 unsupported)", insights.insights[1].message);

                // [2] Async compute insight
                Assert.AreEqual(InsightCategory.AsyncCompute, insights.insights[2].category);
                Assert.AreEqual(InsightSeverity.Info, insights.insights[2].severity);
                Assert.AreEqual("Async compute: 0 async passes, 0 sync points", insights.insights[2].message);

                // [3] Native pass merging insight
                Assert.AreEqual(InsightCategory.Merging, insights.insights[3].category);
                Assert.AreEqual(InsightSeverity.Info, insights.insights[3].severity);
                Assert.AreEqual("Native pass merging: 13 merge breaks out of 17 active passes (23.5% efficiency)", insights.insights[3].message);

                // [4] Most common merge break reason
                Assert.AreEqual(InsightCategory.Merging, insights.insights[4].category);
                Assert.AreEqual(InsightSeverity.Info, insights.insights[4].severity);
                StringAssert.Contains("Most common merge break reason:", insights.insights[4].message);
                StringAssert.Contains("Failed to merge BlitFinalToBackBuffer", insights.insights[4].message);
                StringAssert.Contains("4 occurrences", insights.insights[4].message);

                // [5] Resources summary
                Assert.AreEqual(InsightCategory.Resources, insights.insights[5].category);
                Assert.AreEqual(InsightSeverity.Info, insights.insights[5].severity);
                Assert.AreEqual("Resources: 16 textures, 1 buffers, 8 imported, 0 memoryless", insights.insights[5].message);

                // [6] Texture memory
                Assert.AreEqual(InsightCategory.Resources, insights.insights[6].category);
                Assert.AreEqual(InsightSeverity.Info, insights.insights[6].severity);
                Assert.AreEqual("Estimated texture memory (base mip only): 65.7 MB", insights.insights[6].message);

                // [7] Short-lived resources
                Assert.AreEqual(InsightCategory.Resources, insights.insights[7].category);
                Assert.AreEqual(InsightSeverity.Info, insights.insights[7].severity);
                Assert.AreEqual("Short-lived resources (lifetime ≤ 1 pass): 1", insights.insights[7].message);
            }
        }

        [Test]
        public void GetGraphMetrics_ContainsCullingInsight()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetGraphMetrics();
                Assert.IsTrue(result.TryGetValue(out var insights));

                // Should have at least one culling-related insight
                bool hasCullingInsight = false;
                foreach (var insight in insights.insights)
                {
                    if (insight.category == InsightCategory.Culling)
                    {
                        hasCullingInsight = true;
                        Assert.IsNotEmpty(insight.message, "Culling insight should have a message");
                        break;
                    }
                }
                Assert.IsTrue(hasCullingInsight, "Should have at least one culling insight");
            }
        }

        [Test]
        public void GetGraphMetrics_ContainsResourceInsight()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetGraphMetrics();
                Assert.IsTrue(result.TryGetValue(out var insights));

                // Should have at least one resource-related insight
                bool hasResourceInsight = false;
                foreach (var insight in insights.insights)
                {
                    if (insight.category == InsightCategory.Resources)
                    {
                        hasResourceInsight = true;
                        Assert.IsNotEmpty(insight.message, "Resource insight should have a message");
                        break;
                    }
                }
                Assert.IsTrue(hasResourceInsight, "Should have at least one resource insight");
            }
        }

        [Test]
        public void GetGraphMetrics_ContainsPassDistributionInsight()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetGraphMetrics();
                Assert.IsTrue(result.TryGetValue(out var insights));

                // Should have at least one pass distribution insight
                bool hasPassDistributionInsight = false;
                foreach (var insight in insights.insights)
                {
                    if (insight.category == InsightCategory.PassDistribution)
                    {
                        hasPassDistributionInsight = true;
                        Assert.IsNotEmpty(insight.message, "Pass distribution insight should have a message");
                        // Should mention pass types (raster/compute)
                        Assert.That(insight.message.ToLower(), Does.Match("raster|compute|pass"));
                        break;
                    }
                }
                Assert.IsTrue(hasPassDistributionInsight, "Should have at least one pass distribution insight");
            }
        }

        [Test]
        public void GetGraphMetrics_AllInsightsHaveValidData()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetGraphMetrics();
                Assert.IsTrue(result.TryGetValue(out var insights));

                // Verify each insight has valid data
                foreach (var insight in insights.insights)
                {
                    // Message should not be empty
                    Assert.IsNotEmpty(insight.message, "Every insight should have a message");
                }
            }
        }

        [Test]
        public void GetGraphMetrics_NoCulledPassesGeneratesCorrectInsight()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetGraphMetrics();
                Assert.IsTrue(result.TryGetValue(out var insights));

                // With 0 culled passes out of 17, we expect a low/zero cull rate insight
                bool foundCullRateInsight = false;
                foreach (var insight in insights.insights)
                {
                    if (insight.category == InsightCategory.Culling && insight.message.Contains("0/"))
                    {
                        foundCullRateInsight = true;
                        // Should mention that 0 passes are culled
                        Assert.That(insight.message, Does.Contain("0").Or.Contains("culled"));
                        break;
                    }
                }
                Assert.IsTrue(foundCullRateInsight, "Should have insight about 0% cull rate");
            }
        }

        #endregion

        #region Texture Memory Calculation Tests

        [Test]
        public void ComputeTextureMemory_ValidatesFixtureTextures()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetResources("Texture");
                Assert.IsTrue(result.TryGetValue(out var resources), "GetResources should succeed");

                long totalMemory = 0;
                int texturesWithSize = 0;

                // Iterate through all textures and calculate memory
                foreach (var resourceSummary in resources.resources)
                {
                    var resourceResult = query.Graph(k_ExpectedGraphName).Execution(executionItem)
                        .GetResource(resourceSummary.type, resourceSummary.index);

                    if (resourceResult.TryGetValue(out var resourceInfo))
                    {
                        var res = resourceInfo.resource;

                        // Skip resources with no dimensions (e.g., <null>, imported resources without size info)
                        if (res.width <= 0 || res.height <= 0)
                            continue;

                        // Use reflection to call the private ComputeTextureMemory method
                        var queryType = typeof(RenderGraphDebugSession).GetNestedType("ExecutionQuery",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

                        if (queryType != null)
                        {
                            var computeMethod = queryType.GetMethod("ComputeTextureMemory",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                            if (computeMethod != null)
                            {
                                var memory = (long)computeMethod.Invoke(null, new object[]
                                {
                                    res.width,
                                    res.height,
                                    res.depth,
                                    res.samples,
                                    res.format
                                });

                                totalMemory += memory;
                                texturesWithSize++;
                            }
                        }
                    }
                }

                // Expected values from fixture (validated against GraphicsFormatUtility.ComputeMipmapSize)
                // These values verify that ComputeTextureMemory correctly handles:
                // - Different graphics formats (R8G8B8A8, D32_SFloat, etc.)
                // - Format-specific byte sizes (4 bytes for RGBA8, 4 bytes for D32_SFloat, 8 bytes for D32_SFloat_S8_UInt, etc.)
                // - Non-MSAA textures (samples = 1)
                const long k_ExpectedTotalTextureMemoryBytes = 68872572L;
                const int k_ExpectedTexturesWithSize = 15;

                Assert.AreEqual(k_ExpectedTotalTextureMemoryBytes, totalMemory,
                    $"Total texture memory should match expected value. Expected {k_ExpectedTotalTextureMemoryBytes} bytes, got {totalMemory} bytes. " +
                    "This validates that ComputeTextureMemory correctly uses GraphicsFormatUtility.ComputeMipmapSize for accurate per-format calculations.");

                Assert.AreEqual(k_ExpectedTexturesWithSize, texturesWithSize,
                    $"Number of textures with size should match expected value. Expected {k_ExpectedTexturesWithSize}, got {texturesWithSize}.");
            }
        }

        #endregion

        #region Convenience Methods Tests

        [Test]
        public void GetPassesByType_ReturnsRasterPasses()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetPassesByType("Raster");
                Assert.IsTrue(result.TryGetValue(out var passes), "GetPassesByType should succeed");

                // From fixture: 13 raster passes out of 17 total
                Assert.AreEqual(13, passes.passCount, "Should have 13 raster passes");
                Assert.AreEqual(13, passes.passes.Length, "Passes array length should match count");
                Assert.AreEqual(k_ExpectedExecutionName, passes.executionName, "Execution name should match");

                // Verify all returned passes are raster type
                foreach (var pass in passes.passes)
                {
                    Assert.AreEqual("Raster", pass.type, "All filtered passes should be Raster type");
                }
            }
        }

        [Test]
        public void GetPassesByType_ReturnsComputePasses()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetPassesByType("Compute");
                Assert.IsTrue(result.TryGetValue(out var passes), "GetPassesByType should succeed");

                // From fixture: 0 compute passes
                Assert.AreEqual(0, passes.passCount, "Should have 0 compute passes");
                Assert.AreEqual(0, passes.passes.Length, "Passes array should be empty");
            }
        }

        [Test]
        public void GetPassesByType_IsCaseInsensitive()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();
                var resultLower = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetPassesByType("raster");
                var resultUpper = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetPassesByType("RASTER");

                Assert.IsTrue(resultLower.TryGetValue(out var passesLower));
                Assert.IsTrue(resultUpper.TryGetValue(out var passesUpper));

                Assert.AreEqual(passesLower.passCount, passesUpper.passCount, "Case should not matter");
            }
        }

        [Test]
        public void GetPassesUsingResource_FindsResourceConsumers()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();

                // _CameraDepthAttachment is used by multiple passes
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem)
                    .GetPassesUsingResource("_CameraDepthAttachment");
                Assert.IsTrue(result.TryGetValue(out var output), "GetPassesUsingResource should succeed");

                Assert.AreEqual("_CameraDepthAttachment", output.resourceName, "Resource name should match");
                Assert.Greater(output.passCount, 0, "Should have at least one pass using this resource");
                Assert.AreEqual(output.passCount, output.passes.Length, "Pass count should match array length");

                // Verify all passes actually use the resource
                foreach (var pass in output.passes)
                {
                    bool usesResource = false;

                    if (pass.resourceReads != null)
                    {
                        foreach (var read in pass.resourceReads)
                        {
                            if (string.Equals(read, "_CameraDepthAttachment", StringComparison.OrdinalIgnoreCase))
                            {
                                usesResource = true;
                                break;
                            }
                        }
                    }

                    if (!usesResource && pass.resourceWrites != null)
                    {
                        foreach (var write in pass.resourceWrites)
                        {
                            if (string.Equals(write, "_CameraDepthAttachment", StringComparison.OrdinalIgnoreCase))
                            {
                                usesResource = true;
                                break;
                            }
                        }
                    }

                    Assert.IsTrue(usesResource, $"Pass '{pass.name}' should read or write _CameraDepthAttachment");
                }
            }
        }

        [Test]
        public void GetPassesUsingResource_ReturnsEmptyForUnusedResource()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();

                // UnityBlack is imported but likely not used by passes
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem)
                    .GetPassesUsingResource("UnityBlack");
                Assert.IsTrue(result.TryGetValue(out var output));

                // Should succeed but return empty list
                Assert.AreEqual("UnityBlack", output.resourceName);
                Assert.AreEqual(0, output.passCount, "UnityBlack should not be used by any passes");
            }
        }

        [Test]
        public void GetPassesUsingResource_IsCaseInsensitive()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();

                var resultLower = query.Graph(k_ExpectedGraphName).Execution(executionItem)
                    .GetPassesUsingResource("_cameradepthattachment");
                var resultMixed = query.Graph(k_ExpectedGraphName).Execution(executionItem)
                    .GetPassesUsingResource("_CameraDepthAttachment");

                Assert.IsTrue(resultLower.TryGetValue(out var outputLower));
                Assert.IsTrue(resultMixed.TryGetValue(out var outputMixed));

                Assert.AreEqual(outputLower.passCount, outputMixed.passCount, "Case should not matter");
            }
        }

        [Test]
        public void GetResourceProducerPass_ReturnsCreatorPass()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();

                // _CameraDepthTexture is created by a pass (not imported)
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem)
                    .GetResourceProducerPass("_CameraDepthTexture");
                Assert.IsTrue(result.TryGetValue(out var passInfo), "GetResourceProducerPass should succeed");

                Assert.IsNotNull(passInfo.pass, "Should return a pass");
                Assert.IsNotEmpty(passInfo.pass.name, "Pass should have a name");

                // Verify the pass writes to this resource
                Assert.IsNotNull(passInfo.pass.resourceWrites, "Pass should have resource writes");
                CollectionAssert.Contains(passInfo.pass.resourceWrites, "_CameraDepthTexture",
                    "Pass should write to _CameraDepthTexture");
            }
        }

        [Test]
        public void GetResourceProducerPass_FailsForImportedResource()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();

                // DefaultShadowTexture is imported (not created by a pass)
                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem)
                    .GetResourceProducerPass("DefaultShadowTexture");
                Assert.IsFalse(result.TryGetValue(out _), "Should fail for imported resource");

                Assert.IsNotNull(result.error, "Should have error message");
                StringAssert.Contains("no creation pass", result.error.ToLower(),
                    "Error should mention resource has no creation pass");
                StringAssert.Contains("imported", result.error.ToLower(),
                    "Error should mention resource is imported");
            }
        }

        [Test]
        public void GetResourceProducerPass_FailsForNonExistentResource()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();

                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem)
                    .GetResourceProducerPass("NonExistentResource");
                Assert.IsFalse(result.TryGetValue(out _), "Should fail for non-existent resource");

                Assert.IsNotNull(result.error, "Should have error message");
            }
        }

        #endregion

        #region Session State Tests

        [Test]
        public void Query_ReturnsErrorWhenDisposed()
        {
            var query = RenderGraphDebugSession.CreateQueryScope();
            query.Dispose();

            var result = query.GetGraphs();
            Assert.IsFalse(result.TryGetValue(out _), "Query should fail when disposed");
            Assert.IsNotNull(result.error, "Should have error object");
            Assert.That(result.error, Does.Contain("disposed").IgnoreCase, "Error should mention query was disposed");
        }

        [Test]
        public void Query_ReturnsErrorForInvalidPassIndex()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();

                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetPass(999);
                Assert.IsFalse(result.TryGetValue(out _), "Should fail for invalid pass index");
                Assert.IsNotNull(result.error);
                Assert.That(result.error, Does.Contain("out of range").IgnoreCase);
            }
        }

        [Test]
        public void Query_ReturnsErrorForInvalidPassName()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();

                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetPassByName("NonExistentPass");
                Assert.IsFalse(result.TryGetValue(out _), "Should fail for invalid pass name");
                Assert.IsNotNull(result.error);
                Assert.That(result.error, Does.Contain("not found").IgnoreCase);
            }
        }

        [Test]
        public void Query_ReturnsErrorForInvalidResourceType()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();

                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetResources("InvalidType");
                Assert.IsFalse(result.TryGetValue(out _), "Should fail for invalid resource type");
                Assert.IsNotNull(result.error);
                Assert.That(result.error, Does.Contain("Invalid resource type").IgnoreCase);
            }
        }

        [Test]
        public void Query_ReturnsErrorForInvalidResourceName()
        {
            using (var query = RenderGraphDebugSession.CreateQueryScope())
            {
                var executionItem = GetExecutionItem();

                var result = query.Graph(k_ExpectedGraphName).Execution(executionItem).GetResourceByName("NonExistentResource");
                Assert.IsFalse(result.TryGetValue(out _), "Should fail for invalid resource name");
                Assert.IsNotNull(result.error);
                Assert.That(result.error, Does.Contain("not found").IgnoreCase);
            }
        }

        #endregion
    }
}
