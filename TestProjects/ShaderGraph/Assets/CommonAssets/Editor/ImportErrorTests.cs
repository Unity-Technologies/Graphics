using NUnit.Framework;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;
using GraphicsDeviceType = UnityEngine.Rendering.GraphicsDeviceType;
using System.Text.RegularExpressions;
using UnityEngine.TestTools;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    class ImportErrorTests
    {
        [OneTimeSetUp]
        public void Setup()
        {
        }

        public void TestImport(string assetPath, Regex expectedError = null, LogType logType = LogType.Error)
        {
            if (expectedError != null)
                LogAssert.Expect(logType, expectedError);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);
        }

        [Test]
        public void sg_multipleErrors()
        {
            TestImport(
                "Assets/CommonAssets/Graphs/ErrorTestGraphs/sg_multipleErrors.shadergraph",
                new Regex("sg_multipleErrors.*has 2 error.*Error from Error Node"),
                LogType.Error);
        }

        [Test]
        public void sg_multipleWarning()
        {
            TestImport(
                "Assets/CommonAssets/Graphs/ErrorTestGraphs/sg_multipleWarning.shadergraph",
                new Regex("sg_multipleWarning.*has 2 warning.*Warning from Error Node"),
                LogType.Warning);
        }

        [Test]
        public void sg_subSyntaxError()
        {
            TestImport(
                "Assets/CommonAssets/Graphs/ErrorTestGraphs/sg_subSyntaxError.shadergraph");
        }

        [Test]
        public void sg_subUnconnectedError()
        {
            TestImport(
                "Assets/CommonAssets/Graphs/ErrorTestGraphs/sg_subUnconnectedError.shadergraph");
        }

        [Test]
        public void sg_subValidationError()
        {
            TestImport(
                "Assets/CommonAssets/Graphs/ErrorTestGraphs/sg_subValidationError.shadergraph",
                new Regex("sg_subValidationError.*has 2 error.*Sub Graph.*sub_validationError"),
                LogType.Error);
        }

        [Test]
        public void sg_subWarning()
        {
            TestImport(
                "Assets/CommonAssets/Graphs/ErrorTestGraphs/sg_subWarning.shadergraph");
        }

        [Test]
        public void sg_syntaxError()
        {
            TestImport(
                "Assets/CommonAssets/Graphs/ErrorTestGraphs/sg_syntaxError.shadergraph");
        }

        [Test]
        public void sg_unconnectedErrors()
        {
            TestImport(
                "Assets/CommonAssets/Graphs/ErrorTestGraphs/sg_unconnectedErrors.shadergraph");
        }

        [Test]
        public void sg_unconnectedSubError()
        {
            TestImport(
                "Assets/CommonAssets/Graphs/ErrorTestGraphs/sg_unconnectedSubError.shadergraph");
        }

        [Test]
        public void sg_validationError()
        {
            TestImport(
                "Assets/CommonAssets/Graphs/ErrorTestGraphs/sg_validationError.shadergraph",
                new Regex("sg_validationError.*has 1 error.*Error from Error Node"),
                LogType.Error);
        }

        [Test]
        public void sg_validationWarning()
        {
            TestImport(
                "Assets/CommonAssets/Graphs/ErrorTestGraphs/sg_validationWarning.shadergraph",
                new Regex("sg_validationWarning.*has 1 warning.*Warning from Error Node"),
                LogType.Warning);
        }

        [Test]
        public void sub_multipleValidation()
        {
            TestImport(
                "Assets/CommonAssets/Graphs/ErrorTestGraphs/sub_multipleValidation.shadersubgraph",
                new Regex("sub_multipleValidation.*has 2 error.*Error from Error Node"),
                LogType.Error);
        }

        [Test]
        public void sub_multpleWarning()
        {
            TestImport(
                "Assets/CommonAssets/Graphs/ErrorTestGraphs/sub_multpleWarning.shadersubgraph",
                new Regex("sub_multpleWarning.*has 2 warning.*Warning from Error Node"),
                LogType.Warning);
        }

        [Test]
        public void sub_syntaxError()
        {
            TestImport(
                "Assets/CommonAssets/Graphs/ErrorTestGraphs/sub_syntaxError.shadersubgraph");
        }

        [Test]
        public void sub_unconnectedErrors()
        {
            TestImport(
                "Assets/CommonAssets/Graphs/ErrorTestGraphs/sub_unconnectedErrors.shadersubgraph");
        }

        [Test]
        public void sub_validationError()
        {
            // subgraph import will issue an error
            LogAssert.Expect(
                LogType.Error,
                new Regex("sub_validationError.*has 1 error.*Error from Error Node"));

            // then a dependent import on this shadergraph will issue an error
            LogAssert.Expect(
                LogType.Error,
                new Regex("sg_subValidationError.*has 2 error.*Sub Graph.*sub_validationError"));

            TestImport(
                "Assets/CommonAssets/Graphs/ErrorTestGraphs/sub_validationError.shadersubgraph");
        }

        [Test]
        public void sub_validationWarning()
        {
            TestImport(
                "Assets/CommonAssets/Graphs/ErrorTestGraphs/sub_validationWarning.shadersubgraph",
                new Regex("sub_validationWarning.*has 1 warning.*Warning from Error Node"),
                LogType.Warning);
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
        }
    }
}
