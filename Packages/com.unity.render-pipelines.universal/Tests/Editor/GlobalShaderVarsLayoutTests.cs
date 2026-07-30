using System.Reflection;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEngine.Rendering.Universal;

// GlobalShaderVars/GlobalShaderVars2D rely on their initial fields being byte-for-byte identical to
// BaseGlobalShaderVars: BaseGlobalShaderVarsExtensions.AsBase() reinterprets a leaf-struct reference as a
// BaseGlobalShaderVars reference via UnsafeUtility.As() for performance reason (avoid a costly copy).
// If one of the base fields is renamed, reordered, retyped, or removed in either leaf struct without mirroring the change in the base
// the reinterpret would silently write to the wrong offsets. These tests enforce that invariant.
namespace UnityEditor.Rendering.Universal.Tests
{
    class GlobalShaderVarsLayoutTests
    {
        static readonly BindingFlags k_FieldFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        [Test]
        public void GlobalShaderVars_PrefixMatchesBaseLayout()
        {
            AssertPrefixMatchesBaseLayout(typeof(GlobalShaderVars));
        }

        [Test]
        public void GlobalShaderVars2D_PrefixMatchesBaseLayout()
        {
            AssertPrefixMatchesBaseLayout(typeof(GlobalShaderVars2D));
        }

        static void AssertPrefixMatchesBaseLayout(System.Type leafType)
        {
            var baseType = typeof(BaseGlobalShaderVars);
            var baseFields = baseType.GetFields(k_FieldFlags);

            Assert.That(Marshal.SizeOf(leafType), Is.GreaterThanOrEqualTo(Marshal.SizeOf(baseType)),
                $"{leafType.Name} must be at least as large as {baseType.Name}.");

            foreach (var baseField in baseFields)
            {
                var leafField = leafType.GetField(baseField.Name, k_FieldFlags);
                Assert.That(leafField, Is.Not.Null,
                    $"{leafType.Name} is missing base field '{baseField.Name}'.");
                Assert.That(leafField.FieldType, Is.EqualTo(baseField.FieldType),
                    $"{leafType.Name}.{baseField.Name} type differs from {baseType.Name}.{baseField.Name}.");
                Assert.That(Marshal.OffsetOf(leafType, baseField.Name), Is.EqualTo(Marshal.OffsetOf(baseType, baseField.Name)),
                    $"{leafType.Name}.{baseField.Name} offset differs from {baseType.Name}.{baseField.Name}.");
            }
        }
    }
}
