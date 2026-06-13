using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.GraphAuthoring.Editor.ProviderSystem;
using Unity.GraphAuthoring.Editor.ProviderSystem.Hints;
using Hints = Unity.GraphAuthoring.Editor.ProviderSystem.Hints;

namespace UnityEditor.ShaderGraph.ProviderSystem.Tests
{
    class TestProvider : IProvider<IShaderFunction>
    {
        public string ProviderKey { get; private set; }
        public UnityEngine.GUID AssetID { get; private set; }
        public IShaderFunction Definition { get; private set; }

        internal TestProvider(string testName, IShaderFunction func)
        {
            ProviderKey = testName;
            Definition = func;
        }

        internal TestProvider(string testName, string path = null, IEnumerable<string> namespaces = null)
        {
            ProviderKey = testName;
            AssetID = path == null ? default : AssetDatabase.GUIDFromAssetPath(path);
            Definition = new ShaderFunction(testName, namespaces, null, new ShaderType("void"), null, null);
        }

        public IProvider Clone() => throw new NotImplementedException();
    }

    // Minimal IDefinition for hint tests — no hints, just Name and IsValid.
    struct TestObject : IDefinition
    {
        public string Name { get; }
        public bool IsValid => true;
        internal TestObject(string name) { Name = name; }
    }

    [TestFixture]
    class StrongHintTests
    {
        static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();

        // Calls Initialize() on a freshly constructed hint — bypasses registry machinery.
        static IStrongHint<T> ResolveOne<T>(
            IStrongHint<T> hint,
            IReadOnlyDictionary<string, string> rawHints,
            T obj,
            IProvider provider = null)
            where T : IDefinition
        {
            rawHints.TryGetValue(hint.Key, out var rawValue);
            bool found = rawHints.ContainsKey(hint.Key);
            hint.Initialize(found, rawValue, obj, provider, hint.Key, rawHints);
            return hint;
        }

        [Test]
        public void DisplayName()
        {
            var prototype = new Hints.DisplayName();
            var obj = new TestObject("TestObject");

            // No hint — falls back to Name.
            var result = ResolveOne(prototype, Empty, obj);
            Assert.IsTrue(result.IsResolved);
            Assert.AreEqual("TestObject", result.Value);

            // Explicit hint value used.
            var withHint = new Dictionary<string, string> { { Common.kDisplayName, "Test Object DisplayName" } };
            result = ResolveOne(prototype, withHint, obj);
            Assert.IsTrue(result.IsResolved);
            Assert.AreEqual("Test Object DisplayName", result.Value);
        }

        [Test]
        public void TestRange()
        {
            var floatType = new ShaderType("float");
            var halfType = new ShaderType("half");
            var float2Type = new ShaderType("float2");

            // Fresh Range instance per call — Initialize is a one-shot operation and does not
            // reset IsResolved on error paths, so reusing a prototype would leak state from one
            // assertion to the next.
            IStrongHint<IShaderField> Resolve(ShaderField field, string rawValue)
            {
                var hints = rawValue != null
                    ? new Dictionary<string, string> { { Param.kRange, rawValue } }
                    : new Dictionary<string, string> { { Param.kRange, null } };
                return ResolveOne(new Hints.Range(), hints, field);
            }

            var floatField = new ShaderField("f", true, false, floatType, null);
            var halfField  = new ShaderField("h", true, false, halfType, null);
            var vec2Field  = new ShaderField("v", true, false, float2Type, null);

            // Valid cases.
            AssertRange(Resolve(floatField, null),      true,  0, 1,  false);
            AssertRange(Resolve(floatField, "-5"),      true,  -5, 0, false);
            AssertRange(Resolve(floatField, "5"),       true,  0, 5,  false);
            AssertRange(Resolve(floatField, "-5, 5"),   true,  -5, 5, false);
            AssertRange(Resolve(floatField, "5, -5"),   true,  -5, 5, false);
            AssertRange(Resolve(halfField,  null),      true,  0, 1,  false);

            // Invalid cases.
            AssertRange(Resolve(floatField, "5, -10, 10, -5"), false, 0, 0, true);
            AssertRange(Resolve(vec2Field,  "-5, 5"),           false, 0, 0, true);
            AssertRange(Resolve(floatField, "blah blah blah"),  false, 0, 1, true);
        }

