using System;
using System.Globalization;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    class ShaderGeneratorTests
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            Debug.unityLogger.logHandler = new ConsoleLogHandler();
        }

        class TestNode : AbstractMaterialNode
        {
            public const int V1Out = 0;
            public const int V2Out = 1;
            public const int V3Out = 2;
            public const int V4Out = 3;

            public TestNode()
            {
                AddSlot(new Vector1MaterialSlot(V1Out, "V1Out", "V1Out", SlotType.Output, 0));
                AddSlot(new Vector2MaterialSlot(V2Out, "V2Out", "V2Out", SlotType.Output, Vector4.zero));
                AddSlot(new Vector3MaterialSlot(V3Out, "V3Out", "V3Out", SlotType.Output, Vector4.zero));
                AddSlot(new Vector4MaterialSlot(V4Out, "V4Out", "V4Out", SlotType.Output, Vector4.zero));
            }
        }

        [Test]
        public void AdaptNodeOutput1To1Works()
        {
            var node = new TestNode();
            var result = GenerationUtils.AdaptNodeOutput(node, TestNode.V1Out, ConcreteSlotValueType.Vector1);
            Assert.AreEqual(string.Format("{0}", node.GetVariableNameForSlot(TestNode.V1Out)), result);
        }

        [Test]
        public void AdaptNodeOutput1To2Works()
        {
            var node = new TestNode();
            var result = GenerationUtils.AdaptNodeOutput(node, TestNode.V1Out, ConcreteSlotValueType.Vector2);
            Assert.AreEqual(string.Format("({0}.xx)", node.GetVariableNameForSlot(TestNode.V1Out)), result);
        }

        [Test]
        public void AdaptNodeOutput1To3Works()
        {
            var node = new TestNode();
            var result = GenerationUtils.AdaptNodeOutput(node, TestNode.V1Out, ConcreteSlotValueType.Vector3);
            Assert.AreEqual(string.Format("({0}.xxx)", node.GetVariableNameForSlot(TestNode.V1Out)), result);
        }

        [Test]
        public void AdaptNodeOutput1To4Works()
        {
            var node = new TestNode();
            var result = GenerationUtils.AdaptNodeOutput(node, TestNode.V1Out, ConcreteSlotValueType.Vector4);
            Assert.AreEqual(string.Format("({0}.xxxx)", node.GetVariableNameForSlot(TestNode.V1Out)), result);
        }

        [Test]
        public void AdaptNodeOutput2To1Works()
        {
            var node = new TestNode();
            var result = GenerationUtils.AdaptNodeOutput(node, TestNode.V2Out, ConcreteSlotValueType.Vector1);
            Assert.AreEqual(string.Format("({0}).x", node.GetVariableNameForSlot(TestNode.V2Out)), result);
        }

        [Test]
        public void AdaptNodeOutput2To2Works()
        {
            var node = new TestNode();
            var result = GenerationUtils.AdaptNodeOutput(node, TestNode.V2Out, ConcreteSlotValueType.Vector2);
            Assert.AreEqual(string.Format("{0}", node.GetVariableNameForSlot(TestNode.V2Out)), result);
        }

        [Test]
        public void AdaptNodeOutput2To3Works()
        {
            var node = new TestNode();
            var result = GenerationUtils.AdaptNodeOutput(node, TestNode.V2Out, ConcreteSlotValueType.Vector3);
            Assert.AreEqual(string.Format("($precision3({0}, 0.0))", node.GetVariableNameForSlot(TestNode.V2Out)), result);
        }

        [Test]
        public void AdaptNodeOutput2To4Works()
        {
            var node = new TestNode();
            var result = GenerationUtils.AdaptNodeOutput(node, TestNode.V2Out, ConcreteSlotValueType.Vector4);
            Assert.AreEqual(string.Format("($precision4({0}, 0.0, 1.0))", node.GetVariableNameForSlot(TestNode.V2Out)), result);
        }

        [Test]
        public void AdaptNodeOutput3To1Works()
        {
            var node = new TestNode();
            var result = GenerationUtils.AdaptNodeOutput(node, TestNode.V3Out, ConcreteSlotValueType.Vector1);
            Assert.AreEqual(string.Format("({0}).x", node.GetVariableNameForSlot(TestNode.V3Out)), result);
        }

        [Test]
        public void AdaptNodeOutput3To2Works()
        {
            var node = new TestNode();
            var result = GenerationUtils.AdaptNodeOutput(node, TestNode.V3Out, ConcreteSlotValueType.Vector2);
            Assert.AreEqual(string.Format("({0}.xy)", node.GetVariableNameForSlot(TestNode.V3Out)), result);
        }

        [Test]
        public void AdaptNodeOutput3To3Works()
        {
            var node = new TestNode();
            var result = GenerationUtils.AdaptNodeOutput(node, TestNode.V3Out, ConcreteSlotValueType.Vector3);
            Assert.AreEqual(string.Format("{0}", node.GetVariableNameForSlot(TestNode.V3Out)), result);
        }

        [Test]
        public void AdaptNodeOutput3To4Fails()
        {
            var node = new TestNode();
            var result = GenerationUtils.AdaptNodeOutput(node, TestNode.V3Out, ConcreteSlotValueType.Vector4);
            Assert.AreEqual(string.Format("($precision4({0}, 1.0))", node.GetVariableNameForSlot(TestNode.V3Out)), result);
        }

        [Test]
        public void AdaptNodeOutput4To1Works()
        {
            var node = new TestNode();
            var result = GenerationUtils.AdaptNodeOutput(node, TestNode.V4Out, ConcreteSlotValueType.Vector1);
            Assert.AreEqual(string.Format("({0}).x", node.GetVariableNameForSlot(TestNode.V4Out)), result);
        }

        [Test]
        public void AdaptNodeOutput4To2Works()
        {
            var node = new TestNode();
            var result = GenerationUtils.AdaptNodeOutput(node, TestNode.V4Out, ConcreteSlotValueType.Vector2);
            Assert.AreEqual(string.Format("({0}.xy)", node.GetVariableNameForSlot(TestNode.V4Out)), result);
        }

        [Test]
        public void AdaptNodeOutput4To3Works()
        {
            var node = new TestNode();
            var result = GenerationUtils.AdaptNodeOutput(node, TestNode.V4Out, ConcreteSlotValueType.Vector3);
            Assert.AreEqual(string.Format("({0}.xyz)", node.GetVariableNameForSlot(TestNode.V4Out)), result);
        }

        [Test]
        public void AdaptNodeOutput4To4Works()
        {
            var node = new TestNode();
            var result = GenerationUtils.AdaptNodeOutput(node, TestNode.V4Out, ConcreteSlotValueType.Vector4);
            Assert.AreEqual(string.Format("{0}", node.GetVariableNameForSlot(TestNode.V4Out)), result);
        }

        [Test]
        public void AdaptNodeOutput1To4PreviewWorks()
        {
            var node = new TestNode();
            var result = GenerationUtils.AdaptNodeOutputForPreview(node, TestNode.V1Out);
            Assert.AreEqual(string.Format("half4({0}, {0}, {0}, 1.0)", node.GetVariableNameForSlot(TestNode.V1Out)), result);
        }

        [Test]
        public void AdaptNodeOutput2To4PreviewWorks()
        {
            var node = new TestNode();
            var expected = string.Format("half4({0}.x, {0}.y, 0.0, 1.0)", node.GetVariableNameForSlot(TestNode.V2Out));
            var result = GenerationUtils.AdaptNodeOutputForPreview(node, TestNode.V2Out);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void AdaptNodeOutput3To4PreviewWorks()
        {
            var node = new TestNode();
            var expected = string.Format("half4({0}.x, {0}.y, {0}.z, 1.0)", node.GetVariableNameForSlot(TestNode.V3Out));
            var result = GenerationUtils.AdaptNodeOutputForPreview(node, TestNode.V3Out);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void AdaptNodeOutput4To4PreviewWorks()
        {
            var node = new TestNode();
            var expected = string.Format("half4({0}.x, {0}.y, {0}.z, 1.0)", node.GetVariableNameForSlot(TestNode.V4Out));
            var result = GenerationUtils.AdaptNodeOutputForPreview(node, TestNode.V4Out);
            Assert.AreEqual(expected, result);
        }

        public struct PackingTestCase
        {
            internal string name;
            internal StructDescriptor inputStruct;
            internal StructDescriptor expectedOutputStruct;
            internal string expectedInterpolatorFunction;
            public override string ToString()
            {
                return name;
            }
        }

        public static readonly PackingTestCase[] s_PackingTestCase = new[]
        {
            new PackingTestCase()
            {
                name = "Simple_With_Semantic",
                inputStruct = new StructDescriptor()
                {
                    name = "Input",
                    packFields = false,
                    fields = new []
                    {   
                        new FieldDescriptor(tag: "Original", name: "position",       define: string.Empty, type: ShaderValueType.Float4, semantic: "SV_POSITION",  interpolation: "SV_POSITION_QUALIFIERS"),
                        new FieldDescriptor(tag: "Original", name: "normalWS",       define: string.Empty, type: ShaderValueType.Float3, semantic: "NORMAL_WS",    interpolation: string.Empty),
                        new FieldDescriptor(tag: "Original", name: "myFloatA",       define: string.Empty, type: ShaderValueType.Float,  semantic: string.Empty,   interpolation: string.Empty),
                    }
                },

                expectedOutputStruct = new StructDescriptor()
                {
                    name = "PackedInput",
                    packFields = true,
                    fields = new []
                    {
                        new FieldDescriptor(tag: "Original",    name: "position", define: string.Empty, type: ShaderValueType.Float4, semantic: "SV_POSITION", interpolation: "SV_POSITION_QUALIFIERS"),
                        new FieldDescriptor(tag: "Original",    name: "normalWS", define: string.Empty, type: ShaderValueType.Float3, semantic: "NORMAL_WS",   interpolation: string.Empty),
                        new FieldDescriptor(tag: "PackedInput", name: "myFloatA", define: string.Empty, type: "float1",               semantic: "INTERP0",     interpolation: string.Empty),
                    }
                },
                expectedInterpolatorFunction = @"
PackedInput PackInput (Input input)
{
    PackedInput output;
    ZERO_INITIALIZE(PackedInput, output);
    output.position = input.position;
    output.normalWS = input.normalWS;
    output.myFloatA.x = input.myFloatA;
    return output;
}

Input UnpackInput (PackedInput input)
{
    Input output;
    output.position = input.position;
    output.normalWS = input.normalWS;
    output.myFloatA = input.myFloatA.x;
    return output;
}"
            },

            new PackingTestCase()
            {
                name = "Equivalent_Packing_F3F3F1F1_A",
                inputStruct = new StructDescriptor()
                {
                    name = "Input",
                    packFields = false,
                    fields = new []
                    {
                        new FieldDescriptor(tag: "Original", name: "myVector3_A",  define: string.Empty, type: ShaderValueType.Float3, semantic: string.Empty,  interpolation: string.Empty),
                        new FieldDescriptor(tag: "Original", name: "myVector3_B",  define: string.Empty, type: ShaderValueType.Float3, semantic: string.Empty,  interpolation: string.Empty),
                        new FieldDescriptor(tag: "Original", name: "myFloat_A",    define: string.Empty, type: ShaderValueType.Float,  semantic: string.Empty, interpolation: string.Empty),
                        new FieldDescriptor(tag: "Original", name: "myFloat_B",    define: string.Empty, type: ShaderValueType.Float,  semantic: string.Empty, interpolation: string.Empty),
                    }
                },

                expectedOutputStruct = new StructDescriptor()
                {
                    name = "PackedInput",
                    packFields = true,
                    fields = new []
                    {
                        new FieldDescriptor(tag: "PackedInput", name: "packed_myVector3_A_myFloat_A", define: string.Empty, type: "float4", semantic: "INTERP0", interpolation: string.Empty),
                        new FieldDescriptor(tag: "PackedInput", name: "packed_myVector3_B_myFloat_B", define: string.Empty, type: "float4", semantic: "INTERP1", interpolation: string.Empty),
                    }
                },
                expectedInterpolatorFunction = @"
PackedInput PackInput (Input input)
{
    PackedInput output;
    ZERO_INITIALIZE(PackedInput, output);
    output.packed_myVector3_A_myFloat_A.xyz = input.myVector3_A;
    output.packed_myVector3_A_myFloat_A.w = input.myFloat_A;
    output.packed_myVector3_B_myFloat_B.xyz = input.myVector3_B;
    output.packed_myVector3_B_myFloat_B.w = input.myFloat_B;
    return output;
}

Input UnpackInput (PackedInput input)
{
    Input output;
    output.myVector3_A = input.packed_myVector3_A_myFloat_A.xyz;
    output.myFloat_A = input.packed_myVector3_A_myFloat_A.w;
    output.myVector3_B = input.packed_myVector3_B_myFloat_B.xyz;
    output.myFloat_B = input.packed_myVector3_B_myFloat_B.w;
    return output;
}"
            },

            new PackingTestCase()
            {
                name = "Equivalent_Packing_F3F3F1F1_B",
                inputStruct = new StructDescriptor()
                {
                    name = "Input",
                    packFields = false,
                    fields = new []
                    {
                        //This test insure the order of input packing doesn't fail the following packing
                        new FieldDescriptor(tag: "Original", name: "myFloat_A",    define: string.Empty, type: ShaderValueType.Float,  semantic: string.Empty, interpolation: string.Empty),
                        new FieldDescriptor(tag: "Original", name: "myFloat_B",    define: string.Empty, type: ShaderValueType.Float,  semantic: string.Empty, interpolation: string.Empty),
                        new FieldDescriptor(tag: "Original", name: "myVector3_A",  define: string.Empty, type: ShaderValueType.Float3, semantic: string.Empty,  interpolation: string.Empty),
                        new FieldDescriptor(tag: "Original", name: "myVector3_B",  define: string.Empty, type: ShaderValueType.Float3, semantic: string.Empty,  interpolation: string.Empty),
                    }
                },

                expectedOutputStruct = new StructDescriptor()
                {
                    name = "PackedInput",
                    packFields = true,
                    fields = new []
                    {
                        new FieldDescriptor(tag: "PackedInput", name: "packed_myVector3_A_myFloat_A", define: string.Empty, type: "float4", semantic: "INTERP0", interpolation: string.Empty),
                        new FieldDescriptor(tag: "PackedInput", name: "packed_myVector3_B_myFloat_B", define: string.Empty, type: "float4", semantic: "INTERP1", interpolation: string.Empty),
                    }
                },
                expectedInterpolatorFunction = @"
PackedInput PackInput (Input input)
{
    PackedInput output;
    ZERO_INITIALIZE(PackedInput, output);
    output.packed_myVector3_A_myFloat_A.xyz = input.myVector3_A;
    output.packed_myVector3_A_myFloat_A.w = input.myFloat_A;
    output.packed_myVector3_B_myFloat_B.xyz = input.myVector3_B;
    output.packed_myVector3_B_myFloat_B.w = input.myFloat_B;
    return output;
}

Input UnpackInput (PackedInput input)
{
    Input output;
    output.myVector3_A = input.packed_myVector3_A_myFloat_A.xyz;
    output.myFloat_A = input.packed_myVector3_A_myFloat_A.w;
    output.myVector3_B = input.packed_myVector3_B_myFloat_B.xyz;
    output.myFloat_B = input.packed_myVector3_B_myFloat_B.w;
    return output;
}"
            },

            new PackingTestCase()
            {
                name = "Packing_F3F3F2",
                inputStruct = new StructDescriptor()
                {
                    name = "Input",
                    packFields = false,
                    fields = new []
                    {
                        new FieldDescriptor(tag: "Original", name: "myFloat_A", define: string.Empty, type: ShaderValueType.Float3, semantic: string.Empty, interpolation: string.Empty),
                        new FieldDescriptor(tag: "Original", name: "myFloat_B", define: string.Empty, type: ShaderValueType.Float3, semantic: string.Empty, interpolation: string.Empty),
                        new FieldDescriptor(tag: "Original", name: "myVector2", define: string.Empty, type: ShaderValueType.Float2, semantic: string.Empty, interpolation: string.Empty),
                    }
                },

                expectedOutputStruct = new StructDescriptor()
                {
                    name = "PackedInput",
                    packFields = true,
                    fields = new []
                    {
                        new FieldDescriptor(tag: "PackedInput", name: "packed_myFloat_A_myVector2x", define: string.Empty, type: "float4", semantic: "INTERP0", interpolation: string.Empty),
                        new FieldDescriptor(tag: "PackedInput", name: "packed_myFloat_B_myVector2y", define: string.Empty, type: "float4", semantic: "INTERP1", interpolation: string.Empty),
                    }
                },
                expectedInterpolatorFunction = @"
PackedInput PackInput (Input input)
{
    PackedInput output;
    ZERO_INITIALIZE(PackedInput, output);
    output.packed_myFloat_A_myVector2x.xyz = input.myFloat_A;
    output.packed_myFloat_A_myVector2x.w = input.myVector2.x;
    output.packed_myFloat_B_myVector2y.xyz = input.myFloat_B;
    output.packed_myFloat_B_myVector2y.w = input.myVector2.y;
    return output;
}

Input UnpackInput (PackedInput input)
{
    Input output;
    output.myFloat_A = input.packed_myFloat_A_myVector2x.xyz;
    output.myVector2.x = input.packed_myFloat_A_myVector2x.w;
    output.myFloat_B = input.packed_myFloat_B_myVector2y.xyz;
    output.myVector2.y = input.packed_myFloat_B_myVector2y.w;
    return output;
}
"
            },

            new PackingTestCase()
            {
                name = "Packing_F3F3F3F3",
                inputStruct = new StructDescriptor()
                {
                    name = "Input",
                    packFields = false,
                    fields = new []
                    {
                        new FieldDescriptor(tag: "Original", name: "myFloat3_A", define: string.Empty, type: ShaderValueType.Float3, semantic: string.Empty, interpolation: string.Empty),
                        new FieldDescriptor(tag: "Original", name: "myFloat3_B", define: string.Empty, type: ShaderValueType.Float3, semantic: string.Empty, interpolation: string.Empty),
                        new FieldDescriptor(tag: "Original", name: "myFloat3_C", define: string.Empty, type: ShaderValueType.Float3, semantic: string.Empty, interpolation: string.Empty),
                        new FieldDescriptor(tag: "Original", name: "myFloat3_D", define: string.Empty, type: ShaderValueType.Float3, semantic: string.Empty, interpolation: string.Empty),
                    }
                },

                expectedOutputStruct = new StructDescriptor()
                {
                    name = "PackedInput",
                    packFields = true,
                    fields = new []
                    {
                        new FieldDescriptor(tag: "PackedInput", name: "packed_myFloat3_A_myFloat3_Dx", define: string.Empty, type: "float4", semantic: "INTERP0", interpolation: string.Empty),
                        new FieldDescriptor(tag: "PackedInput", name: "packed_myFloat3_B_myFloat3_Dy", define: string.Empty, type: "float4", semantic: "INTERP1", interpolation: string.Empty),
                        new FieldDescriptor(tag: "PackedInput", name: "packed_myFloat3_C_myFloat3_Dz", define: string.Empty, type: "float4", semantic: "INTERP2", interpolation: string.Empty),
                    }
                },
                expectedInterpolatorFunction = @"
PackedInput PackInput (Input input)
{
    PackedInput output;
    ZERO_INITIALIZE(PackedInput, output);
    output.packed_myFloat3_A_myFloat3_Dx.xyz = input.myFloat3_A;
    output.packed_myFloat3_A_myFloat3_Dx.w = input.myFloat3_D.x;
    output.packed_myFloat3_B_myFloat3_Dy.xyz = input.myFloat3_B;
    output.packed_myFloat3_B_myFloat3_Dy.w = input.myFloat3_D.y;
    output.packed_myFloat3_C_myFloat3_Dz.xyz = input.myFloat3_C;
    output.packed_myFloat3_C_myFloat3_Dz.w = input.myFloat3_D.z;
    return output;
}

Input UnpackInput (PackedInput input)
{
    Input output;
    output.myFloat3_A = input.packed_myFloat3_A_myFloat3_Dx.xyz;
    output.myFloat3_D.x = input.packed_myFloat3_A_myFloat3_Dx.w;
    output.myFloat3_B = input.packed_myFloat3_B_myFloat3_Dy.xyz;
    output.myFloat3_D.y = input.packed_myFloat3_B_myFloat3_Dy.w;
    output.myFloat3_C = input.packed_myFloat3_C_myFloat3_Dz.xyz;
    output.myFloat3_D.z = input.packed_myFloat3_C_myFloat3_Dz.w;
    return output;
}
"
            },

            new PackingTestCase()
            {
                name = "Typical_Use_Case",
                inputStruct = new StructDescriptor()
                {
                    name = "Input",
                    packFields = false,
                    fields = new []
                    {
                        new FieldDescriptor(tag: "Original", name: "position",       define: string.Empty, type: ShaderValueType.Float4,  semantic: "SV_POSITION",        interpolation: "SV_POSITION_QUALIFIERS"),
                        new FieldDescriptor(tag: "Original", name: "myIntA",         define: string.Empty, type: ShaderValueType.Integer, semantic: string.Empty,         interpolation: "nointerpolation"),
                        new FieldDescriptor(tag: "Original", name: "myIntB",         define: string.Empty, type: ShaderValueType.Integer, semantic: string.Empty,         interpolation: "nointerpolation"),
                        new FieldDescriptor(tag: "Original", name: "myFloatA",       define: string.Empty, type: ShaderValueType.Float,   semantic: string.Empty,         interpolation: "nointerpolation"),
                        new FieldDescriptor(tag: "Original", name: "myFloatB",       define: string.Empty, type: ShaderValueType.Float2,  semantic: string.Empty,         interpolation: "nointerpolation"),
                        new FieldDescriptor(tag: "Original", name: "myInterFloatA",  define: string.Empty, type: ShaderValueType.Float,   semantic: string.Empty,         interpolation: string.Empty),
                        new FieldDescriptor(tag: "Original", name: "myInterFloatB",  define: string.Empty, type: ShaderValueType.Float2,  semantic: string.Empty,         interpolation: string.Empty),
                        new FieldDescriptor(tag: "Original", name: "instanceID",     define: string.Empty, type: ShaderValueType.Uint,    semantic: "CUSTOM_INSTANCE_ID", preprocessor: "UNITY_ANY_INSTANCING_ENABLED")
                    }
                },
                expectedOutputStruct = new StructDescriptor()
                {
                    name = "PackedInput",
                    packFields = true,
                    fields = new[]
                    {
                        new FieldDescriptor(tag: "Original",    name: "position",                           define: string.Empty, type: ShaderValueType.Float4,  semantic: "SV_POSITION",        interpolation: "SV_POSITION_QUALIFIERS"),
                        new FieldDescriptor(tag: "PackedInput", name: "myIntA",                             define: string.Empty, type: ShaderValueType.Integer, semantic: "INTERP0",            interpolation: "nointerpolation"),
                        new FieldDescriptor(tag: "PackedInput", name: "myIntB",                             define: string.Empty, type: ShaderValueType.Integer, semantic: "INTERP1",            interpolation: "nointerpolation"),
                        new FieldDescriptor(tag: "PackedInput", name: "packed_myFloatB_myFloatA",           define: string.Empty, type: "float3",                semantic: "INTERP2",            interpolation: "nointerpolation"),
                        new FieldDescriptor(tag: "PackedInput", name: "packed_myInterFloatB_myInterFloatA", define: string.Empty, type: "float3",                semantic: "INTERP3",            interpolation: string.Empty),
                        new FieldDescriptor(tag: "Original",    name: "instanceID",                         define: string.Empty, type: ShaderValueType.Uint,    semantic: "CUSTOM_INSTANCE_ID", preprocessor: "UNITY_ANY_INSTANCING_ENABLED")
                    }
                },

                expectedInterpolatorFunction = @"
PackedInput PackInput (Input input)
{
    PackedInput output;
    ZERO_INITIALIZE(PackedInput, output);
    output.position = input.position;
    output.myIntA = input.myIntA;
    output.myIntB = input.myIntB;
    output.packed_myFloatB_myFloatA.xy = input.myFloatB;
    output.packed_myFloatB_myFloatA.z = input.myFloatA;
    output.packed_myInterFloatB_myInterFloatA.xy = input.myInterFloatB;
    output.packed_myInterFloatB_myInterFloatA.z = input.myInterFloatA;
    #if UNITY_ANY_INSTANCING_ENABLED
    output.instanceID = input.instanceID;
    #endif
    return output;
}

Input UnpackInput (PackedInput input)
{
    Input output;
    output.position = input.position;
    output.myIntA = input.myIntA;
    output.myIntB = input.myIntB;
    output.myFloatB = input.packed_myFloatB_myFloatA.xy;
    output.myFloatA = input.packed_myFloatB_myFloatA.z;
    output.myInterFloatB = input.packed_myInterFloatB_myInterFloatA.xy;
    output.myInterFloatA = input.packed_myInterFloatB_myInterFloatA.z;
    #if UNITY_ANY_INSTANCING_ENABLED
    output.instanceID = input.instanceID;
    #endif
    return output;
}"
            }
        };

        [Test]
        public void GenerationUtils_GeneratePackedStruct([ValueSource(nameof(s_PackingTestCase))] PackingTestCase testCase)
        {
            var activeFields = new ActiveFields();
            foreach (var field in testCase.inputStruct.fields)
                activeFields.all.AddAll(field);

            GenerationUtils.GeneratePackedStruct(testCase.inputStruct, activeFields, out var packedStruct);

            var expected = testCase.expectedOutputStruct;
            Assert.AreEqual(expected.name, packedStruct.name);
            Assert.AreEqual(expected.packFields, packedStruct.packFields);
            Assert.AreEqual(expected.fields.Length, packedStruct.fields.Length);
            for (int i = 0; i < expected.fields.Length; i++)
            {
                var currentField = packedStruct.fields[i];
                var expectedField = expected.fields[i];
                Assert.AreEqual(expectedField.tag, currentField.tag);
                Assert.AreEqual(expectedField.name, currentField.name);
                Assert.AreEqual(expectedField.define, currentField.define);
                Assert.AreEqual(expectedField.interpolation, currentField.interpolation);
                Assert.AreEqual(expectedField.type, currentField.type);
                Assert.AreEqual(expectedField.vectorCount, currentField.vectorCount);
                Assert.AreEqual(expectedField.semantic, currentField.semantic);
                Assert.AreEqual(expectedField.preprocessor, currentField.preprocessor);
                Assert.AreEqual(expectedField.subscriptOptions, currentField.subscriptOptions);
            }
        }

        [Test]
        public void GenerationUtils_GenerateInterpolatorFunctions([ValueSource(nameof(s_PackingTestCase))] PackingTestCase packingTestCase)
        {
            var activeFields = new ActiveFields();
            foreach (var field in packingTestCase.inputStruct.fields)
                activeFields.all.AddAll(field);

            GenerationUtils.GenerateInterpolatorFunctions(packingTestCase.inputStruct, activeFields.baseInstance, true, out var shaderFunction);

            var result = shaderFunction.ToString();
            int length = Math.Max(packingTestCase.expectedInterpolatorFunction.Length, result.Length);
            var compare = String.Compare(packingTestCase.expectedInterpolatorFunction, 0, result, 0, length, CultureInfo.InvariantCulture, CompareOptions.IgnoreSymbols);
            Assert.AreEqual(0, compare, "Unexpected generated function:\n" + result);

        }

        [Test]
        public void GenerationUtils_ActivationFields()
        {
            var inputStruct = new StructDescriptor()
            {
                name = "Input",
                packFields = false,
                fields = new[]
                {
                    //This test insure the order of input packing doesn't fail the following packing
                    new FieldDescriptor(tag: "Original", name: "myFloat_Cond_0",    define: string.Empty, type: ShaderValueType.Float,  semantic: string.Empty, interpolation: string.Empty),
                    new FieldDescriptor(tag: "Original", name: "myFloat_Cond_1",    define: string.Empty, type: ShaderValueType.Float,  semantic: string.Empty, interpolation: string.Empty),
                    new FieldDescriptor(tag: "Original", name: "myFloat_Cond_2",    define: string.Empty, type: ShaderValueType.Float,  semantic: string.Empty, interpolation: string.Empty),
                    new FieldDescriptor(tag: "Original", name: "myFloat_Cond_ALL",  define: string.Empty, type: ShaderValueType.Float,  semantic: string.Empty, interpolation: string.Empty),
                }
            };

            var activeFields = new ActiveFields();
            foreach (var field in inputStruct.fields)
            {
                var index = field.name.EndsWith("0") ? 0 : field.name.EndsWith("1") ? 1 : field.name.EndsWith("2") ? 2 : -1;
                if (index != -1)
                {
                    activeFields[index].Add(field);
                }
                else
                {
                    activeFields.all.AddAll(field);
                }
            }

            GenerationUtils.GenerateInterpolatorFunctions(inputStruct, activeFields.baseInstance, true, out var shaderFunctionBuilder);
            GenerationUtils.GeneratePackedStruct(inputStruct, activeFields, out var packedStruct);
            GenerationUtils.GenerateShaderStruct(packedStruct, activeFields, true, out var structDeclarationBuilder);

            var expectedShaderFunction = @"
PackedInput PackInput (Input input)
{
    PackedInput output;
    ZERO_INITIALIZE(PackedInput, output);
    output.packed_myFloat_Cond_0_myFloat_Cond_1_myFloat_Cond_2_myFloat_Cond_ALL.x = input.myFloat_Cond_0;
    output.packed_myFloat_Cond_0_myFloat_Cond_1_myFloat_Cond_2_myFloat_Cond_ALL.y = input.myFloat_Cond_1;
    output.packed_myFloat_Cond_0_myFloat_Cond_1_myFloat_Cond_2_myFloat_Cond_ALL.z = input.myFloat_Cond_2;
    output.packed_myFloat_Cond_0_myFloat_Cond_1_myFloat_Cond_2_myFloat_Cond_ALL.w = input.myFloat_Cond_ALL;
    return output;
}

Input UnpackInput (PackedInput input)
{
    Input output;
    output.myFloat_Cond_0 = input.packed_myFloat_Cond_0_myFloat_Cond_1_myFloat_Cond_2_myFloat_Cond_ALL.x;
    output.myFloat_Cond_1 = input.packed_myFloat_Cond_0_myFloat_Cond_1_myFloat_Cond_2_myFloat_Cond_ALL.y;
    output.myFloat_Cond_2 = input.packed_myFloat_Cond_0_myFloat_Cond_1_myFloat_Cond_2_myFloat_Cond_ALL.z;
    output.myFloat_Cond_ALL = input.packed_myFloat_Cond_0_myFloat_Cond_1_myFloat_Cond_2_myFloat_Cond_ALL.w;
    return output;
}";

            var expectedStructDeclaration = @"
struct PackedInput
{
    #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2)
     float4 packed_myFloat_Cond_0_myFloat_Cond_1_myFloat_Cond_2_myFloat_Cond_ALL : INTERP0;
    #endif
};";
            {
                var shaderFunction = shaderFunctionBuilder.ToString();
                var length = Math.Max(expectedShaderFunction.Length, shaderFunction.Length);
                var compare = String.Compare(expectedShaderFunction, 0, shaderFunction, 0, length, CultureInfo.InvariantCulture, CompareOptions.IgnoreSymbols);
                Assert.AreEqual(0, compare, "Unexpected generated function:\n" + shaderFunction);
            }

            {
                var structDeclaration = structDeclarationBuilder.ToString();
                var length = Math.Max(expectedStructDeclaration.Length, structDeclaration.Length);
                var compare = String.Compare(expectedStructDeclaration, 0, expectedStructDeclaration, 0, length, CultureInfo.InvariantCulture, CompareOptions.IgnoreSymbols);
                Assert.AreEqual(0, compare, "Unexpected generated function:\n" + structDeclaration);
            }
        }
    }
}
