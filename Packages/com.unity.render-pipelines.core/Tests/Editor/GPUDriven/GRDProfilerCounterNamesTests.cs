#if !UNITY_WEBGL_RENDERER_ONLY
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.Assemblies;

namespace UnityEngine.Rendering.Tests
{
    // Drift detection for the wire-format string mirror between:
    //   - GRDProfilerCounters (this package, runtime — emits counters by string name)
    //   - GRDCounterNames     (Editor module ProfilerEditor, internal — reads counters by string name)
    //
    // The two files MUST agree on every counter name and category, otherwise the
    // Rendering Profiler module silently shows blank values instead of GRD stats.
    // ProfilerEditor is a built-in Editor module that lives in a different assembly
    // and is not directly referenceable from this package; we resolve its types via
    // reflection so this test stays in the contract-owner's package.
    internal class GRDProfilerCounterNamesTests
    {
        const string k_EditorMirrorTypeName = "UnityEditorInternal.Profiling.GRDCounterNames";

        static Type EditorMirrorType
        {
            get
            {
                // CurrentAssemblies.GetLoadedAssemblies() over AppDomain.CurrentDomain.GetAssemblies()
                // because Unity uses AssemblyLoadContext for code reload — the AppDomain list can
                // include unloaded assemblies and trip the UAC0005 analyzer.
                foreach (var asm in CurrentAssemblies.GetLoadedAssemblies())
                {
                    var t = asm.GetType(k_EditorMirrorTypeName, throwOnError: false);
                    if (t != null) return t;
                }
                return null;
            }
        }

        static T GetMirrorField<T>(string fieldName) where T : class
        {
            var t = EditorMirrorType;
            Assert.IsNotNull(t, $"Could not resolve {k_EditorMirrorTypeName}. " +
                "If the editor-side mirror moved, update this test.");
            var f = t.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(f, $"{k_EditorMirrorTypeName}.{fieldName} not found.");
            var v = f.GetValue(null) as T;
            Assert.IsNotNull(v, $"{k_EditorMirrorTypeName}.{fieldName} is null or wrong type.");
            return v;
        }

        static string GetMirrorConst(string fieldName)
        {
            var t = EditorMirrorType;
            Assert.IsNotNull(t);
            var f = t.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(f, $"{k_EditorMirrorTypeName}.{fieldName} not found.");
            return (string)f.GetRawConstantValue();
        }

        [Test]
        public void EditorMirrorIsReachable()
        {
            // If this fails, the editor module containing GRDCounterNames was not loaded into
            // the test domain, OR the namespace/class moved. The remaining tests would be
            // vacuously skipped without this guard.
            Assert.IsNotNull(EditorMirrorType,
                $"{k_EditorMirrorTypeName} not found in any loaded assembly.");
        }

        [Test]
        public void CategoryNameMatches()
        {
            string editor = GetMirrorConst("k_CategoryName");
            Assert.AreEqual(GRDProfilerCounters.CategoryNameForValidation, editor,
                "Profiler category name diverged. Both sides must use the same string.");
        }

        // Each non-key counter declared in GRDProfilerCounters must also be present somewhere
        // in the editor mirror — as either a const string OR an entry in a static readonly
        // string[] (per-reason counter names live in arrays). The matching is by VALUE (the
        // wire string), not by C# field name — runtime/editor are free to use different
        // identifiers.
        [Test]
        public void EveryRuntimeCounterIsMirrored()
        {
            var runtimeNames = GRDProfilerCounters.GetCounterNamesForValidation();
            var editorStrings = CollectAllMirrorStrings(EditorMirrorType);

            var missing = runtimeNames.Where(n => !editorStrings.Contains(n)).ToArray();
            CollectionAssert.IsEmpty(missing,
                "Runtime counters not mirrored in editor side: " + string.Join(", ", missing));
        }

        // Per-reason counter array must be index-aligned with GRDExclusionReason (None at 0).
        [Test]
        public void ExclusionReasonCounterArrayMatches()
        {
            var editorArr = GetMirrorField<string[]>("k_ExclusionReasonCounterNames");
            Assert.AreEqual(GRDProfilerCounters.k_ExclusionReasonCounterNames.Length, editorArr.Length,
                "Mirror array length mismatch — GRDExclusionReason enum likely grew or shrank.");

            for (int i = 0; i < editorArr.Length; i++)
            {
                Assert.AreEqual(GRDProfilerCounters.k_ExclusionReasonCounterNames[i], editorArr[i],
                    $"Exclusion reason name mismatch at index {i} (enum value {(GRDExclusionReason)i}).");
            }
        }

