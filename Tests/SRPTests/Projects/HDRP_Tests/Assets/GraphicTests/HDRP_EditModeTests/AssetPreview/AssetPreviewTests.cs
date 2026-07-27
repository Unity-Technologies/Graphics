using System;
using System.Collections;
using Common;
using Framework;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using Object = UnityEngine.Object;

namespace Preview
{
    [TestFixture("HDRP", TestName = "HDRP")]
    public class AssetPreviewTests
    {
        public const string k_PreviewFolderPrefix = "Assets/GraphicTests/HDRP_EditModeTests/AssetPreview/PreviewData";
        public const string k_GeneratedImagesProjectFolder = "Assets/PreviewImporterGeneratedImages";
        public const string k_GeneratedImagesLibraryFolder = "Library/TempArtifacts/Debug";

        private readonly string m_PipelineName;
        private readonly Type m_RenderPipelineAssetType;
        private RenderPipelineScope m_RenderPipelineScope;

        private int m_OriginalDesiredImportWorkerCount = -1;
        private int m_OriginalStandbyImportWorkerCount = -1;
        private int m_OriginalIdleImportWorkerShutdownDelayMilliseconds = -1;

        string m_CreatedObjectPath;
        AssetFactoryResultStatus m_AssetFactoryAssetFactoryResultStatus;

        public AssetPreviewTests(string pipelineName)
        {
            m_PipelineName = pipelineName;
            m_RenderPipelineAssetType = pipelineName switch
            {
                "HDRP" => typeof(HDRenderPipelineAsset),
                _ => null
            };
        }

        string GetPipelineName()
        {
            // Use BIRP instead of Built-In for consistency
            return m_PipelineName == "Built-In" ? "BIRP" : m_PipelineName;
        }

        IEnumerator CreatePreviewInternal(AssetFactory objectFactory, string assetTypeName)
        {
            yield return null;

            //Arrange
            var testObject = ProduceNewObject(objectFactory, m_RenderPipelineAssetType, out m_CreatedObjectPath);
            var loadIcon = LoadIcon($"AssetPreviewTests_{GetPipelineName()}_{assetTypeName}");

            yield return new WaitForEndOfFrame();

            //Act
            yield return WaitForShadersToCompile(testObject, m_CreatedObjectPath);
            var tex = AssetPreviewUpdater.CreatePreviewForAsset(testObject, null, m_CreatedObjectPath);

            //Assert
            ImageAssert.AreEqual(loadIcon, tex, new ImageComparisonSettings
            {
                TargetWidth = 128,
                TargetHeight = 128,
                AverageCorrectnessThreshold = 0.005f,
                PerPixelCorrectnessThreshold = 0.005f
            }, saveFailedImage: true);
        }

        [UnityTest]
        public IEnumerator CreatePreview_Model()
        {
            yield return CreatePreviewInternal(new GameObjectFactory("Hammer.fbx"), "Model");
        }

        [UnityTest]
        public IEnumerator CreatePreview_Prefab()
        {
            yield return CreatePreviewInternal(new GameObjectFactory("Hammer.prefab"), "Prefab");
        }

        [UnityTest]
        public IEnumerator CreatePreview_Material()
        {
            var threadingMode = SystemInfo.renderingThreadingMode;
            bool nativeGfxJobs = threadingMode == RenderingThreadingMode.NativeGraphicsJobs
                || threadingMode == RenderingThreadingMode.NativeGraphicsJobsWithoutRenderThread
                || threadingMode == RenderingThreadingMode.NativeGraphicsJobsSplitThreading;
            bool isD3D12 = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12;
            if (m_PipelineName == "HDRP" && isD3D12 && nativeGfxJobs)
                Assert.Ignore("HDRP material preview renders blank under native/split graphics jobs on D3D12. Tracking: UUM-147205");

            yield return CreatePreviewInternal(new MaterialFactory($"TestMaterial-{GetPipelineName()}"), "Material");
        }