        static void AssertRange(IStrongHint<IShaderField> hint, bool expectResolved, float min, float max, bool expectMessage)
        {
            Assert.AreEqual(expectResolved, hint.IsResolved);
            Assert.AreEqual(expectMessage, !string.IsNullOrEmpty(hint.Message));
            if (expectResolved)
            {
                var range = (float[])hint.Value;
                Assert.AreEqual(min, range[0]);
                Assert.AreEqual(max, range[1]);
            }
        }

        [Test]
        public void TestCategory()
        {
            var prototype = new Hints.SearchCategory();
            var floatType = new ShaderType("float");

            // Explicit category string used directly.
            var withCategory = new Dictionary<string, string> { { Func.kSearchCategory, "This/Is/A/Category" } };
            var func = new ShaderFunction("base", null, null, new ShaderType("void"), null, null);
            var result = ResolveOne(prototype, withCategory, func);
            Assert.IsTrue(result.IsResolved);
            Assert.AreEqual("This/Is/A/Category", result.Value);

            // Namespace used as fallback when no category hint.
            var namespaced = new ShaderFunction("ns", new[] { "NamespaceA", "NamespaceB" }, null, new ShaderType("void"), null, null);
            result = ResolveOne(prototype, Empty, namespaced);
            Assert.IsTrue(result.IsResolved);
            Assert.AreEqual("Reflected by Namespace/NamespaceA/NamespaceB", result.Value);

            // No namespace or path — uncategorized.
            var bare = new ShaderFunction("nocategory", null, null, new ShaderType("void"), null, null);
            result = ResolveOne(prototype, Empty, bare);
            Assert.IsTrue(result.IsResolved);
            Assert.AreEqual("Uncategorized", result.Value);
        }

        // ── Version ──────────────────────────────────────────────────────────────

        [Test]
        public void Version_ResolvedWithValue()
        {
            var func = new ShaderFunction("F", null, null, new ShaderType("void"), null, null);
            var result = ResolveOne(new Hints.Version(),
                new Dictionary<string, string> { { Func.kVersion, "1.2.3" } }, func);
            Assert.IsTrue(result.IsResolved);
            Assert.IsNull(result.Message);
            Assert.AreEqual("1.2.3", result.Value);
        }

        [Test]
        public void Version_NotFound_NotResolved()
        {
            var func = new ShaderFunction("F", null, null, new ShaderType("void"), null, null);
            var result = ResolveOne(new Hints.Version(), Empty, func);
            Assert.IsFalse(result.IsResolved);
        }

        [Test]
        public void Version_EmptyValue_ProducesMessage()
        {
            var func = new ShaderFunction("F", null, null, new ShaderType("void"), null, null);
            var result = ResolveOne(new Hints.Version(),
                new Dictionary<string, string> { { Func.kVersion, "" } }, func);
            Assert.IsFalse(result.IsResolved);
            Assert.IsNotEmpty(result.Message);
        }

        // ── Deprecated ───────────────────────────────────────────────────────────

        [Test]
        public void Deprecated_ResolvedWithMessage()
        {
            var func = new ShaderFunction("F", null, null, new ShaderType("void"), null, null);
            var result = ResolveOne(new Hints.Deprecated(),
                new Dictionary<string, string> { { Func.kDeprecated, "Use NewFunc() instead." } }, func);
            Assert.IsTrue(result.IsResolved);
            Assert.AreEqual("Use NewFunc() instead.", result.Value);
            // Resolved Deprecated/Obsolete hints attach a user-facing diagnostic that wraps
            // the raw value with the function name.
            Assert.IsNotEmpty(result.Message);
            StringAssert.Contains("Use NewFunc() instead.", result.Message);
        }

        [Test]
        public void Deprecated_BareFlag_ResolvedWithNullValue()
        {
            var func = new ShaderFunction("F", null, null, new ShaderType("void"), null, null);
            var result = ResolveOne(new Hints.Deprecated(),
                new Dictionary<string, string> { { Func.kDeprecated, null } }, func);
            Assert.IsTrue(result.IsResolved);
            Assert.IsNull(result.Value);
        }

