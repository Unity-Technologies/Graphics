using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEditor.Rendering.Converter;
using UnityEngine.TestTools;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.Tools
{
    [Category("Graphics Tools")]
    class ConverterTests
    {
        class NotAConverterType
        {

        }

        // Mirrors the json layout written by Converters.ScanToFile
        [Serializable]
        class ScanFileItem
        {
            public string name;
            public string info;
            public string type;
        }

        [Serializable]
        class ScanFileConverter
        {
            public string container;
            public string converterType;
            public List<ScanFileItem> items;
        }

        [Serializable]
        class ScanFile
        {
            public string status;
            public int convertersCompleted;
            public int convertersFailed;
            public List<ScanFileConverter> converters;
        }

        static ScanFile ReadScanFile(string filePath)
        {
            return JsonUtility.FromJson<ScanFile>(File.ReadAllText(filePath));
        }

        class TestConverterItem : IRenderPipelineConverterItem
        {
            public string name { get; set; }
            public string info { get; set; }
            public bool isEnabled { get; set; } = true;
            public string isDisabledMessage { get; set; }

            public void OnClicked() { }
        }

        class TestFolderConverterItem : TestConverterItem, IFolderRenderPipelineConverterItem
        {
            public IList<IRenderPipelineConverterItem> children { get; } = new List<IRenderPipelineConverterItem>();
        }

        class ScanOnlyConverter : IRenderPipelineConverter
        {
            public static bool converted;

            public void Scan(Action<List<IRenderPipelineConverterItem>> onScanFinish)
            {
                var folder = new TestFolderConverterItem { name = "Folder", info = "FolderInfo" };
                folder.children.Add(new TestConverterItem { name = "ItemB", info = "InfoB" });

                onScanFinish(new List<IRenderPipelineConverterItem>
                {
                    new TestConverterItem { name = "ItemA", info = "InfoA" },
                    folder
                });
            }

            public Status Convert(IRenderPipelineConverterItem item, out string message)
            {
                converted = true;
                message = null;
                return Status.Success;
            }
        }

        // Reports its items only when the test tells it to, the way the asset converters report once the Search
        // service has finished indexing
        class DeferredScanConverter : IRenderPipelineConverter
        {
            public static DeferredScanConverter instance;

            Action<List<IRenderPipelineConverterItem>> m_OnScanFinish;

            public DeferredScanConverter()
            {
                instance = this;
            }

            public void Scan(Action<List<IRenderPipelineConverterItem>> onScanFinish)
            {
                m_OnScanFinish = onScanFinish;
            }

            public void FinishScan()
            {
                m_OnScanFinish(new List<IRenderPipelineConverterItem>
                {
                    new TestConverterItem { name = "DeferredItem", info = "DeferredInfo" }
                });
            }

            public Status Convert(IRenderPipelineConverterItem item, out string message)
            {
                message = null;
                return Status.Success;
            }
        }

        // An asset converter which has nothing to search for, the way PPv2Converter behaves when the Post Processing
        // package is not installed
        class NoQueriesAssetsConverter : AssetsConverter
        {
            public override bool isEnabled => false;
            public override string isDisabledMessage => "Nothing to search for";

            protected override List<(string query, string description)> contextSearchQueriesAndIds => null;

            protected override Status ConvertObject(UnityEngine.Object obj, StringBuilder message) => Status.Error;
        }

        class EmptyQueriesAssetsConverter : NoQueriesAssetsConverter
        {
            protected override List<(string query, string description)> contextSearchQueriesAndIds => new();
        }

        // An upgrader with no material upgraders to run, which has nothing to scan for
        class NoUpgradersMaterialUpgrader : RenderPipelineConverterMaterialUpgrader
        {
            protected override List<MaterialUpgrader> upgraders => new();
        }

        class ThrowingScanConverter : IRenderPipelineConverter
        {
            public void Scan(Action<List<IRenderPipelineConverterItem>> onScanFinish)
            {
                throw new InvalidOperationException("Scan failed");
            }

            public Status Convert(IRenderPipelineConverterItem item, out string message)
            {
                message = null;
                return Status.Success;
            }
        }

        [Test]
        public void ScanToFile_WritesScannedItemsWithoutConverting()
        {
            ScanOnlyConverter.converted = false;

            var filePath = Converters.ScanToFile(new List<Type> { typeof(ScanOnlyConverter) }, "ConverterScanTest.json");

            Assert.IsFalse(ScanOnlyConverter.converted, "ScanToFile must not convert any item.");
            Assert.IsFalse(Converters.IsScanInProgress, "A synchronous converter must leave the scan finished.");
            FileAssert.Exists(filePath);

            var scanFile = ReadScanFile(filePath);
            File.Delete(filePath);

            Assert.AreEqual("Completed", scanFile.status);
            Assert.AreEqual(1, scanFile.convertersCompleted);
            Assert.AreEqual(0, scanFile.convertersFailed);

            Assert.AreEqual(1, scanFile.converters.Count);
            Assert.AreEqual(nameof(ScanOnlyConverter), scanFile.converters[0].converterType);

            // Folders are flattened, so only the leaf items are reported
            var items = scanFile.converters[0].items;
            CollectionAssert.AreEqual(new[] { "ItemA", "ItemB" }, items.ConvertAll(item => item.name));
            CollectionAssert.AreEqual(new[] { "InfoA", "InfoB" }, items.ConvertAll(item => item.info));
            CollectionAssert.AreEqual(
                new[] { typeof(TestConverterItem).FullName, typeof(TestConverterItem).FullName },
                items.ConvertAll(item => item.type));
        }

        [Test]
        public void ScanToFile_ReportsScanInProgressUntilConvertersFinish()
        {
            string finishedStatus = null;
            var filePath = Converters.ScanToFile(new List<Type> { typeof(DeferredScanConverter) },
                "ConverterScanPendingTest.json", status => finishedStatus = status);

            try
            {
                Assert.IsTrue(Converters.IsScanInProgress, "The scan must be in progress until the converter reports.");
                Assert.IsNull(finishedStatus, "The scan must not be reported as finished until the converter reports.");

                // Results are only written once the scan has finished, there is nothing to read before that
                FileAssert.DoesNotExist(filePath);
            }
            finally
            {
                // Never leave a scan in progress behind, it would make every following scan throw
                DeferredScanConverter.instance.FinishScan();
            }

            Assert.IsFalse(Converters.IsScanInProgress, "The scan must be finished once the converter has reported.");
            Assert.AreEqual("Completed", finishedStatus);

            var scanFile = ReadScanFile(filePath);
            File.Delete(filePath);

            Assert.AreEqual("Completed", scanFile.status);
            Assert.AreEqual(1, scanFile.convertersCompleted);
            CollectionAssert.AreEqual(new[] { "DeferredItem" }, scanFile.converters[0].items.ConvertAll(item => item.name));
        }

        [Test]
        public void CancelScan_AbandonsScanAndDiscardsLateResults()
        {
            string finishedStatus = null;
            var filePath = Converters.ScanToFile(new List<Type> { typeof(DeferredScanConverter) },
                "ConverterScanCancelTest.json", status => finishedStatus = status);

            Converters.CancelScan();

            Assert.AreEqual("Cancelled", finishedStatus);
            Assert.IsFalse(Converters.IsScanInProgress, "A cancelled scan must no longer be in progress.");

            // Converters cannot be interrupted, so the one that was scanning still reports its results
            DeferredScanConverter.instance.FinishScan();

            Assert.AreEqual("Cancelled", finishedStatus, "A cancelled scan must only be reported once.");
            FileAssert.DoesNotExist(filePath);

            // Cancelling releases the scan, so the next one can start
            var completedFilePath = Converters.ScanToFile(new List<Type> { typeof(ScanOnlyConverter) }, "ConverterScanAfterCancelTest.json");
            Assert.AreEqual("Completed", ReadScanFile(completedFilePath).status);
            File.Delete(completedFilePath);
        }

        [Test]
        public void CancelScan_WithoutScan_DoesNothing()
        {
            Assert.IsFalse(Converters.IsScanInProgress);
            Assert.DoesNotThrow(Converters.CancelScan);
        }

        [Test]
        public void ScanToFile_ConverterThrowing_FinishesScanAndReportsFailure()
        {
            LogAssert.ignoreFailingMessages = true;
            try
            {
                var filePath = Converters.ScanToFile(new List<Type> { typeof(ThrowingScanConverter) }, "ConverterScanFailureTest.json");

                Assert.IsFalse(Converters.IsScanInProgress, "A converter that throws must not leave the scan hanging.");

                var scanFile = ReadScanFile(filePath);
                File.Delete(filePath);

                Assert.AreEqual("Failed", scanFile.status);
                Assert.AreEqual(0, scanFile.convertersCompleted);
                Assert.AreEqual(1, scanFile.convertersFailed);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }
        }

        // A converter with nothing to scan for must not leave the scan waiting for it.
        [TestCase(typeof(NoQueriesAssetsConverter), TestName = "{m}(Assets converter with no search queries)")]
        [TestCase(typeof(EmptyQueriesAssetsConverter), TestName = "{m}(Assets converter with empty search queries)")]
        [TestCase(typeof(NoUpgradersMaterialUpgrader), TestName = "{m}(Material upgrader with no upgraders)")]
        public void ScanToFile_ConverterWithNothingToScan_FinishesScanWithoutItems(Type converterType)
        {
            string finishedStatus = null;
            var filePath = Converters.ScanToFile(new List<Type> { converterType },
                "ConverterScanNothingToScanTest.json", status => finishedStatus = status);

            Assert.IsFalse(Converters.IsScanInProgress, "A converter with nothing to scan must not leave the scan hanging.");
            Assert.AreEqual("Completed", finishedStatus);

            var scanFile = ReadScanFile(filePath);
            File.Delete(filePath);

            // Nothing to scan for is an empty result, not a failure
            Assert.AreEqual("Completed", scanFile.status);
            Assert.AreEqual(1, scanFile.convertersCompleted);
            Assert.AreEqual(0, scanFile.convertersFailed);
            Assert.AreEqual(1, scanFile.converters.Count);
            Assert.That(scanFile.converters[0].items, Is.Null.Or.Empty);
        }

#if !PPV2_EXISTS
        // Without the Post Processing package the PPv2 converter has no types to search for.
        [Test]
        public void ScanToFile_PPv2ConverterWithoutPostProcessingPackage_FinishesScan()
        {
            string finishedStatus = null;
            var filePath = Converters.ScanToFile(new List<Type> { typeof(PPv2Converter) },
                "ConverterScanPPv2Test.json", status => finishedStatus = status);

            Assert.IsFalse(Converters.IsScanInProgress, "The PPv2 converter must not leave the scan hanging.");
            Assert.AreEqual("Completed", finishedStatus);

            var scanFile = ReadScanFile(filePath);
            File.Delete(filePath);

            Assert.AreEqual("Completed", scanFile.status);
            Assert.AreEqual("PPv2", scanFile.converters[0].converterType);
            Assert.That(scanFile.converters[0].items, Is.Null.Or.Empty);
        }
#endif

        [Test]
        public void ScanToFile_NoConverterCanBeCreated_ReportsFailure()
        {
            string finishedStatus = null;
            var filePath = Converters.ScanToFile(new List<Type> { typeof(NotAConverterType) },
                "ConverterScanUncreatableTest.json", status => finishedStatus = status);

            // Nothing was scanned, so this must not look like a successful, empty scan
            Assert.AreEqual("Failed", finishedStatus);

            var scanFile = ReadScanFile(filePath);
            File.Delete(filePath);

            Assert.AreEqual("Failed", scanFile.status);
            Assert.AreEqual(0, scanFile.convertersCompleted);
            Assert.AreEqual(1, scanFile.convertersFailed);
            Assert.That(scanFile.converters, Is.Null.Or.Empty);
        }

        [Test]
        public void ScanToFile_SomeConvertersCannotBeCreated_ReportsTheFailuresWithTheResults()
        {
            var filePath = Converters.ScanToFile(new List<Type> { typeof(ScanOnlyConverter), typeof(NotAConverterType) },
                "ConverterScanPartialTest.json");

            var scanFile = ReadScanFile(filePath);
            File.Delete(filePath);

            // The converter which did run leaves usable results, but the failure is still reported
            Assert.AreEqual("Completed", scanFile.status);
            Assert.AreEqual(1, scanFile.convertersCompleted);
            Assert.AreEqual(1, scanFile.convertersFailed);
            Assert.AreEqual(1, scanFile.converters.Count);
        }

        [Test]
        public void BatchModeFails()
        {
            bool ok = Converters.RunInBatchMode(new List<Type>() { typeof(NotAConverterType) });
            Assert.IsFalse(ok);
        }

        [Test]
        public void RunInBatchMode_LogsUsageWarning()
        {
            LogAssert.Expect(
                LogType.Warning,
                "Using this API can lead to incomplete or unpredictable conversion outcomes. " +
                "For reliable results, please perform the conversion via the dedicated window: " +
                "Window > Rendering > Render Pipeline Converter."
            );

            bool _ = Converters.RunInBatchMode(new List<Type>() {});
        }

        public static IEnumerable<TestCaseData> TestCases()
        {
            yield return new TestCaseData(
                "BuiltInToURP",
                new List<string> { "Material" },
                true,
                new List<Type> { typeof(BuiltInToURP3DMaterialUpgrader) }
            ).SetName("{m}(When Using Inclusive filter with Material in the correct category. The Filter only returns that converter)");
            yield return new TestCaseData(
                "BuiltInToURP",
                new List<string> { "ParametricToFreeformLight" },
                true,
                new List<Type>()
            ).SetName("{m}(When Using Inclusive filter with Light in the wrong category. The Filter returns nothing)");

            yield return new TestCaseData(
                "BuiltInToURP",
                new List<string>
                {
                    "RenderSettings",
                    "PPv2"
                },
                false,
                new List<Type>
                {
                    typeof(AnimationClipConverter),
                    typeof(BuiltInToURP3DMaterialUpgrader),
                    typeof(BuiltInToURP3DReadonlyMaterialConverter),
                }
            ).SetName("{m}(When Using Exclusive filter. The filter returns everything except the given ids)");

            yield return new TestCaseData(
                "BuiltInToURP",
                new List<string>(),
                true,
                new List<Type>() // No converters match
            ).SetName("{m}(When Using Inclusive filter with no converters. The filter returns nothing)");

            yield return new TestCaseData(
                "BuiltInToURP",
                new List<string>(),
                false,
                 new List<Type>
                 {
                    typeof(PPv2Converter),
                    typeof(BuiltInToURP3DRenderSettingsConverter),
                    typeof(AnimationClipConverter),
                    typeof(BuiltInToURP3DMaterialUpgrader),
                    typeof(BuiltInToURP3DReadonlyMaterialConverter),
                 }
            ).SetName("{m}(BuiltInToURP - When Using Exclusive filter with no converters. The filter returns everything)");

            yield return new TestCaseData(
                "BuiltInToURP2D",
                new List<string>(),
                false,
                 new List<Type>
                 {
                    typeof(BuiltInToURP2DRenderSettingsConverter),
                    typeof(BuiltInToURP2DReadonlyMaterialConverter),
                 }
            ).SetName("{m}(BuiltInToURP2D - When Using Exclusive filter with no converters. The filter returns everything)");

            yield return new TestCaseData(
                "UpgradeURP2DAssets",
                new List<string>(),
                false,
                new List<Type>
                {
                    typeof(ParametricToFreeformLightUpgrader)
                }
            ).SetName("{m}(UpgradeURP2DAssets - When Using Exclusive filter with no converters. The filter returns everything)");
        }

        [TestCaseSource(nameof(TestCases))]
        public void FilterConverters_ShouldReturnExpectedConverters(
            string containerName,
            List<string> filterList,
            bool filterModeIsInclusive,
            List<Type> expectedTypes)
        {
            var actualTypes = Converters.FilterConverters(containerName, filterList, filterModeIsInclusive);
            CollectionAssert.AreEquivalent(expectedTypes, actualTypes);
        }

        [Test]
        public void CommandLine_ArgumentsParsedProperly()
        {
            // case 1 - working command
            var dummyArgs = "-batchmode -executeMethod UnityEditor.Rendering.Universal.Converters.RunInBatchModeCmdLine --flagA -paramA 1 2 3 --flagB --flagC -paramB --flagD";
            var actualArgs = Converters.ParseArgs(dummyArgs.Split(' '));
            Dictionary<string, List<string>> expectedArgs = new()
            {
                { "-executeMethod", new List<string> {"UnityEditor.Rendering.Universal.Converters.RunInBatchModeCmdLine"} },
                { "-paramA", new List<string> {"1", "2", "3"} },
                { "-paramB", new List<string> {} },
                { "Flags", new List<string> {"--flagA", "--flagB", "--flagC", "--flagD"} }
            };
            CollectionAssert.AreEquivalent(expectedArgs, actualArgs);

            // case 2 - Error: No -batchmode flag
            dummyArgs = "-executeMethod UnityEditor.Rendering.Universal.Converters.RunInBatchModeCmdLine --flagA -paramA 1 2 3";
            var exception = Assert.Throws<ArgumentException>(() => Converters.ParseArgs(dummyArgs.Split(' ')));
            Assert.That(exception.Message, Is.EqualTo("No -batchmode argument found. Exiting."));

            // case 3 - Error: Adding values without a key
            dummyArgs = "-batchmode -executeMethod UnityEditor.Rendering.Universal.Converters.RunInBatchModeCmdLine --flagA wrongArgument";
            exception = Assert.Throws<ArgumentException>(() => Converters.ParseArgs(dummyArgs.Split(' ')));
            Assert.That(exception.Message, Is.EqualTo("Unrecognized argument: wrongArgument"));
        }

#pragma warning disable CS0618 // Type or member is obsolete
        public static IEnumerable<TestCaseData> TestCasesDeprecated()
        {
            yield return new TestCaseData(
                ConverterContainerId.BuiltInToURP,
                new List<ConverterId> { ConverterId.Material },
                ConverterFilter.Inclusive,
                new List<Type> { typeof(BuiltInToURP3DMaterialUpgrader) }
            ).SetName("{m}(When Using Inclusive filter with Material in the correct category. The Filter only returns that converter)");
            yield return new TestCaseData(
                ConverterContainerId.BuiltInToURP,
                new List<ConverterId> { ConverterId.ParametricToFreeformLight },
                ConverterFilter.Inclusive,
                new List<Type>()
            ).SetName("{m}(When Using Inclusive filter with Light in the wrong category. The Filter returns nothing)");

            yield return new TestCaseData(
                ConverterContainerId.BuiltInToURP,
                new List<ConverterId>
                {
                    ConverterId.RenderSettings,
#if PPV2_EXISTS
                    ConverterId.PPv2
#endif
                },
                ConverterFilter.Exclusive,
                new List<Type>
                {
                    typeof(AnimationClipConverter),
                    typeof(BuiltInToURP3DMaterialUpgrader),
                    typeof(BuiltInToURP3DReadonlyMaterialConverter),
                }
            ).SetName("{m}(When Using Exclusive filter. The filter returns everything except the given ids)");

            yield return new TestCaseData(
                ConverterContainerId.BuiltInToURP,
                new List<ConverterId>(),
                ConverterFilter.Inclusive,
                new List<Type>() // No converters match
            ).SetName("{m}(When Using Inclusive filter with no converters. The filter returns nothing)");

            yield return new TestCaseData(
                ConverterContainerId.BuiltInToURP,
                new List<ConverterId>(),
                ConverterFilter.Exclusive,
                 new List<Type>
                 {
#if PPV2_EXISTS
                    typeof(PPv2Converter),
#endif
                    typeof(BuiltInToURP3DRenderSettingsConverter),
                    typeof(AnimationClipConverter),
                    typeof(BuiltInToURP3DMaterialUpgrader),
                    typeof(BuiltInToURP3DReadonlyMaterialConverter),
                 }
            ).SetName("{m}(BuiltInToURP - When Using Exclusive filter with no converters. The filter returns everything)");

            yield return new TestCaseData(
                ConverterContainerId.BuiltInToURP2D,
                new List<ConverterId>(),
                ConverterFilter.Exclusive,
                 new List<Type>
                 {
                    typeof(BuiltInToURP2DRenderSettingsConverter),
                    typeof(BuiltInToURP2DReadonlyMaterialConverter),
                 }
            ).SetName("{m}(BuiltInToURP2D - When Using Exclusive filter with no converters. The filter returns everything)");

            yield return new TestCaseData(
                ConverterContainerId.UpgradeURP2DAssets,
                new List<ConverterId>(),
                ConverterFilter.Exclusive,
                new List<Type>
                {
                    typeof(URP3DToURP2DReadonlyMaterialConverter),
                    typeof(ParametricToFreeformLightUpgrader)
                }
            ).SetName("{m}(UpgradeURP2DAssets - When Using Exclusive filter with no converters. The filter returns everything)");
        }

        [TestCaseSource(nameof(TestCasesDeprecated))]
        public void FilterConverters_ShouldReturnExpectedConverters_DeprecatedAPI(
                ConverterContainerId containerId,
                List<ConverterId> filterList,
                ConverterFilter filterMode,
                List<Type> expectedTypes)
        {
            var actualTypes = Converters.FilterConverters(containerId, filterList, filterMode);
            CollectionAssert.AreEquivalent(expectedTypes, actualTypes);
        }

        [Test]
        public void CommandLine_SuggestCorrectCommands()
        {
            var expectedOutputTemplate = "The method you're trying to use is deprecated. Try running the following command in the command line:\n{0}";

            // case 1
            var containerID = ConverterContainerId.BuiltInToURP;
            var filterList = new List<ConverterId> { ConverterId.Material, ConverterId.RenderSettings };
            Converters.SuggestUpdatedCommand(containerID.ToString(), filterList.ConvertAll(id => id.ToString()), true);
            var suggestedCommand = "<path to Unity> -projectPath <path to project> -batchmode -executeMethod UnityEditor.Rendering.Universal.Converters.RunInBatchModeCmdLine --inclusive -container BuiltInToURP -typesFilter Material RenderSettings";
            LogAssert.Expect(LogType.Log, String.Format(expectedOutputTemplate, suggestedCommand));

            // case 2
            containerID = ConverterContainerId.BuiltInToURP2D;
            filterList = new List<ConverterId> { };
            Converters.SuggestUpdatedCommand(containerID.ToString(), filterList.ConvertAll(id => id.ToString()), false);
            suggestedCommand = "<path to Unity> -projectPath <path to project> -batchmode -executeMethod UnityEditor.Rendering.Universal.Converters.RunInBatchModeCmdLine --exclusive -container BuiltInToURP2D";
            LogAssert.Expect(LogType.Log, String.Format(expectedOutputTemplate, suggestedCommand));
        }

#pragma warning restore CS0618 // Type or member is obsolete
    }

}