        IEnumerator CreatePreviewAssetImportWorkerInternal(AssetFactory objectFactory, string assetTypeName)
        {
            // Configure the worker to dump preview images to disk for debugging purposes
            using (var diagnosticSwitchGuard = new Diagnostics.DiagnosticSwitchGuard(Diagnostics.DiagnosticSwitches.ExportPreviewPNGs, true))
            {
                yield return null;

                var objectToPreview = ProduceNewObject(objectFactory, m_RenderPipelineAssetType, out m_CreatedObjectPath);

                // Prepare to force generate the preview using the PreviewImporter specifically
                var createdObjectGUID = AssetDatabase.GUIDFromAssetPath(m_CreatedObjectPath);
                var artifactKey = new ArtifactKey(createdObjectGUID, typeof(PreviewImporter));

                // Snapshot worker logs before import iterations to establish baseline
                var preIterationSnapshot = WorkerLogAnalyzer.SnapshotWorkerLogs(m_CreatedObjectPath);
                Debug.Log($"Pre-iteration worker log snapshot: {preIterationSnapshot.GetSummary()}");

                // Perform the generation itself more than once. The bug should occur on subsequent generations on the same worker.
                // This is because workers unload any assets loaded during the import, so any problems caused by unloading the render pipeline asset should
                // occur on a subsequent import.
                int iterations = 5;
                for (int importIteration = 0; importIteration < iterations; importIteration++)
                {
                    ClearGeneratedPreviewImporterImages();

                    var importResultID = AssetDatabaseExperimental.ForceProduceArtifact(artifactKey);
                    Assert.IsTrue(importResultID.isValid, "AssetDatabaseExperimental.ForceProduceArtifact should return a valid Import Result ID (it is synchronous)");

                    // The PreviewImporter debug mode writes the preview(s) to disk in the project Library
                    var pathToGeneratedImages = "Library/TempArtifacts/Debug/" + m_CreatedObjectPath;
                    FileUtil.CopyDirectoryRecursive(pathToGeneratedImages, k_GeneratedImagesProjectFolder);
                    AssetDatabase.Refresh();
                    var previewFilepaths = System.IO.Directory.GetFiles(k_GeneratedImagesProjectFolder, "*.png", System.IO.SearchOption.AllDirectories);
                    Assert.IsTrue(previewFilepaths.Length > 0, $"There should be at least one generated preview in {k_GeneratedImagesProjectFolder}");

                    foreach (var previewFilepath in previewFilepaths)
                    {
                        var previewFilename = System.IO.Path.GetFileName(previewFilepath);
                        var previewProjectFilepath = String.Format("{0}/{1}", k_GeneratedImagesProjectFolder, previewFilename);
                        var textureImporter = AssetImporter.GetAtPath(previewProjectFilepath) as TextureImporter;
                        textureImporter.isReadable = true;
                        textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
                        textureImporter.SaveAndReimport();
                        var generatedPreview = AssetDatabase.LoadAssetAtPath<Texture2D>(previewProjectFilepath);

                        Assert.IsNotNull(generatedPreview, $"The generated preview at {previewFilename} should be loadable");
                        var referenceImage = LoadIcon(String.Format("AssetPreviewTests_Importer_{0}_{1}_{2}", GetPipelineName(), assetTypeName, previewFilename));
                        //Assert
                        ImageAssert.AreEqual(referenceImage, generatedPreview, new ImageComparisonSettings
                        {
                            TargetWidth = 128,
                            TargetHeight = 128,
                            AverageCorrectnessThreshold = 0.005f,
                            PerPixelCorrectnessThreshold = 0.005f
                        }, saveFailedImage: true);
                    }
                }

                // Snapshot worker logs after import iterations and verify single worker usage
                var postIterationSnapshot = WorkerLogAnalyzer.SnapshotWorkerLogs(m_CreatedObjectPath);
                Debug.Log($"Post-iteration worker log snapshot: {postIterationSnapshot.GetSummary()}");

                var importDifference = postIterationSnapshot.GetDifference(preIterationSnapshot);
                Debug.Log($"Import difference: {importDifference.GetSummary()}");

                // Assert that all imports occurred on a single worker
                WorkerLogAnalyzer.AssertSingleWorkerUsedAtLeastTwice(importDifference, m_CreatedObjectPath, iterations);
            }
        }