        [Test]
        public void Deprecated_NotFound_NotResolved()
        {
            var func = new ShaderFunction("F", null, null, new ShaderType("void"), null, null);
            var result = ResolveOne(new Hints.Deprecated(), Empty, func);
            Assert.IsFalse(result.IsResolved);
        }

        // ── Obsolete ─────────────────────────────────────────────────────────────

        [Test]
        public void Obsolete_ResolvedWithMessage()
        {
            var func = new ShaderFunction("F", null, null, new ShaderType("void"), null, null);
            var result = ResolveOne(new Hints.Obsolete(),
                new Dictionary<string, string> { { Func.kObsolete, "Will be removed in 2.0." } }, func);
            Assert.IsTrue(result.IsResolved);
            Assert.AreEqual("Will be removed in 2.0.", result.Value);
            Assert.IsNotEmpty(result.Message);
            StringAssert.Contains("Will be removed in 2.0.", result.Message);
        }

        [Test]
        public void Obsolete_NotFound_NotResolved()
        {
            var func = new ShaderFunction("F", null, null, new ShaderType("void"), null, null);
            var result = ResolveOne(new Hints.Obsolete(), Empty, func);
            Assert.IsFalse(result.IsResolved);
        }

        // ── Deprecated + Obsolete conflict ───────────────────────────────────────

        [Test]
        public void Deprecated_And_Obsolete_SameConflictClass()
        {
            CollectionAssert.Contains(new Hints.Deprecated().Conflicts, Func.kLifecycleConflictClass);
            CollectionAssert.Contains(new Hints.Obsolete().Conflicts, Func.kLifecycleConflictClass);
        }

        readonly string kAllHintsPath = "Assets/Testing/IntegrationTests/Graphs/ProviderSystem/AllHints.hlsl";
        readonly string kAllHintsGraphPath = "Assets/Testing/IntegrationTests/Graphs/ProviderSystem/ShouldCompileProperlyOnImport.shadergraph";

