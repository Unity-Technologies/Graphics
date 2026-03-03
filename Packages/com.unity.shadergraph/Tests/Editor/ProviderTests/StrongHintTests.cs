using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.ShaderGraph.ProviderSystem.Tests
{
    class TestProvider : IProvider<IShaderFunction>
    {
        public string ProviderKey { get; private set; }
        public GUID AssetID { get; private set; }
        public IShaderFunction Definition { get; private set; }

        internal TestProvider(string testName, string path = null, IEnumerable<string> namespaces = null)
        {
            ProviderKey = testName;
            AssetID = path == null ? default : AssetDatabase.GUIDFromAssetPath(path);
            Definition = new ShaderFunction(testName, namespaces, null, new ShaderType("void"), null, null);
        }

        internal TestProvider(string testName, string hintKey, string hintValue, string path = null, IEnumerable<string> namespaces = null)
        {
            ProviderKey = testName;
            AssetID = path == null ? default : AssetDatabase.GUIDFromAssetPath(path);
            Definition = new ShaderFunction(testName, namespaces, null, new ShaderType("void"), null, new Dictionary<string, string> { { hintKey, hintValue } });
        }

        public IProvider Clone()
        {
            throw new NotImplementedException();
        }
    }

    [TestFixture]
    class StrongHintTests
    {
        private static void DoConflictTest(HintRegistry<IShaderObject> reg, string testName, IEnumerable<string> hintSet, int expectSuccess, bool expectMessage, Dictionary<string, object> values, List<string> messages)
        {
            values.Clear();
            messages.Clear();

            var obj = new TestObject(testName, hintSet);

            reg.ProcessObject(obj, null, out values, out messages);

            Assert.AreEqual(expectSuccess, values.Count, $"{testName} successes expected");
            Assert.AreEqual(expectMessage, messages.Count > 0, $"{testName} conflicts expected");
        }

        private static void DoDisplayNameTest(HintRegistry<IShaderObject> reg, string referenceName, string displayName, Dictionary<string, object> values, List<string> messages)
        {
            

            if (displayName == null)
                displayName = referenceName;

            var hints = new Dictionary<string, string>();
            if (displayName != null)
                hints.Add(Hints.Common.kDisplayName, displayName);

            var obj = new TestObject(referenceName, hints);

            DoOneHintTest(reg, obj, true, displayName, false, values, messages, Hints.Common.kDisplayName);
        }

        private static IShaderField Param(string testName, IShaderType shaderType, string hintKey, string hintValueRaw)
        {
            var hints = new Dictionary<string, string>() { { hintKey, hintValueRaw } };
            return new ShaderField(testName, false, false, shaderType, hints);
        }

        private static void DoOneHintTest<T, V>(HintRegistry<T> reg, IProvider<T> provider, bool expectsValue, V expectedValue, bool expectsMessage, Dictionary<string, object> values, List<string> messages, string hintKey = null) where T : IShaderObject
        {
            DoOneHintTest(reg, provider.Definition, expectsValue, expectedValue, expectsMessage, values, messages, hintKey, provider);
        }

        private static void DoOneHintTest<T, V>(HintRegistry<T> reg, T obj, bool expectsValue, V expectedValue, bool expectsMessage, Dictionary<string, object> values, List<string> messages, string hintKey = null, IProvider<T> provider = null) where T : IShaderObject
        {
            values.Clear();
            messages.Clear();

            string testName = obj.Name;
            if (hintKey == null)
                foreach (var hint in obj.Hints.Keys)
                    hintKey = hint;

            Assert.NotNull(hintKey, $"ShaderObject {obj.Name} must have a hint to test.");

            reg.ProcessObject((T)obj, provider, out values, out messages);

            Assert.AreEqual(expectsValue, values.Count > 0, $"{testName} for {hintKey}, value expected");
            Assert.AreEqual(expectsMessage, messages.Count > 0, $"{testName} for {hintKey}, message expected");
            if (expectsValue)
                Assert.AreEqual(expectedValue, values[hintKey], $"{testName} for {hintKey}, expected value");
        }

        struct TestObject : IShaderObject
        {
            public string Name { get; private set; }
            public IReadOnlyDictionary<string, string> Hints { get; private set; }
            public bool IsValid => true;

            internal TestObject(string name, IEnumerable<string> flagHints)
            {
                Name = name;
                Dictionary<string, string> hints = new();
                foreach (var hint in flagHints)
                    hints.Add(hint, "");
                Hints = hints;
            }

            internal TestObject(string name, Dictionary<string, string> hints)
            {
                Name = name;
                Hints = hints;
            }
        }

        [Test]
        public void ConflictClasses()
        {
            HintRegistry<IShaderObject> reg = new();

            var ha = new Hints.Flag<IShaderObject>("hA", "cA");
            var hb = new Hints.Flag<IShaderObject>("hB", "cA");
            var hc = new Hints.Flag<IShaderObject>("hC", "cB");
            var hd = new Hints.Flag<IShaderObject>("hD", "cB");

            reg.RegisterStrongHint(ha);
            reg.RegisterStrongHint(hb);
            reg.RegisterStrongHint(hc);
            reg.RegisterStrongHint(hd);

            Dictionary<string, object> values = new();
            List<string> messages = new();

            DoConflictTest(reg, "BaseNoResults", new string[] { }, 0, false, values, messages);
            DoConflictTest(reg, "OneConflict", new string[] { "hA", "hB" }, 0, true, values, messages);
            DoConflictTest(reg, "TwoConflicts", new string[] { "hA", "hB", "hC", "hD" }, 0, true, values, messages);
            DoConflictTest(reg, "OneConflictOneSafe", new string[] { "hA", "hC", "hD" }, 1, true, values, messages);
            DoConflictTest(reg, "Safe2Results", new string[] { "hA", "hC" }, 2, false, values, messages);
        }

        [Test]
        public void DisplayName()
        {
            HintRegistry<IShaderObject> reg = new();
            reg.RegisterStrongHint(new Hints.DisplayName<IShaderObject>());
            
            Dictionary<string, object> values = new();
            List<string> messages = new();

            DoDisplayNameTest(reg, "TestObject", null, values, messages); // default to reference name
            DoDisplayNameTest(reg, "TestObject", "Test Object DisplayName", values, messages);
        }

        
        [Test]
        public void TestRange()
        {
            // Most hints are _very simple_.
            HintRegistry<IShaderField> reg = new();
            reg.RegisterStrongHint(new Hints.Range());

            Dictionary<string, object> values = new();
            List<string> messages = new();

            var floatType = new ShaderType("float");
            var halfType = new ShaderType("half");
            var float2Type = new ShaderType("float2");

            // regularly supported use-cases
            DoOneHintTest(reg, Param("RangeNone", floatType, Hints.Param.kRange, null), true, new float[2] { 0, 1 }, false, values, messages);
            DoOneHintTest(reg, Param("RangeMin", floatType, Hints.Param.kRange, "-5"), true, new float[2] { -5, 0 }, false, values, messages);
            DoOneHintTest(reg, Param("RangeMax", floatType, Hints.Param.kRange, "5"), true, new float[2] { 0, 5 }, false, values, messages);
            DoOneHintTest(reg, Param("RangeMinMax", floatType, Hints.Param.kRange, "-5, 5"), true, new float[2] { -5, 5 }, false, values, messages);
            DoOneHintTest(reg, Param("RangeMaxMin", floatType, Hints.Param.kRange, "5, -5"), true, new float[2] { -5, 5 }, false, values, messages);
            DoOneHintTest(reg, Param("RangeHalfType", halfType, Hints.Param.kRange, null), true, new float[2] { 0, 1 }, false, values, messages);

            // invalid cases
            DoOneHintTest(reg, Param("RangeTooManyArgs", floatType, Hints.Param.kRange, "5, -10, 10, -5"), false, new float[2] { 0, 0 }, true, values, messages);
            DoOneHintTest(reg, Param("RangeTypeUnsupported", float2Type, Hints.Param.kRange, "-5, 5"), false, new float[2] { 0, 0 }, true, values, messages);
            DoOneHintTest(reg, Param("RangeInvalidString", floatType, Hints.Param.kRange, "blah blah blah"), false, new float[2] { 0, 1 }, true, values, messages);
        }

        [Test]
        public void TestCategory()
        {
            var reg = new HintRegistry<IShaderFunction>();
            reg.RegisterStrongHint(new Hints.SearchCategory());

            Dictionary<string, object> values = new();
            List<string> messages = new();

            var searchBase = new TestProvider("base", Hints.Func.kSearchCategory, "This/Is/A/Category");
            DoOneHintTest(reg, searchBase, true, "This/Is/A/Category", false, values, messages, Hints.Func.kSearchCategory);

            // Doesn't work on Mac; probably due to ADB warmup issues; TODO: determine why and correct this.
            //var searchPath = new TestProvider("path", kAllHintsPath, null);
            //DoOneHintTest(reg, searchPath, true, $"Reflected by Path/{kAllHintsPath}", false, values, messages, Hints.Func.kSearchCategory);

            // namespace should trump the file path fallback
            var searchNamespace = new TestProvider("namespace", kAllHintsPath, new string[] { "NamespaceA", "NamespaceB" });
            DoOneHintTest(reg, searchNamespace, true, "Reflected by Namespace/NamespaceA/NamespaceB", false, values, messages, Hints.Func.kSearchCategory);

            var searchUncategorized = new TestProvider("nocategory");
            DoOneHintTest(reg, searchUncategorized, true, "Uncategorized", false, values, messages, Hints.Func.kSearchCategory);
        }

        [Test]
        public void SummaryAndSynonyms()
        {
            // Most hints are _very simple_.
            HintRegistry<IShaderObject> reg = new();
            reg.RegisterStrongHint(new Hints.Tooltip<IShaderObject>());

            Dictionary<string, object> values = new();
            List<string> messages = new();

            string tooltipValue = "this is a tooltip";
            var noHint = new TestObject("NoHint", new Dictionary<string, string>());
            var matchHint = new TestObject("WithCorrectHintName", new Dictionary<string, string>() { { Hints.Common.kTooltip, tooltipValue } });
            var disqHint = new TestObject("WithDisqualifiedName", new Dictionary<string, string>() { { "Tooltip", tooltipValue } });
            var altHint = new TestObject("WithAlternateSynonym", new Dictionary<string, string>() { { "summary", tooltipValue } });

            DoOneHintTest(reg, noHint, false, "", false, values, messages, Hints.Common.kTooltip);
            DoOneHintTest(reg, matchHint, true, tooltipValue, false, values, messages, Hints.Common.kTooltip);
            DoOneHintTest(reg, disqHint, true, tooltipValue, false, values, messages, Hints.Common.kTooltip);
            DoOneHintTest(reg, altHint, true, tooltipValue, false, values, messages, Hints.Common.kTooltip);
        }

        readonly string kAllHintsPath = "Assets/Testing/IntegrationTests/Graphs/ProviderSystem/AllHints.hlsl";
        readonly string kAllHintsGraphPath = "Assets/Testing/IntegrationTests/Graphs/ProviderSystem/ShouldCompileProperlyOnImport.shadergraph";

        [Test]
        public void CheckAllHints()
        {
            // 'AllHints.hlsl' is used by 'ShouldCompileOnImport.shadergraph'
            // This reflected function covers most of our expected hint usage, so as a broad coverage integration test we can
            // ask it to reimport, which will populate error messages and fail the test if anything is awry.
            AssetDatabase.ImportAsset(kAllHintsPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(kAllHintsGraphPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }
    }
}
