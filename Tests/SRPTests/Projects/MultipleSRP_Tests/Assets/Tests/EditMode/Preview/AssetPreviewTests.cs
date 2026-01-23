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
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using Object = UnityEngine.Object;

namespace Preview
{
    public class AssetPreviewTests
    {
        public const string k_PreviewFolderPrefix = "Assets/GraphicsTests/0x_Base/0003_Preview";
        public const string k_GeneratedImagesProjectFolder = "Assets/PreviewImporterGeneratedImages";
        public const string k_GeneratedImagesLibraryFolder = "Library/TempArtifacts/Debug";
        private int m_OriginalDesiredImportWorkerCount = -1;
        private int m_OriginalStandbyImportWorkerCount = -1;
        private int m_OriginalIdleImportWorkerShutdownDelayMilliseconds = -1;

        static TestCaseData[] s_TestCaseDataAssetPreviewUpdater =
        {
            new TestCaseData(new MaterialFactory($"TestMaterial-Built-In"), null)
                .SetName($"Preview generation for Material Built-In")
                .Returns(null),
            new TestCaseData(new MaterialFactory($"TestMaterial-{nameof(UniversalRenderPipelineAsset)}"), typeof(UniversalRenderPipelineAsset))
                .SetName($"Preview generation for Material {nameof(UniversalRenderPipelineAsset)}")
                .Returns(null),
            new TestCaseData(new MaterialFactory($"TestMaterial-{nameof(HDRenderPipelineAsset)}"), typeof(HDRenderPipelineAsset))
                .SetName($"Preview generation for Material {nameof(HDRenderPipelineAsset)}")
                .Returns(null),
            new TestCaseData(new GameObjectFactory($"Hammer.fbx"), null)
                .SetName($"Preview generation for Model Built-In")
                .Returns(null),
            new TestCaseData(new GameObjectFactory($"Hammer.fbx"), typeof(UniversalRenderPipelineAsset))
                .SetName($"Preview generation for Model {nameof(UniversalRenderPipelineAsset)}")
                .Returns(null),
            new TestCaseData(new GameObjectFactory($"Hammer.fbx"), typeof(HDRenderPipelineAsset))
                .SetName($"Preview generation for Model {nameof(HDRenderPipelineAsset)}")
                .Returns(null),
            new TestCaseData(new GameObjectFactory($"Hammer.prefab"), null)
                .SetName($"Preview generation for Prefab Built-In")
                .Returns(null),
            new TestCaseData(new GameObjectFactory($"Hammer.prefab"), typeof(UniversalRenderPipelineAsset))
                .SetName($"Preview generation for Prefab {nameof(UniversalRenderPipelineAsset)}")
                .Returns(null),
            new TestCaseData(new GameObjectFactory($"Hammer.prefab"), typeof(HDRenderPipelineAsset))
                .SetName($"Preview generation for Prefab {nameof(HDRenderPipelineAsset)}")
                .Returns(null),
        };

        static TestCaseData[] s_TestCaseDataPreviewImporter =
        {
            new TestCaseData(new MaterialFactory($"TestMaterial-Built-In"), null)
                .SetName($"Preview generation for Material Built-In with PreviewImporter")
                .Returns(null),
            new TestCaseData(new MaterialFactory($"TestMaterial-{nameof(UniversalRenderPipelineAsset)}"), typeof(UniversalRenderPipelineAsset))
                .SetName($"Preview generation for Material {nameof(UniversalRenderPipelineAsset)} with PreviewImporter")
                .Returns(null),
            new TestCaseData(new MaterialFactory($"TestMaterial-{nameof(HDRenderPipelineAsset)}"), typeof(HDRenderPipelineAsset))
                .SetName($"Preview generation for Material {nameof(HDRenderPipelineAsset)} with PreviewImporter")
                .Returns(null),
            new TestCaseData(new GameObjectFactory($"Hammer.fbx"), null)
                .SetName($"Preview generation for Model Built-In with PreviewImporter")
                .Returns(null),
            new TestCaseData(new GameObjectFactory($"Hammer.fbx"), typeof(UniversalRenderPipelineAsset))
                .SetName($"Preview generation for Model {nameof(UniversalRenderPipelineAsset)} with PreviewImporter")
                .Returns(null),
            new TestCaseData(new GameObjectFactory($"Hammer.fbx"), typeof(HDRenderPipelineAsset))
                .SetName($"Preview generation for Model {nameof(HDRenderPipelineAsset)} with PreviewImporter")
                .Returns(null),
            new TestCaseData(new GameObjectFactory($"Hammer.prefab"), null)
                .SetName($"Preview generation for Prefab Built-In with PreviewImporter")
                .Returns(null),
            new TestCaseData(new GameObjectFactory($"Hammer.prefab"), typeof(UniversalRenderPipelineAsset))
                .SetName($"Preview generation for Prefab {nameof(UniversalRenderPipelineAsset)} with PreviewImporter")
                .Returns(null),
            new TestCaseData(new GameObjectFactory($"Hammer.prefab"), typeof(HDRenderPipelineAsset))
                .SetName($"Preview generation for Prefab {nameof(HDRenderPipelineAsset)} with PreviewImporter")
                .Returns(null),
        };

        string m_CreatedObjectPath;
        AssetFactoryResultStatus m_AssetFactoryAssetFactoryResultStatus;

        [UnityTest]
        [TestCaseSource(nameof(s_TestCaseDataAssetPreviewUpdater))]
        public IEnumerator CreatePreview(AssetFactory objectFactory, Type renderPipelineAssetType)
        {
            using (new RenderPipelineScope(renderPipelineAssetType))
            {
                yield return null;

                //Arrange
                var testObject = ProduceNewObject(objectFactory, renderPipelineAssetType, out m_CreatedObjectPath);
                var loadIcon = LoadIcon(TestContext.CurrentContext.Test.Name);

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
        }

        [Ignore("issue: no worker when launching AssertSingleWorkerUsedAtLeastTwice https://jira.unity3d.com/browse/UUM-131927")]
        [UnityTest]
        [TestCaseSource(nameof(s_TestCaseDataPreviewImporter))]
        /// <summary>
        /// Test that the PreviewImporter on an Asset Import Worker generates a representative preview.
        /// </summary>
        /// <description>
        /// This is a test for a historic bug that occurred on Asset Import Workers in ~2023, where the worker would unload render pipeline assets after
        /// an import, causing an issue when generating subsequent previews on the same worker.
        /// This is deliberately not using the Preview API (AssetPreview.GetAssetPreview) which is tested elsewhere.
        /// The Preview manager caches previews, so this test forces the generation of a new preview by using AssetDatabaseExperimental.ForceProduceArtifact
        /// and specifying the PreviewImporter explicitly as that is where the bug occurred.
        /// </description>
        public IEnumerator CreatePreviewAssetImportWorker(AssetFactory objectFactory, Type renderPipelineAssetType)
        {
            // Configure the worker to dump preview images to disk for debugging purposes
            using (var diagnosticSwitchGuard = new Diagnostics.DiagnosticSwitchGuard(Diagnostics.DiagnosticSwitches.ExportPreviewPNGs, true))
            {
                using (new RenderPipelineScope(renderPipelineAssetType))
                {
                    yield return null;

                    var objectToPreview = ProduceNewObject(objectFactory, renderPipelineAssetType, out m_CreatedObjectPath);

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
                            var referenceImage = LoadIcon(String.Format("{0}_{1}", TestContext.CurrentContext.Test.Name, previewFilename));
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
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
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

            var testCase = new GraphicsTestCase(name);
            var image = testCase.ReferenceImage.Image;
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