using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace UnityEditor.Rendering.Universal.ProjectAuditor.Tests
{
    [NUnit.Framework.Category("Graphics Tools")]
    [TestFixture]
    class URPSettingsAnalyzerTests
    {
        private UniversalRenderPipelineAsset m_PreviousDefaultRenderPipeline;
        private RenderPipelineAsset[] m_PreviousQualitySettings;
        private int m_PreviousQualityLevel;
        private ColorSpace m_PreviousColorSpace;
        private bool m_PreviousStaticBatching;

        [SetUp]
        public void SetUp()
        {
            // Save previous settings
            m_PreviousDefaultRenderPipeline = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            m_PreviousColorSpace = PlayerSettings.colorSpace;
            m_PreviousStaticBatching = PlayerSettings.GetStaticBatchingForPlatform(EditorUserBuildSettings.activeBuildTarget);

            // Save quality settings
            m_PreviousQualitySettings = new RenderPipelineAsset[QualitySettings.names.Length];
            m_PreviousQualityLevel = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                m_PreviousQualitySettings[i] = QualitySettings.renderPipeline;
            }
            QualitySettings.SetQualityLevel(m_PreviousQualityLevel);
        }

        [TearDown]
        public void TearDown()
        {
            // Restore previous settings
            GraphicsSettings.defaultRenderPipeline = m_PreviousDefaultRenderPipeline;
            PlayerSettings.colorSpace = m_PreviousColorSpace;
            PlayerSettings.SetStaticBatchingForPlatform(EditorUserBuildSettings.activeBuildTarget, m_PreviousStaticBatching);

            // Restore quality settings
            for (int i = 0; i < QualitySettings.names.Length && i < m_PreviousQualitySettings.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                QualitySettings.renderPipeline = m_PreviousQualitySettings[i];
            }
            QualitySettings.SetQualityLevel(m_PreviousQualityLevel);
        }

        #region URP Assets Tests (0001-0100)

        [Test]
        public void URP0001_MissingURPAsset_DetectsIssue()
        {
            // Arrange
            GraphicsSettings.defaultRenderPipeline = null;

            for (int i = 0; i < QualitySettings.names.Length && i < m_PreviousQualitySettings.Length; i++)
            {                
                QualitySettings.SetQualityLevel(i);
                QualitySettings.renderPipeline = null;
            }

            var analyzer = new MissingAssignedRenderPipeline();

            // Act
            var issues = analyzer.EnumerateIssues().ToList();

            // Assert
            Assert.AreEqual(1 + QualitySettings.count, issues.Count, "Should detect missing URP asset from default");
            Assert.AreEqual("URP0001", analyzer.Descriptor.Id);
        }

        [Test]
        public void URP0001_URPAssetPresent_NoIssue()
        {
            // Arrange
            var urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            GraphicsSettings.defaultRenderPipeline = urpAsset;

            for (int i = 0; i < QualitySettings.names.Length && i < m_PreviousQualitySettings.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                QualitySettings.renderPipeline = urpAsset;
            }

            var analyzer = new MissingAssignedRenderPipeline();

            // Act
            var issues = analyzer.EnumerateIssues().ToList();

            // Assert
            Assert.AreEqual(0, issues.Count, "Should not detect issue when URP asset is assigned");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(urpAsset);
        }

        [Test]
        public void URP0002_NoRendererAssigned_DetectsIssue()
        {
            // Arrange
            var urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();

            // Set renderer list to null
            urpAsset.m_RendererDataList = null;
            GraphicsSettings.defaultRenderPipeline = urpAsset;
            var analyzer = new NoRendererAssignedAnalyzer();

            // Act
            var issues = analyzer.EnumerateIssues().ToList();

            // Assert
            Assert.AreEqual(1, issues.Count, "Should detect missing renderer");
            Assert.AreEqual("URP0002", analyzer.Descriptor.Id);

            // Cleanup
            UnityEngine.Object.DestroyImmediate(urpAsset);
        }

        [Test]
        public void URP0003_URPAssetNotAtLastVersion_DetectsIssue()
        {
            // Arrange
            var urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            urpAsset.k_AssetVersion--;
            GraphicsSettings.defaultRenderPipeline = urpAsset;

            var analyzer = new URPAssetLastVersionValidation();

            // Act
            var issues = analyzer.EnumerateIssues().ToList();

            // Assert
            Assert.AreEqual(1, issues.Count, "Should detect not at last version asset");
            Assert.IsNotNull(analyzer.Descriptor);
            Assert.AreEqual("URP0003", analyzer.Descriptor.Id);

            // Cleanup
            UnityEngine.Object.DestroyImmediate(urpAsset);
        }

        [Test]
        public void URP0004_HDREnabled_DetectsIssue()
        {
            // Arrange
            var urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            urpAsset.supportsHDR = true;
            GraphicsSettings.defaultRenderPipeline = urpAsset;

            for (int i = 0; i < QualitySettings.names.Length && i < m_PreviousQualitySettings.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                QualitySettings.renderPipeline = urpAsset;
            }

            var analyzer = new HDREnabledValidation();

            // Act
            var issues = analyzer.EnumerateIssues().ToList();

            // Assert
            Assert.AreEqual(1 + QualitySettings.count, issues.Count, "Should detect not at last version asset");

            Assert.IsNotNull(analyzer.Descriptor);
            Assert.AreEqual("URP0004", analyzer.Descriptor.Id);
            Assert.IsTrue(analyzer.Descriptor.Title.Contains("HDR"));

            // Cleanup
            UnityEngine.Object.DestroyImmediate(urpAsset);
        }

        [Test]
        public void URP0004_HDRDisabled_NoIssue()
        {
            // Arrange
            var urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            urpAsset.supportsHDR = false;
            GraphicsSettings.defaultRenderPipeline = urpAsset;

            for (int i = 0; i < QualitySettings.names.Length && i < m_PreviousQualitySettings.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                QualitySettings.renderPipeline = urpAsset;
            }

            var analyzer = new HDREnabledValidation();

            // Act
            var issues = analyzer.EnumerateIssues().ToList();

            // Assert
            Assert.AreEqual(0, issues.Count, "Should not detect not at last version asset");

            Assert.IsNotNull(analyzer.Descriptor);
            Assert.AreEqual("URP0004", analyzer.Descriptor.Id);
            Assert.IsTrue(analyzer.Descriptor.Title.Contains("HDR"));

            // Cleanup
            UnityEngine.Object.DestroyImmediate(urpAsset);
        }

        [Test]
        public void URP0005_MSAA4x_DetectsIssue()
        {
            // Arrange
            var urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();

            // Set MSAA to 4x
            urpAsset.msaaSampleCount = 4;
            urpAsset.m_RendererDataList = new ScriptableRendererData[] { rendererData };
            urpAsset.m_DefaultRendererIndex = 0;

            GraphicsSettings.defaultRenderPipeline = urpAsset;

            for (int i = 0; i < QualitySettings.names.Length && i < m_PreviousQualitySettings.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                QualitySettings.renderPipeline = urpAsset;
            }

            var analyzer = new MSAAValidation();

            // Act
            var issues = analyzer.EnumerateIssues().ToList();

            // Assert
            Assert.AreEqual(1 + QualitySettings.count, issues.Count, "Should detect MSAA 4x");
            Assert.AreEqual("URP0005", analyzer.Descriptor.Id);

            // Cleanup
            UnityEngine.Object.DestroyImmediate(rendererData);
            UnityEngine.Object.DestroyImmediate(urpAsset);
        }

        [Test]
        public void URP0005_MSAA8x_DetectsIssue()
        {
            // Arrange
            var urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            for (int i = 0; i < QualitySettings.names.Length && i < m_PreviousQualitySettings.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                QualitySettings.renderPipeline = urpAsset;
            }

            // Set MSAA to 8x
            urpAsset.msaaSampleCount = 8;
            urpAsset.m_RendererDataList = new ScriptableRendererData[] { rendererData };
            urpAsset.m_DefaultRendererIndex = 0;

            GraphicsSettings.defaultRenderPipeline = urpAsset;
            var analyzer = new MSAAValidation();

            // Act
            var issues = analyzer.EnumerateIssues().ToList();

            // Assert
            Assert.AreEqual(1 + QualitySettings.count, issues.Count, "Should detect MSAA 8x");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(rendererData);
            UnityEngine.Object.DestroyImmediate(urpAsset);
        }

        [Test]
        public void URP0005_MSAA2x_NoIssue()
        {
            // Arrange
            var urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            for (int i = 0; i < QualitySettings.names.Length && i < m_PreviousQualitySettings.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                QualitySettings.renderPipeline = urpAsset;
            }

            // Set MSAA to 2x
            urpAsset.msaaSampleCount = 2;
            urpAsset.m_RendererDataList = new ScriptableRendererData[] { rendererData };
            urpAsset.m_DefaultRendererIndex = 0;

            GraphicsSettings.defaultRenderPipeline = urpAsset;
            var analyzer = new MSAAValidation();

            // Act
            var issues = analyzer.EnumerateIssues().ToList();

            // Assert
            Assert.AreEqual(0, issues.Count, "Should not detect issue when MSAA is 2x or lower");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(rendererData);
            UnityEngine.Object.DestroyImmediate(urpAsset);
        }

        #endregion

        #region Renderer Tests (0101-0200)

        [Test]
        public void URP0101_InvalidRendererIndex_DetectsOutOfBounds()
        {
            // Arrange
            var urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            for (int i = 0; i < QualitySettings.names.Length && i < m_PreviousQualitySettings.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                QualitySettings.renderPipeline = urpAsset;
            }

            // Set invalid renderer index (out of bounds)
            urpAsset.m_RendererDataList = new ScriptableRendererData[] { rendererData };
            urpAsset.m_DefaultRendererIndex = 99; // Out of bounds

            GraphicsSettings.defaultRenderPipeline = urpAsset;
            var analyzer = new InvalidRendererIndexAnalyzer();

            // Act
            var issues = analyzer.EnumerateIssues().ToList();

            // Assert
            Assert.AreEqual(1 + QualitySettings.count, issues.Count, "Should detect invalid renderer index");
            Assert.AreEqual("URP0101", analyzer.Descriptor.Id);

            // Cleanup
            UnityEngine.Object.DestroyImmediate(rendererData);
            UnityEngine.Object.DestroyImmediate(urpAsset);
        }

        [Test]
        public void URP0101_InvalidRendererIndex_DetectsNullRenderer()
        {
            // Arrange
            var urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            for (int i = 0; i < QualitySettings.names.Length && i < m_PreviousQualitySettings.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                QualitySettings.renderPipeline = urpAsset;
            }

            // Set renderer at index to null
            urpAsset.m_RendererDataList = new ScriptableRendererData[] { null };
            urpAsset.m_DefaultRendererIndex = 0;

            GraphicsSettings.defaultRenderPipeline = urpAsset;
            var analyzer = new InvalidRendererIndexAnalyzer();

            // Act
            var issues = analyzer.EnumerateIssues().ToList();

            // Assert
            Assert.AreEqual(1 + QualitySettings.count, issues.Count, "Should detect null renderer at valid index");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(urpAsset);
        }

        [Test]
        public void URP0101_ValidRendererIndex_NoIssue()
        {
            // Arrange
            var urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            for (int i = 0; i < QualitySettings.names.Length && i < m_PreviousQualitySettings.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                QualitySettings.renderPipeline = urpAsset;
            }

            // Set valid renderer index
            urpAsset.m_RendererDataList = new ScriptableRendererData[] { rendererData };
            urpAsset.m_DefaultRendererIndex = 0;

            GraphicsSettings.defaultRenderPipeline = urpAsset;
            var analyzer = new InvalidRendererIndexAnalyzer();

            // Act
            var issues = analyzer.EnumerateIssues().ToList();

            // Assert
            Assert.AreEqual(0, issues.Count, "Should not detect issue with valid renderer index");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(rendererData);
            UnityEngine.Object.DestroyImmediate(urpAsset);
        }

        [Test]
        public void URP0102_MissingRendererFeatures_HasCorrectDescriptor()
        {
            var analyzer = new MissingRendererFeaturesAnalyzer();
            Assert.IsNotNull(analyzer.Descriptor);
            Assert.AreEqual("URP0102", analyzer.Descriptor.Id);
        }

        [Test]
        public void URP0103_DuplicateRendererFeatures_HasCorrectDescriptor()
        {
            var analyzer = new DuplicateRendererFeaturesAnalyzer();
            Assert.IsNotNull(analyzer.Descriptor);
            Assert.AreEqual("URP0103", analyzer.Descriptor.Id);
        }

        [Test]
        public void URP0104_InactiveRendererFeatures_HasCorrectDescriptor()
        {
            var analyzer = new InactiveRendererFeaturesAnalyzer();
            Assert.IsNotNull(analyzer.Descriptor);
            Assert.AreEqual("URP0104", analyzer.Descriptor.Id);
        }

        #endregion

        #region Global Settings Tests (0201-0300)

        [Test]
        public void URP0201_GlobalSettingsAsset_HasCorrectDescriptor()
        {
            var analyzer = new GlobalSettingsAssetAnalyzer();
            Assert.IsNotNull(analyzer.Descriptor);
            Assert.AreEqual("URP0201", analyzer.Descriptor.Id);
            Assert.IsTrue(analyzer.Descriptor.Title.Contains("Global Settings"));
        }

        [Test]
        public void URP0202_DefaultVolumeProfile_HasCorrectDescriptor()
        {
            var analyzer = new DefaultVolumeProfileAnalyzer();
            Assert.IsNotNull(analyzer.Descriptor);
            Assert.AreEqual("URP0202", analyzer.Descriptor.Id);
            Assert.IsTrue(analyzer.Descriptor.Title.Contains("Volume Profile"));
        }

        #endregion

        #region SRP and Static Batching Tests (0301-0400)

        [Test]
        public void URP0301_SRPBatcherDisabled_HasCorrectDescriptor()
        {
            var analyzer = new SRPBatcherAnalyzer();
            Assert.IsNotNull(analyzer.Descriptor);
            Assert.AreEqual("URP0301", analyzer.Descriptor.Id);
            Assert.IsTrue(analyzer.Descriptor.Title.Contains("SRP Batcher"));
        }

        [Test]
        public void URP0301_SRPBatcherDisabled_DetectsIssue()
        {
            // Arrange
            var urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            for (int i = 0; i < QualitySettings.names.Length && i < m_PreviousQualitySettings.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                QualitySettings.renderPipeline = urpAsset;
            }

            // Disable SRP Batcher
            urpAsset.useSRPBatcher = false;

            // Assign renderer to URP asset
            urpAsset.m_RendererDataList = new ScriptableRendererData[] { rendererData };
            urpAsset.m_DefaultRendererIndex = 0;

            GraphicsSettings.defaultRenderPipeline = urpAsset;
            var analyzer = new SRPBatcherAnalyzer();

            // Act
            var issues = analyzer.EnumerateIssues().ToList();

            // Assert
            Assert.AreEqual(1 + QualitySettings.count, issues.Count, "Should detect disabled SRP Batcher");
            Assert.AreEqual("URP0301", analyzer.Descriptor.Id);

            // Cleanup
            UnityEngine.Object.DestroyImmediate(rendererData);
            UnityEngine.Object.DestroyImmediate(urpAsset);
        }

        [Test]
        public void URP0301_SRPBatcherEnabled_NoIssue()
        {
            // Arrange
            var urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            for (int i = 0; i < QualitySettings.names.Length && i < m_PreviousQualitySettings.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                QualitySettings.renderPipeline = urpAsset;
            }

            // Enable SRP Batcher
            urpAsset.useSRPBatcher = true;

            // Assign renderer to URP asset
            urpAsset.m_RendererDataList = new ScriptableRendererData[] { rendererData };
            urpAsset.m_DefaultRendererIndex = 0;

            GraphicsSettings.defaultRenderPipeline = urpAsset;
            var analyzer = new SRPBatcherAnalyzer();

            // Act
            var issues = analyzer.EnumerateIssues().ToList();

            // Assert
            Assert.AreEqual(0, issues.Count, "Should not detect issue when SRP Batcher is enabled");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(rendererData);
            UnityEngine.Object.DestroyImmediate(urpAsset);
        }

        [Test]
        public void URP0302_StaticBatchingConflict_HasCorrectDescriptor()
        {
            var analyzer = new StaticBatchingWithSRPBatcherAnalyzer();
            Assert.IsNotNull(analyzer.Descriptor);
            Assert.AreEqual("URP0302", analyzer.Descriptor.Id);
            Assert.IsTrue(analyzer.Descriptor.Title.Contains("Static Batching"));
        }

        [Test]
        public void URP0302_StaticBatchingDisabled_NoIssue()
        {
            // Arrange
            var urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            for (int i = 0; i < QualitySettings.names.Length && i < m_PreviousQualitySettings.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                QualitySettings.renderPipeline = urpAsset;
            }

            urpAsset.useSRPBatcher = true;
            urpAsset.m_RendererDataList = new ScriptableRendererData[] { rendererData };
            urpAsset.m_DefaultRendererIndex = 0;

            GraphicsSettings.defaultRenderPipeline = urpAsset;
            PlayerSettings.SetStaticBatchingForPlatform(EditorUserBuildSettings.activeBuildTarget, false);

            var analyzer = new StaticBatchingWithSRPBatcherAnalyzer();

            // Act
            var issues = analyzer.EnumerateIssues().ToList();

            // Assert
            Assert.AreEqual(0, issues.Count, "Should not detect issue when static batching is disabled");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(rendererData);
            UnityEngine.Object.DestroyImmediate(urpAsset);
        }

        [Test]
        public void URP0302_StaticBatchingEnabledWithSRPBatcher_DetectsIssue()
        {
            // Arrange
            var urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            for (int i = 0; i < QualitySettings.names.Length && i < m_PreviousQualitySettings.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                QualitySettings.renderPipeline = urpAsset;
            }

            // Enable both SRP Batcher and Static Batching
            urpAsset.useSRPBatcher = true;
            urpAsset.m_RendererDataList = new ScriptableRendererData[] { rendererData };
            urpAsset.m_DefaultRendererIndex = 0;

            GraphicsSettings.defaultRenderPipeline = urpAsset;
            PlayerSettings.SetStaticBatchingForPlatform(EditorUserBuildSettings.activeBuildTarget, true);

            var analyzer = new StaticBatchingWithSRPBatcherAnalyzer();

            // Act
            var issues = analyzer.EnumerateIssues().ToList();

            // Assert
            Assert.AreEqual(1 + QualitySettings.count, issues.Count, "Should detect conflict when both SRP Batcher and Static Batching are enabled");
            Assert.AreEqual("URP0302", analyzer.Descriptor.Id);

            // Cleanup
            UnityEngine.Object.DestroyImmediate(rendererData);
            UnityEngine.Object.DestroyImmediate(urpAsset);
        }

        #endregion

        #region Other Tests (0401+)

        [Test]
        public void URP0401_LinearColorSpace_DetectsGammaSpace()
        {
            // Arrange
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            var analyzer = new LinearColorSpaceAnalyzer();

            // Act
            var issues = analyzer.EnumerateIssues().ToList();

            // Assert
            Assert.AreEqual(1, issues.Count, "Should detect Gamma color space");
            Assert.AreEqual("URP0401", analyzer.Descriptor.Id);
        }

        [Test]
        public void URP0401_LinearColorSpace_NoIssueWhenLinear()
        {
            // Arrange
            PlayerSettings.colorSpace = ColorSpace.Linear;
            var analyzer = new LinearColorSpaceAnalyzer();

            // Act
            var issues = analyzer.EnumerateIssues().ToList();

            // Assert
            Assert.AreEqual(0, issues.Count, "Should not detect issue when Linear color space is used");
        }

        [Test]
        public void URP0402_UnmigratedMaterials_HasCorrectDescriptor()
        {
            var analyzer = new UnmigratedMaterialsAnalyzer();
            Assert.IsNotNull(analyzer.Descriptor);
            Assert.AreEqual("URP0402", analyzer.Descriptor.Id);
            Assert.IsTrue(analyzer.Descriptor.Title.Contains("Materials"));
        }

        #endregion

        #region Quality Settings Tests

        [Test]
        public void URP0301_SRPBatcherDisabled_DetectsIssues()
        {
            // Arrange
            var urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            GraphicsSettings.defaultRenderPipeline = urpAsset;
            for (int i = 0; i < QualitySettings.names.Length && i < m_PreviousQualitySettings.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                QualitySettings.renderPipeline = urpAsset;
            }

            // Disable SRP Batcher
            urpAsset.useSRPBatcher = false;
            urpAsset.m_RendererDataList = new ScriptableRendererData[] { rendererData };
            urpAsset.m_DefaultRendererIndex = 0;

            // Set in quality settings instead of default
            int currentQualityLevel = QualitySettings.GetQualityLevel();
            QualitySettings.renderPipeline = urpAsset;

            var analyzer = new SRPBatcherAnalyzer();

            // Act
            var issues = analyzer.EnumerateIssues().ToList();

            // Assert
            Assert.AreEqual(1 + QualitySettings.count, issues.Count, "Should detect disabled SRP Batcher in quality settings");

            // Cleanup
            QualitySettings.renderPipeline = m_PreviousQualitySettings[currentQualityLevel];
            UnityEngine.Object.DestroyImmediate(rendererData);
            UnityEngine.Object.DestroyImmediate(urpAsset);
        }

        [Test]
        public void URP0001_MissingURPAsset_DetectsInMultipleQualityLevels()
        {
            // Arrange
            var analyzer = new MissingAssignedRenderPipeline();
            int currentQualityLevel = QualitySettings.GetQualityLevel();

            // Clear render pipeline in multiple quality levels
            List<int> clearedLevels = new List<int>();
            for (int i = 0; i < Mathf.Min(2, QualitySettings.names.Length); i++)
            {
                QualitySettings.SetQualityLevel(i);
                QualitySettings.renderPipeline = null;
                clearedLevels.Add(i);
            }
            QualitySettings.SetQualityLevel(currentQualityLevel);

            // Act
            var issues = analyzer.EnumerateIssues().ToList();

            // Assert
            Assert.IsTrue(issues.Count >= clearedLevels.Count,
                $"Should detect missing URP asset in at least {clearedLevels.Count} quality level(s)");

            // Cleanup happens in TearDown
        }

        #endregion

        #region Integration Tests

        private IEnumerable<IRenderingSettingsAnalyzer> GetAllAnalyzers()
        {
            var analyzerTypes = TypeCache.GetTypesDerivedFrom<IRenderingSettingsAnalyzer>();
            foreach (var type in analyzerTypes)
            {
                if (!type.IsAbstract && !type.IsInterface)
                {
                    yield return Activator.CreateInstance(type) as IRenderingSettingsAnalyzer;
                }
            }
        }

        [Test]
        public void AllAnalyzers_HaveUniqueDescriptorIds()
        {
            // Arrange
            var analyzers = GetAllAnalyzers().ToList();

            // Act
            var descriptorIds = analyzers.Select(a => a.Descriptor.Id).ToList();
            var uniqueIds = descriptorIds.Distinct().ToList();

            // Assert
            Assert.AreEqual(descriptorIds.Count, uniqueIds.Count,
                "All descriptor IDs should be unique. Duplicates found.");
        }

        [Test]
        public void AllAnalyzers_FollowNamingConvention()
        {
            // Arrange
            var analyzers = GetAllAnalyzers();
            var idPattern = new Regex(@"^URP\d{4}$");

            // Act & Assert
            foreach (var analyzer in analyzers)
            {
                string id = analyzer.Descriptor.Id;
                Assert.IsTrue(idPattern.IsMatch(id),
                    $"Descriptor ID '{id}' should match pattern 'URP####' where #### is a 4-digit number");
            }
        }

        [Test]
        public void AllAnalyzers_HaveValidDescriptors()
        {
            // Arrange
            var analyzers = GetAllAnalyzers();

            // Act & Assert
            foreach (var analyzer in analyzers)
            {
                var descriptor = analyzer.Descriptor;
                var typeName = analyzer.GetType().Name;

                Assert.IsNotNull(descriptor, $"{typeName}: Descriptor should not be null");
                Assert.IsFalse(string.IsNullOrEmpty(descriptor.Id), $"{typeName}: Descriptor ID should not be empty");
                Assert.IsFalse(string.IsNullOrEmpty(descriptor.Title), $"{typeName}: Descriptor Title should not be empty");
                Assert.IsFalse(string.IsNullOrEmpty(descriptor.Description), $"{typeName}: Descriptor Description should not be empty");
                Assert.IsFalse(string.IsNullOrEmpty(descriptor.Recommendation), $"{typeName}: Descriptor Recommendation should not be empty");
            }
        }

        [Test]
        public void AllAnalyzers_DescriptorIdsAreInValidRanges()
        {
            // Arrange
            var analyzers = GetAllAnalyzers();
            var idPattern = new Regex(@"^URP(\d{4})$");

            // Define valid ranges
            var validRanges = new[]
            {
                (start: 1, end: 100, name: "URP Assets"),
                (start: 101, end: 200, name: "Renderer"),
                (start: 201, end: 300, name: "Global Settings"),
                (start: 301, end: 400, name: "SRP and Static Batching"),
                (start: 401, end: 9999, name: "Other")
            };

            // Act & Assert
            foreach (var analyzer in analyzers)
            {
                string id = analyzer.Descriptor.Id;
                var typeName = analyzer.GetType().Name;
                var match = idPattern.Match(id);

                Assert.IsTrue(match.Success, $"{typeName}: Descriptor ID '{id}' should match pattern 'URP####'");

                int numericId = int.Parse(match.Groups[1].Value);

                bool isInValidRange = validRanges.Any(range => numericId >= range.start && numericId <= range.end);
                Assert.IsTrue(isInValidRange,
                    $"{typeName}: Descriptor ID '{id}' (numeric value: {numericId}) is not in any valid range. " +
                    $"Valid ranges are: {string.Join(", ", validRanges.Select(r => $"{r.name} ({r.start:D4}-{r.end:D4})"))}");
            }
        }

        [Test]
        public void AllAnalyzers_HaveCategoryAttribute()
        {
            // Arrange
            var analyzerTypes = TypeCache.GetTypesDerivedFrom<IRenderingSettingsAnalyzer>();

            // Act & Assert
            foreach (var type in analyzerTypes)
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                var categoryAttr = type.GetCustomAttribute<System.ComponentModel.CategoryAttribute>();
                Assert.IsNotNull(categoryAttr, $"{type.Name}: Should have [Category] attribute");

                var validCategories = new[] { "URP Assets", "Renderer", "Global Settings", "SRP and Static Batching", "Other" };
                Assert.Contains(categoryAttr.Category, validCategories,
                    $"{type.Name}: Category '{categoryAttr.Category}' should be one of the valid categories");
            }
        }

        [Test]
        public void AllAnalyzers_DescriptorIdsMatchCategoryAttribute()
        {
            // Arrange
            var analyzerTypes = TypeCache.GetTypesDerivedFrom<IRenderingSettingsAnalyzer>();
            var idPattern = new Regex(@"^URP(\d{4})$");

            // Act & Assert
            foreach (var type in analyzerTypes)
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                var analyzer = Activator.CreateInstance(type) as IRenderingSettingsAnalyzer;
                string id = analyzer.Descriptor.Id;
                var typeName = type.Name;

                // Get the Category attribute
                var categoryAttr = type.GetCustomAttribute<System.ComponentModel.CategoryAttribute>();
                Assert.IsNotNull(categoryAttr, $"{typeName}: Should have [Category] attribute");

                string category = categoryAttr.Category;
                var match = idPattern.Match(id);

                Assert.IsTrue(match.Success, $"{typeName}: Descriptor ID '{id}' should match pattern 'URP####'");

                int numericId = int.Parse(match.Groups[1].Value);
                var expectedRange = GetRangeForCategory(category);

                Assert.IsTrue(numericId >= expectedRange.start && numericId <= expectedRange.end,
                    $"{typeName}: ID {id} (numeric: {numericId}) should be in range {expectedRange.start:D4}-{expectedRange.end:D4} for category '{category}'");
            }
        }

        [Test]
        public void AllCategories_HaveAnalyzersInCorrectRanges()
        {
            // Arrange
            var analyzerTypes = TypeCache.GetTypesDerivedFrom<IRenderingSettingsAnalyzer>();
            var idPattern = new Regex(@"^URP(\d{4})$");
            var categoriesFound = new Dictionary<string, List<(string typeName, int id)>>();

            // Act
            foreach (var type in analyzerTypes)
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                var analyzer = Activator.CreateInstance(type) as IRenderingSettingsAnalyzer;
                var categoryAttr = type.GetCustomAttribute<System.ComponentModel.CategoryAttribute>();

                if (categoryAttr != null)
                {
                    var match = idPattern.Match(analyzer.Descriptor.Id);
                    if (match.Success)
                    {
                        int numericId = int.Parse(match.Groups[1].Value);
                        if (!categoriesFound.ContainsKey(categoryAttr.Category))
                            categoriesFound[categoryAttr.Category] = new List<(string, int)>();

                        categoriesFound[categoryAttr.Category].Add((type.Name, numericId));
                    }
                }
            }

            // Assert
            foreach (var kvp in categoriesFound)
            {
                var category = kvp.Key;
                var analyzers = kvp.Value;
                var expectedRange = GetRangeForCategory(category);

                foreach (var (typeName, numericId) in analyzers)
                {
                    Assert.IsTrue(numericId >= expectedRange.start && numericId <= expectedRange.end,
                        $"Category '{category}': {typeName} has ID {numericId:D4} which is outside expected range {expectedRange.start:D4}-{expectedRange.end:D4}");
                }
            }
        }

        private (int start, int end) GetRangeForCategory(string category)
        {
            return category switch
            {
                "URP Assets" => (1, 100),
                "Renderer" => (101, 200),
                "Global Settings" => (201, 300),
                "SRP and Static Batching" => (301, 400),
                "Other" => (401, 9999),
                _ => throw new ArgumentException($"Unknown category: {category}")
            };
        }

        #endregion
    }
}
