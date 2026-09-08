using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
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
            // URP 2D-only globals, empty so disabled.
            // typeof(GlobalShaderVariablesOnly2D),
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

        // Mirror of k_AllowedFieldTypes, used to compare the C# layout against the hlsl declaration.
        static readonly Dictionary<Type, string> k_HlslTypeNames = new Dictionary<Type, string>
        {
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(float), "float" },
            { typeof(Vector2), "float2" },
            { typeof(Vector2Int), "int2" },
            { typeof(Vector4), "float4" },
            { typeof(Matrix4x4), "float4x4" },
        };

        // Matches any "<type> <name>;" so an unregistered type fails loudly below instead of being skipped,
        // which would hide the drift this test exists to catch.
        // The trailing [..] is optional so array members are captured too, like _MainLightWorldToShadow[MAX_SHADOW_CASCADES + 1].
        static readonly Regex k_HlslDeclaration = new Regex(@"^\s*([A-Za-z_]\w*)\s+(\w+)\s*(?:\[([^\]]+)\])?\s*;");

        // Integer defines of that same file, the only thing an array length is allowed to reference.
        static readonly Regex k_HlslDefine = new Regex(@"^#define\s+(\w+)\s+(\d+)\s*$");

        // The shader define selecting the persistent constant buffer, so the layout the hlsl is read for.
        const string k_PersistentConstantBuffersDefine = "URP_GLOBAL_CONSTANT_BUFFER";

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
        // URP 2D-only globals, empty so disabled.
        // [TestCase(typeof(GlobalShaderVariablesOnly2D), typeof(GlobalShaderVariablesOnly2DDirty))]
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
        // URP 2D-only globals, empty so disabled.
        // [TestCase(typeof(GlobalShaderVariablesOnly2DDirty))]
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
        // URP 2D-only globals, empty so disabled.
        // [TestCase(typeof(GlobalShaderVariablesOnly2DDirty))]
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

        // The hlsl and these structs are two hand-maintained copies of one cbuffer layout. A rename or reorder in
        // either compiles cleanly on both sides and silently feeds shaders the wrong register
        [Test]
        public void HlslDeclarationMatchesLayoutStructs()
        {
            // Sorted by offset rather than trusting reflection's declaration order, which is not guaranteed.
            var fields = new List<(int offset, string declaration)>();
            ForEachHlslDeclaration(typeof(GlobalShaderVariables), 0, (declaration, offset) =>
                fields.Add((offset, declaration)));
            fields.Sort((a, b) => a.offset.CompareTo(b.offset));

            var expected = new List<string>();
            foreach (var field in fields)
                expected.Add(field.declaration);

            Assert.That(ParseHlslDeclarations(), Is.EqualTo(expected),
                "GlobalShaderVariables.hlsl and the GlobalShaderVariables structs have drifted apart. They are two " +
                "copies of the same cbuffer layout and must match in name, type and order.");
        }

        static string HlslTypeFor(Type type)
        {
            Assert.That(k_HlslTypeNames.ContainsKey(type), Is.True,
                $"No HLSL equivalent registered for {type.Name}. Add one to k_HlslTypeNames (and to k_AllowedFieldTypes).");

            return k_HlslTypeNames[type];
        }

        // Pulls the "float4 _Time;" declarations out of the hlsl, dropping comments, preprocessor lines and the
        // CBUFFER_START/CBUFFER_END macros so only the field list is compared.
        static List<string> ParseHlslDeclarations()
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(GlobalShaderVariables).Assembly);
            Assert.That(package, Is.Not.Null, "Could not resolve the URP package from its runtime assembly.");

            var path = Path.Combine(package.resolvedPath, "ShaderLibrary", "GlobalShaderVariables.hlsl");
            Assert.That(File.Exists(path), Is.True, $"Could not find {path}.");

            var declarations = new List<string>();
            var defines = new Dictionary<string, int>();

            // A member can be declared once per branch of a conditional, when only its precision changes. The layout
            // read here is the one the persistent constant buffer mode compiles, so the branch taken is the one that
            // mode selects. Conditions are tracked as a stack because of the include guard wrapping the whole file.
            var conditions = new List<string>();
            bool skipping = false;

            foreach (var rawLine in File.ReadAllLines(path))
            {
                var line = rawLine;

                int comment = line.IndexOf("//", StringComparison.Ordinal);
                if (comment >= 0)
                    line = line.Substring(0, comment);

                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("#", StringComparison.Ordinal))
                {
                    // Kept so an array length can name one, they are declared before the members that use them.
                    var define = k_HlslDefine.Match(trimmed.TrimEnd());
                    if (define.Success)
                        defines[define.Groups[1].Value] = int.Parse(define.Groups[2].Value);

                    if (trimmed.StartsWith("#if", StringComparison.Ordinal))
                    {
                        conditions.Add(trimmed);
                    }
                    else if (trimmed.StartsWith("#elif", StringComparison.Ordinal))
                    {
                        Assert.Fail("GlobalShaderVariables.hlsl uses #elif, which this parser cannot attribute to a " +
                            "branch. Declare the layout under a plain #if on URP_GLOBAL_CONSTANT_BUFFER instead.");
                    }
                    else if (trimmed.StartsWith("#else", StringComparison.Ordinal))
                    {
                        // Only the persistent constant buffer condition has a known answer here, so anything else
                        // would leave the layout ambiguous instead of silently picking one of the two branches.
                        Assert.That(conditions.Count, Is.GreaterThan(0), "GlobalShaderVariables.hlsl has an #else without a matching #if.");
                        Assert.That(conditions[conditions.Count - 1], Does.Contain(k_PersistentConstantBuffersDefine),
                            $"GlobalShaderVariables.hlsl declares members under '{conditions[conditions.Count - 1]}', whose #else branch " +
                            $"cannot be resolved. Only a conditional on {k_PersistentConstantBuffersDefine} may hold two versions of a member.");

                        skipping = true;
                    }
                    else if (trimmed.StartsWith("#endif", StringComparison.Ordinal))
                    {
                        if (conditions.Count > 0)
                            conditions.RemoveAt(conditions.Count - 1);

                        skipping = false;
                    }

                    continue;
                }

                if (skipping)
                    continue;

                var match = k_HlslDeclaration.Match(line);
                if (!match.Success)
                    continue;

                var hlslType = match.Groups[1].Value;
                var hlslName = match.Groups[2].Value;
                var arrayLength = match.Groups[3];

                Assert.That(k_HlslTypeNames.ContainsValue(hlslType), Is.True,
                    $"GlobalShaderVariables.hlsl declares '{hlslType} {hlslName};' but '{hlslType}' has no entry in " +
                    $"k_HlslTypeNames. Register it there and in k_AllowedFieldTypes.");

                declarations.Add(arrayLength.Success
                    ? $"{hlslType} {hlslName}[{ResolveArrayLength(arrayLength.Value, defines, hlslName)}]"
                    : $"{hlslType} {hlslName}");
            }

            return declarations;
        }

        // Turns a length like "MAX_SHADOW_CASCADES + 1" into a number, so a drift between the hlsl array and the C#
        // struct standing for it fails the comparison instead of going unnoticed.
        static int ResolveArrayLength(string expression, Dictionary<string, int> defines, string memberName)
        {
            int total = 0;

            foreach (var rawTerm in expression.Split('+'))
            {
                var term = rawTerm.Trim();
                if (int.TryParse(term, out int literal))
                {
                    total += literal;
                    continue;
                }

                Assert.That(defines.ContainsKey(term), Is.True,
                    $"GlobalShaderVariables.hlsl sizes '{memberName}' with '{term}', which is not an integer #define " +
                    "of that file. Array lengths there must be literals or such defines, added together.");

                total += defines[term];
            }

            return total;
        }

        // Same walk as ForEachLeafField, except it stops where the hlsl stops. The group structs (varsBase, vars3D)
        // are pure containers whose fields sit inline in the buffer, so they are recursed into. Any other
        // nested struct groups the elements of one hlsl array, so it maps to a single "float4x4 name[5]" declaration
        // rather than to its leaves, and the element count is part of the comparison.
        static void ForEachHlslDeclaration(Type layoutType, int baseOffset, Action<string, int> visit)
        {
            foreach (var field in layoutType.GetFields(k_FieldFlags))
            {
                int offset = baseOffset + Marshal.OffsetOf(layoutType, field.Name).ToInt32();
                Type type = field.FieldType;

                if (Array.IndexOf(k_LayoutTypes, type) >= 0)
                {
                    ForEachHlslDeclaration(type, offset, visit);
                    continue;
                }

                if (!IsNestedLayoutStruct(type))
                {
                    visit($"{HlslTypeFor(type)} {field.Name}", offset);
                    continue;
                }

                FieldInfo[] elements = type.GetFields(k_FieldFlags);
                Assert.That(elements.Length, Is.GreaterThan(0),
                    $"{type.Name} has no field, so it cannot map to an hlsl array.");

                Type elementType = elements[0].FieldType;
                foreach (var element in elements)
                    Assert.That(element.FieldType, Is.EqualTo(elementType),
                        $"{type.Name}.{element.Name} is a {element.FieldType.Name} while {type.Name}.{elements[0].Name} " +
                        $"is a {elementType.Name}. A struct standing for an hlsl array must hold one type only.");

                visit($"{HlslTypeFor(elementType)} {field.Name}[{elements.Length}]", offset);
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