        /// <summary>
        /// Test that the PreviewImporter on an Asset Import Worker generates a representative preview for Model.
        /// </summary>
        /// <description>
        /// This is a test for a historic bug that occurred on Asset Import Workers in ~2023, where the worker would unload render pipeline assets after
        /// an import, causing an issue when generating subsequent previews on the same worker.
        /// This is deliberately not using the Preview API (AssetPreview.GetAssetPreview) which is tested elsewhere.
        /// The Preview manager caches previews, so this test forces the generation of a new preview by using AssetDatabaseExperimental.ForceProduceArtifact
        /// and specifying the PreviewImporter explicitly as that is where the bug occurred.
        /// </description>
        [UnityTest]
        public IEnumerator CreatePreviewAssetImportWorker_Model()
        {
            if (m_RenderPipelineAssetType == typeof(HDRenderPipelineAsset))
                Assert.Ignore("Reports incompatible keyword space errors");

            yield return CreatePreviewAssetImportWorkerInternal(new GameObjectFactory("Hammer.fbx"), "Model");
        }

        /// <summary>
        /// Test that the PreviewImporter on an Asset Import Worker generates a representative preview for Prefab.
        /// </summary>
        [UnityTest]
        public IEnumerator CreatePreviewAssetImportWorker_Prefab()
        {
            if (m_RenderPipelineAssetType == typeof(HDRenderPipelineAsset))
                Assert.Ignore("Reports incompatible keyword space errors");

            yield return CreatePreviewAssetImportWorkerInternal(new GameObjectFactory("Hammer.prefab"), "Prefab");
        }

        /// <summary>
        /// Test that the PreviewImporter on an Asset Import Worker generates a representative preview for Material.
        /// </summary>
        [UnityTest]
        public IEnumerator CreatePreviewAssetImportWorker_Material()
        {
            if (m_RenderPipelineAssetType == typeof(HDRenderPipelineAsset))
                Assert.Ignore("Reports incompatible keyword space errors");

            yield return CreatePreviewAssetImportWorkerInternal(new MaterialFactory($"TestMaterial-{GetPipelineName()}"), "Material");
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            // Switch to the render pipeline for this test fixture
            m_RenderPipelineScope = new RenderPipelineScope(m_RenderPipelineAssetType);

            // Request that only one worker is used, to give the maximum possibility that multiple previews are generated on the same worker.
            m_OriginalDesiredImportWorkerCount = EditorUserSettings.desiredImportWorkerCount;
            m_OriginalStandbyImportWorkerCount = EditorUserSettings.standbyImportWorkerCount;
            m_OriginalIdleImportWorkerShutdownDelayMilliseconds = EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds;
            EditorUserSettings.desiredImportWorkerCount = 1;
            EditorUserSettings.standbyImportWorkerCount = 1;
            EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds = 1000; // 1 second

            // Wait for other workers to terminate before proceeding with the test
            ImportWorkerTestUtility.WaitForDesiredWorkerCountOrFewer(EditorUserSettings.desiredImportWorkerCount + EditorUserSettings.standbyImportWorkerCount);
        }

        [OneTimeTearDown]
        public void OneTimeTeardown()
        {
            // Restore the original render pipeline
            m_RenderPipelineScope?.Dispose();
            m_RenderPipelineScope = null;

            if (m_OriginalDesiredImportWorkerCount != -1)
            {
                EditorUserSettings.desiredImportWorkerCount = m_OriginalDesiredImportWorkerCount;
                m_OriginalDesiredImportWorkerCount = -1;
                // Note: No need to wait for workers to spawn - they are created on-demand when import work is needed
            }
            if (m_OriginalStandbyImportWorkerCount != -1)
            {
                EditorUserSettings.standbyImportWorkerCount = m_OriginalStandbyImportWorkerCount;
                m_OriginalStandbyImportWorkerCount = -1;
            }
            if (m_OriginalIdleImportWorkerShutdownDelayMilliseconds != -1)
            {
                EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds = m_OriginalIdleImportWorkerShutdownDelayMilliseconds;
                m_OriginalIdleImportWorkerShutdownDelayMilliseconds = -1;
            }
        }

