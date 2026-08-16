using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// GlobalShaderVariables and its sub-structs mirror an HLSL constant buffer layout, so they must obey the packing
// rules documented in GlobalShaderVariables.cs. A field violating them still compiles on C# side but silently
// reads garbage on at least one graphics backend, so these tests enforce the rules instead.
namespace UnityEditor.Rendering.Universal.Tests
{
    class GlobalShaderVariablesLayoutTests
    {
        // A cbuffer is addressed as an array of float4 registers.
        const int k_RegisterSize = 16;

        static readonly BindingFlags k_FieldFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        static readonly Type[] k_LayoutTypes =
        {
            typeof(GlobalShaderVariables),
            typeof(GlobalShaderVariablesBase),
            typeof(GlobalShaderVariablesOnly2D),
            typeof(GlobalShaderVariablesOnly3D),
        };

        // Types packing identically in C# and across every graphics backend we support.
        // Notably absent: bool/char/enum (1 byte in C#, 4 in HLSL), double/half, and Vector3/Vector3Int
        // (float3 size and alignment differ between Metal, Vulkan std140 and HLSL).
        static readonly HashSet<Type> k_AllowedFieldTypes = new HashSet<Type>
        {
            typeof(int), typeof(uint), typeof(float),
            typeof(Vector2), typeof(Vector2Int),
            typeof(Vector4),
            typeof(Matrix4x4),
        };

        [Test]
        public void FieldTypesAreConstantBufferSafe([ValueSource(nameof(k_LayoutTypes))] Type layoutType)
        {
            ForEachLeafField(layoutType, 0, (field, offset) =>
                Assert.That(k_AllowedFieldTypes, Does.Contain(field.FieldType),
                    $"{field.DeclaringType.Name}.{field.Name} is a {field.FieldType.Name}, which does not pack " +
                    "consistently across graphics backends. See the declaration guidelines in GlobalShaderVariables.cs."));
        }

        [Test]
        public void SizeIsMultipleOfRegisterSize([ValueSource(nameof(k_LayoutTypes))] Type layoutType)
        {
            Assert.That(Marshal.SizeOf(layoutType) % k_RegisterSize, Is.Zero,
                $"{layoutType.Name} is {Marshal.SizeOf(layoutType)} bytes, which is not a multiple of {k_RegisterSize}. Add trailing padding.");
        }

        [Test]
        public void NoFieldStraddlesRegisterBoundary([ValueSource(nameof(k_LayoutTypes))] Type layoutType)
        {
            ForEachLeafField(layoutType, 0, (field, offset) =>
            {
                int size = Marshal.SizeOf(field.FieldType);

                if (size >= k_RegisterSize)
                {
                    Assert.That(offset % k_RegisterSize, Is.Zero,
                        $"{field.DeclaringType.Name}.{field.Name} is {size} bytes at offset {offset}, it must start on a {k_RegisterSize}-byte boundary.");
                }
                else
                {
                    // A field smaller than a register must fit entirely inside one.
                    Assert.That(offset % k_RegisterSize + size, Is.LessThanOrEqualTo(k_RegisterSize),
                        $"{field.DeclaringType.Name}.{field.Name} is {size} bytes at offset {offset}, it straddles a register boundary. Add padding before it.");
                }
            });
        }

        // A field with no bit would never be pushed after the first ResetToDefault, and a bit with no field is dead.
        // Neither shows up as a compile error, so the correspondence is checked here instead.
        [TestCase(typeof(GlobalShaderVariablesBase), typeof(GlobalShaderVariablesBaseDirty))]
        [TestCase(typeof(GlobalShaderVariablesOnly2D), typeof(GlobalShaderVariablesOnly2DDirty))]
        [TestCase(typeof(GlobalShaderVariablesOnly3D), typeof(GlobalShaderVariablesOnly3DDirty))]
        public void DirtyFlagsMatchFields(Type layoutType, Type dirtyType)
        {
            var fieldNames = new HashSet<string>();
            foreach (var field in layoutType.GetFields(k_FieldFlags))
            {
                // Explicit padding exists only to satisfy the register rules, it is never pushed.
                if (!field.Name.StartsWith("_URPPadding", StringComparison.Ordinal))
                    fieldNames.Add(field.Name);
            }

            var flagNames = new HashSet<string>();
            foreach (var name in Enum.GetNames(dirtyType))
            {
                if (name != "None" && name != "All")
                    flagNames.Add(name);
            }

            Assert.That(flagNames, Is.EquivalentTo(fieldNames),
                $"{dirtyType.Name} and {layoutType.Name} have drifted apart. Every pushable field needs exactly one " +
                "bit of the same name, and every bit needs a matching field.");
        }

        // All is written as an exact mask derived from the last declared bit, so adding a variable without moving it
        // would leave that variable out of every ResetToDefault push. Nothing else catches that.
        [TestCase(typeof(GlobalShaderVariablesBaseDirty))]
        [TestCase(typeof(GlobalShaderVariablesOnly2DDirty))]
        [TestCase(typeof(GlobalShaderVariablesOnly3DDirty))]
        public void AllCoversEveryFlag(Type dirtyType)
        {
            uint expected = 0u;

            foreach (var name in Enum.GetNames(dirtyType))
            {
                if (name != "None" && name != "All")
                    expected |= Convert.ToUInt32(Enum.Parse(dirtyType, name));
            }

            uint all = Convert.ToUInt32(Enum.Parse(dirtyType, "All"));

            Assert.That(all, Is.EqualTo(expected),
                $"{dirtyType.Name}.All must be the union of every declared bit. Point it at the last bit you added.");
        }

        // Two fields sharing a bit would push each other spuriously; checked separately from the name matching above.
        [TestCase(typeof(GlobalShaderVariablesBaseDirty))]
        [TestCase(typeof(GlobalShaderVariablesOnly2DDirty))]
        [TestCase(typeof(GlobalShaderVariablesOnly3DDirty))]
        public void DirtyFlagsAreDistinctPowersOfTwo(Type dirtyType)
        {
            var seen = new Dictionary<uint, string>();

            foreach (var name in Enum.GetNames(dirtyType))
            {
                if (name == "None" || name == "All")
                    continue;

                uint value = Convert.ToUInt32(Enum.Parse(dirtyType, name));

                Assert.That(value, Is.Not.Zero, $"{dirtyType.Name}.{name} must not be zero.");
                Assert.That(value & (value - 1), Is.Zero, $"{dirtyType.Name}.{name} must be a single bit.");
                Assert.That(seen.TryGetValue(value, out var other), Is.False,
                    $"{dirtyType.Name}.{name} shares its bit with {other}.");

                seen[value] = name;
            }
        }

        // Walks the fields of layoutType, recursing into our own nested layout structs so offsets stay absolute.
        static void ForEachLeafField(Type layoutType, int baseOffset, Action<FieldInfo, int> visit)
        {
            foreach (var field in layoutType.GetFields(k_FieldFlags))
            {
                int offset = baseOffset + Marshal.OffsetOf(layoutType, field.Name).ToInt32();

                if (IsNestedLayoutStruct(field.FieldType))
                    ForEachLeafField(field.FieldType, offset, visit);
                else
                    visit(field, offset);
            }
        }

        // Our layout structs live in the URP runtime namespace; Vector4/Matrix4x4 and friends live in UnityEngine.
        static bool IsNestedLayoutStruct(Type type)
            => type.IsValueType && !type.IsPrimitive && !type.IsEnum
               && type.Namespace == typeof(GlobalShaderVariables).Namespace;
    }
}