        [Test]
        [UnityPlatform(exclude = new RuntimePlatform[] { RuntimePlatform.WindowsEditor })] // Unstable: https://jira.unity3d.com/browse/UUM-141869
        public void CheckAllHints()
        {
            AssetDatabase.ImportAsset(kAllHintsPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(kAllHintsGraphPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        [Test]
        public void LegacyPrecisionAndDynamics()
        {
            var DynamicHint  = new IStrongHint[] { new Hints.Dynamic(true) };
            var PrecisionHint = new IStrongHint[] { new Hints.Precision(true) };
            var generated = new List<string> { "unity_sg_generated" };

            ShaderType f  = new("float");  ShaderType h  = new("half");
            ShaderType f2 = new("float2"); ShaderType h2 = new("half2");
            ShaderType h3 = new("half3");  ShaderType h4 = new("half4");

            ShaderFunction testFunc = new("TestFunc", null, new IShaderField[]
            {
                new ShaderField("A", true, true, f,  DynamicHint),
                new ShaderField("B", true, true, h3, DynamicHint),
                new ShaderField("C", true, true, h2, null),
                new ShaderField("D", true, true, f2, null),
            }, f, "", PrecisionHint);

            var provider = new TestProvider("TestProvider", testFunc);
            var funcHeader = new FunctionHeader();
            var paramHeaders = new Dictionary<string, ParameterHeader>();

            funcHeader.Process(provider.Definition, provider);
            foreach (var param in provider.Definition.Parameters)
                paramHeaders.Add(param.Name, new ParameterHeader(param, provider));

            ShaderFunction expected = new("TestFunc_half4", generated, new IShaderField[]
            {
                new ShaderField("A", true, true, new ShaderType("half4"), DynamicHint),
                new ShaderField("B", true, true, new ShaderType("half4"), DynamicHint),
                new ShaderField("C", true, true, h2, null),
                new ShaderField("D", true, true, h2, null),
            }, h, "", PrecisionHint);

            HeaderUtils.TryApplyLegacy(provider.Definition, funcHeader, paramHeaders, "half", 4, out var result);
            Assert.IsTrue(TestUtils.CompareFunction(expected, result));

            expected = new("TestFunc_float1", generated, new IShaderField[]
            {
                new ShaderField("A", true, true, f,  DynamicHint),
                new ShaderField("B", true, true, f,  DynamicHint),
                new ShaderField("C", true, true, f2, null),
                new ShaderField("D", true, true, f2, null),
            }, f, "", PrecisionHint);

            HeaderUtils.TryApplyLegacy(provider.Definition, funcHeader, paramHeaders, "float", 1, out result);
            Assert.IsTrue(TestUtils.CompareFunction(expected, result));
        }
    }

    [TestFixture]
    class HelpURLHintTests
    {
        static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();

        static IStrongHint<IShaderFunction> ResolveOne(string rawValue)
        {
            var hint = new Hints.HelpURL();
            var raw = new Dictionary<string, string> { { Func.kDocumentationLink, rawValue } };
            raw.TryGetValue(hint.Key, out var value);
            hint.Initialize(true, value, new ShaderFunction("F", null, null, new ShaderType("void"), null, null), null, hint.Key, raw);
            return hint;
        }

        // ── Not found ─────────────────────────────────────────────────────────

        [Test]
        public void HelpURL_NotFound_NotResolved()
        {
            var hint = new Hints.HelpURL();
            hint.Initialize(false, null, new ShaderFunction("F", null, null, new ShaderType("void"), null, null), null, hint.Key, Empty);
            Assert.IsFalse(hint.IsResolved);
            Assert.IsNull(hint.Message);
        }

        [Test]
        public void HelpURL_EmptyValue_ProducesMessage()
        {
            var result = ResolveOne("");
            Assert.IsFalse(result.IsResolved);
            Assert.IsNotEmpty(result.Message);
        }

        // ── Raw URL ───────────────────────────────────────────────────────────

        [Test]
        public void HelpURL_RawUrl_PassedThrough()
        {
            var result = ResolveOne("https://example.com/my-docs");
            Assert.IsTrue(result.IsResolved);
            Assert.IsNull(result.Message);
            Assert.AreEqual("https://example.com/my-docs", result.Value);
        }

        [Test]
        public void HelpURL_RawUrl_HttpScheme_PassedThrough()
        {
            var result = ResolveOne("http://example.com/page");
            Assert.IsTrue(result.IsResolved);
            Assert.AreEqual("http://example.com/page", result.Value);
        }

        [Test]
        public void HelpURL_RawUrl_Trimmed()
        {
            var result = ResolveOne("  https://example.com/page  ");
            Assert.IsTrue(result.IsResolved);
            Assert.AreEqual("https://example.com/page", result.Value);
        }

        // ── Page + package ────────────────────────────────────────────────────
        // Token order is (page, package). The resolver looks the package up via
        // PackageManager.PackageInfo and embeds the package's actual version (x.y form)
        // in the URL. We use com.unity.shadergraph as the package since these tests run
        // inside its asmdef and it is guaranteed to be installed; the URL's version
        // segment is matched by shape rather than literal value.

        [Test]
        public void HelpURL_PageAndPackage_BuildsPackageDocsUrl()
        {
            var result = ResolveOne("MyPage, com.unity.shadergraph");
            Assert.IsTrue(result.IsResolved);
            Assert.IsNull(result.Message);
            StringAssert.StartsWith("https://docs.unity3d.com/Packages/com.unity.shadergraph@", (string)result.Value);
            StringAssert.EndsWith("/manual/MyPage.html", (string)result.Value);
        }

        [Test]
        public void HelpURL_PageAndPackage_HandlesWhitespace()
        {
            var result = ResolveOne("  SomePage  ,  com.unity.shadergraph  ");
            Assert.IsTrue(result.IsResolved);
            StringAssert.StartsWith("https://docs.unity3d.com/Packages/com.unity.shadergraph@", (string)result.Value);
            StringAssert.EndsWith("/manual/SomePage.html", (string)result.Value);
        }

        [Test]
        public void HelpURL_UnknownPackage_ProducesMessage()
        {
            var result = ResolveOne("MyPage, com.unity.does-not-exist");
            Assert.IsFalse(result.IsResolved);
            Assert.IsNotEmpty(result.Message);
        }
    }

}