        [UnityTearDown]
        // ReSharper disable once UnusedMember.Global
        public IEnumerator TearDown()
        {
            yield return null;

            if (m_AssetFactoryAssetFactoryResultStatus == AssetFactoryResultStatus.Created && !string.IsNullOrEmpty(m_CreatedObjectPath) && AssetDatabase.AssetPathExists(m_CreatedObjectPath))
            {
                AssetDatabase.DeleteAsset(m_CreatedObjectPath);
                AssetDatabase.Refresh();
            }
            ClearGeneratedPreviewImporterImages();
        }

        void ClearGeneratedPreviewImporterImages()
        {
            if (System.IO.Directory.Exists(k_GeneratedImagesProjectFolder))
            {
                AssetDatabase.DeleteAsset(k_GeneratedImagesProjectFolder);
                AssetDatabase.Refresh();
            }
            if (System.IO.Directory.Exists(k_GeneratedImagesLibraryFolder))
            {
                System.IO.Directory.Delete(k_GeneratedImagesLibraryFolder, true);
            }
        }

        Texture2D LoadIcon(string name)
        {
            if (name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - 4);

            var image = new ReferenceImage(name).Image;
            Assert.IsTrue(image != null, $"Reference image not found for test case {name}");
            return image;
        }


        Object ProduceNewObject(AssetFactory objectFactory, Type renderPipelineType, out string path)
        {
            m_AssetFactoryAssetFactoryResultStatus = objectFactory.GetObjectWithPath(k_PreviewFolderPrefix, renderPipelineType, out var testObject, out path);
            return testObject;
        }

        IEnumerator WaitForShadersToCompile(Object testObject, string path, float maxTime = 300)
        {
            AssetPreviewUpdater.CreatePreviewForAsset(testObject, null, path);
            yield return null;
            yield return WaitForChange(() => !ShaderUtil.anythingCompiling, maxTime);
        }

        static IEnumerator WaitForChange(Func<bool> changeCheck, double maxWaitTime = 1)
        {
            var startTime = EditorApplication.timeSinceStartup;
            while (!changeCheck.Invoke() && startTime + maxWaitTime > EditorApplication.timeSinceStartup)
            {
                yield return null;
            }
        }
    }

    public enum AssetFactoryResultStatus
    {
        Created,
        Loaded
    }

    public abstract class AssetFactory
    {
        public abstract AssetFactoryResultStatus GetObjectWithPath(string prefix, Type renderPipelineType, out Object newObject, out string path);

        public string name;

        public AssetFactory(string name)
        {
            this.name = name;
        }
    }

    public class MaterialFactory : AssetFactory
    {
        public MaterialFactory(string name) : base(name)
        {
        }

        public override AssetFactoryResultStatus GetObjectWithPath(string prefix, Type renderPipelineType, out Object newObject, out string path)
        {
            string shaderName;
            if (renderPipelineType != null)
            {
                var asset = RenderPipelineUtils.LoadAsset(renderPipelineType);
                shaderName = asset.defaultShader.name;
            }
            else
                shaderName = "Standard";

            Assert.That(shaderName, Is.Not.Empty,
                $"Can not test Material Preview outside of predefined list of SRPs. Current RenderPipelineType is {GraphicsSettings.currentRenderPipelineAssetType?.FullName}");

            var shader = Shader.Find(shaderName);
            newObject = new Material(shader);
            path = $"{prefix}/{name}.mat";

            AssetDatabase.CreateAsset(newObject, path);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            return AssetFactoryResultStatus.Created;
        }
    }

    public class GameObjectFactory : AssetFactory
    {
        public GameObjectFactory(string name) : base(name)
        {
        }

        public override AssetFactoryResultStatus GetObjectWithPath(string prefix, Type renderPipelineType, out Object newObject, out string path)
        {
            path = $"{AssetPreviewTests.k_PreviewFolderPrefix}/{name}";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            newObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            Assert.That(newObject, Is.Not.Null, $"Couldn't load a {name} by this {path} for testing.");

            return AssetFactoryResultStatus.Loaded;
        }
    }
}