        // The three category arrays must partition the non-null exclusion reasons (each
        // reason in exactly one category) AND must agree with the runtime's GetCategory mapping.
        [Test]
        public void CategoryArraysPartitionAllReasonsAndAgreeWithRuntime()
        {
            var excluded     = GetMirrorField<string[]>("k_ExcludedCategoryReasonNames");
            var nonRendering = GetMirrorField<string[]>("k_NonRenderingCategoryReasonNames");
            var inactive     = GetMirrorField<string[]>("k_InactiveCategoryReasonNames");

            var byName = new Dictionary<string, GRDExclusionCategory>();
            void Add(string[] arr, GRDExclusionCategory cat)
            {
                foreach (var n in arr)
                {
                    Assert.IsFalse(byName.ContainsKey(n),
                        $"Reason '{n}' appears in more than one category array.");
                    byName[n] = cat;
                }
            }

            Add(excluded, GRDExclusionCategory.Excluded);
            Add(nonRendering, GRDExclusionCategory.NonRendering);
            Add(inactive, GRDExclusionCategory.Inactive);

            // Every non-None reason in the runtime enum must appear in exactly one category,
            // AND that category must match what GetCategory() returns.
            for (int i = 1; i < (int)GRDExclusionReason.Count; i++)
            {
                var reason = (GRDExclusionReason)i;
                var name = GRDProfilerCounters.k_ExclusionReasonCounterNames[i];
                Assert.IsNotNull(name, $"Runtime reason {reason} has no counter name.");
                Assert.IsTrue(byName.TryGetValue(name, out var editorCat),
                    $"Reason '{name}' is missing from all editor category arrays.");
                Assert.AreEqual(reason.GetCategory(), editorCat,
                    $"Category mismatch for {reason} ('{name}'): runtime says {reason.GetCategory()}, editor says {editorCat}.");
            }
        }

        // Walks all static fields of `t` and collects every non-null wire-format string —
        // both `const string` literals and entries of `static readonly string[]` arrays.
        // We need both forms because the editor mirror keeps top-level counters as consts
        // but per-reason counter names in arrays.
        static HashSet<string> CollectAllMirrorStrings(Type t)
        {
            var set = new HashSet<string>();
            foreach (var f in t.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (f.FieldType == typeof(string))
                {
                    string v = f.IsLiteral
                        ? (string)f.GetRawConstantValue()
                        : (string)f.GetValue(null);
                    if (v != null) set.Add(v);
                }
                else if (f.FieldType == typeof(string[]))
                {
                    var arr = (string[])f.GetValue(null);
                    if (arr == null) continue;
                    foreach (var s in arr)
                        if (s != null) set.Add(s);
                }
            }
            return set;
        }

        // GRDExclusionReasonText is the editor-side single source of reason label/tooltip text, index-aligned to GRDExclusionReason.
        // It lives in the UnityEditor assembly (not this package), so we reach it by reflection — same approach as the GRDCounterNames mirror.
        const string k_ReasonTextTypeName = "UnityEditor.Rendering.GRDExclusionReasonText";

        static Type ResolveEditorType(string fullName)
        {
            foreach (var asm in CurrentAssemblies.GetLoadedAssemblies())
            {
                var t = asm.GetType(fullName, throwOnError: false);
                if (t != null)
                {
                    return t;
                }
            }
            return null;
        }

        static string[] GetStaticStringArray(Type t, string fieldName)
        {
            var field = t.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(field, $"{t.FullName}.{fieldName} not found.");

            var value = field.GetValue(null) as string[];
            Assert.IsNotNull(value, $"{t.FullName}.{fieldName} is null or not string[].");

            return value;
        }

        // The reason text arrays must stay index-aligned with GRDExclusionReason (None at 0) and must have a non-empty label + tooltip for every real reason.
        // If the enum grows, this fails until GRDExclusionReasonText gets the matching entry.
        [Test]
        public void ExclusionReasonTextIsAlignedWithEnum()
        {
            var textType = ResolveEditorType(k_ReasonTextTypeName);
            Assert.IsNotNull(textType, $"{k_ReasonTextTypeName} not found in any loaded assembly. If it moved, update this test.");

            var labels = GetStaticStringArray(textType, "k_Labels");
            var tooltips = GetStaticStringArray(textType, "k_Tooltips");

            int count = (int)GRDExclusionReason.Count;
            Assert.AreEqual(count, labels.Length, "GRDExclusionReasonText.k_Labels length must equal GRDExclusionReason.Count.");
            Assert.AreEqual(count, tooltips.Length, "GRDExclusionReasonText.k_Tooltips length must equal GRDExclusionReason.Count.");

            Assert.IsNull(labels[0], "Index 0 (None) label must be null.");
            Assert.IsNull(tooltips[0], "Index 0 (None) tooltip must be null.");

            for (int i = 1; i < count; i++)
            {
                Assert.IsFalse(string.IsNullOrEmpty(labels[i]), $"Missing label for {(GRDExclusionReason)i} (index {i}).");
                Assert.IsFalse(string.IsNullOrEmpty(tooltips[i]), $"Missing tooltip for {(GRDExclusionReason)i} (index {i}).");
            }
        }
    }
}
#endif
