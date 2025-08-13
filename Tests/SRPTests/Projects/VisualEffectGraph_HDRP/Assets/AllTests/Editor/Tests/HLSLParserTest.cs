#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using System.Linq;
using Moq;
using NUnit.Framework;
using UnityEditor.VFX.Block;
using UnityEngine;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    class VFXCustomHLSLTest
    {
        const string templateHlslCode = "void CustomHLSL(inout VFXAttributes attributes, {0} {1} {2}) {{{3}}}";

        private IVFXAttributesManager attributesManager;

        [SetUp]
        public void Setup()
        {
            this.attributesManager = new VFXAttributesManager();
        }

        [TestCase("float", typeof(float))]
        [TestCase("uint", typeof(uint))]
        [TestCase("int", typeof(int))]
        [TestCase("bool", typeof(bool))]
        [TestCase("float2", typeof(Vector2))]
        [TestCase("float3", typeof(Vector3))]
        [TestCase("float4", typeof(Vector4))]
        [TestCase("float4x4", typeof(Matrix4x4))]
        [TestCase("VFXSampler2D", typeof(Texture2D))]
        [TestCase("VFXSampler3D", typeof(Texture3D))]
        [TestCase("VFXGradient", typeof(Gradient))]
        [TestCase("VFXCurve", typeof(AnimationCurve))]
        [TestCase("ByteAddressBuffer", typeof(GraphicsBuffer))]
        public void HLSL_Check_Parameter_Supported_Types(string hlslType, Type csharpType)
        {
            // Arrange
            var hlslCode = string.Format(templateHlslCode, "in", hlslType, "param", string.Empty);

            // Act
            var functions = HLSLFunction.Parse(this.attributesManager, hlslCode).ToArray();
            var function = functions.FirstOrDefault();

            // Assert
            Assert.AreEqual(1, functions.Length, "HLSL code could not be parsed properly to detect a function");
            Assert.NotNull(function);

            Assert.AreEqual(2, function.inputs.Count(), "Function parameters were not properly detected");
            var input = function.inputs.ElementAt(1); // The first parameter is a VFXAttributes
            Assert.NotNull(input);
            Assert.AreEqual(HLSLAccess.IN, input.access, "Wrong parameter access modifier");
            Assert.AreEqual("param", input.name, "Wrong parameter name");
            Assert.AreEqual(hlslType, input.rawType, "Wrong parameter hlsl type");
            Assert.AreEqual(csharpType, input.type, "Wrong parameter csharp type");
            Assert.IsNull(input.errors, "There was errors when parsing parameters");
        }

        [TestCase("in", HLSLAccess.IN)]
        [TestCase("out", HLSLAccess.OUT)]
        [TestCase("", HLSLAccess.NONE)]
        [TestCase("inout", HLSLAccess.INOUT)]
        public void HLSL_Check_Parameter_Access_Modifier(string modifier, HLSLAccess access)
        {
            // Arrange
            var hlslCode = string.Format(templateHlslCode, modifier, "float", "param", string.Empty);

            // Act
            var function = HLSLFunction.Parse(this.attributesManager, hlslCode).Single();

            // Assert
            if (access != HLSLAccess.NONE)
            {
                var input = function.inputs.ElementAt(1); // The first parameter is a VFXAttributes
                Assert.AreEqual(access, input.access, "Wrong parameter access modifier");
            }
            else
            {
                // Should handle this case
            }
        }

        [TestCase("toto")]
        [TestCase("someName")]
        [TestCase("_otherName")]
        public void HLSL_Check_Parameter_Name(string name)
        {
            // Arrange
            var hlslCode = string.Format(templateHlslCode, "in", "float", name, string.Empty);

            // Act
            var function = HLSLFunction.Parse(this.attributesManager, hlslCode).Single();

            // Assert
            var input = function.inputs.ElementAt(1); // The first parameter is a VFXAttributes
            Assert.AreEqual(name, input.name, "Parameter name not correctly parsed");
        }

        [TestCase("int")]
        [TestCase("uint")]
        [TestCase("float")]
        public void HLSL_Check_Parameter_StructuredBuffer(string templateType)
        {
            // Arrange
            var hlslCode = string.Format(templateHlslCode, "in", $"StructuredBuffer<{templateType}>", "buffer", string.Empty);

            // Act
            var function = HLSLFunction.Parse(this.attributesManager, hlslCode).Single();

            // Assert
            var input = function.inputs.ElementAt(1); // The first parameter is a VFXAttributes
            Assert.NotNull(input);
            Assert.AreEqual(HLSLAccess.IN, input.access, "Wrong parameter access modifier");
            Assert.AreEqual("buffer", input.name, "Wrong parameter name");
            Assert.AreEqual("StructuredBuffer", input.rawType, "Wrong parameter hlsl type");
            Assert.AreEqual(typeof(GraphicsBuffer), input.type, "Wrong parameter csharp type");
            Assert.AreEqual(templateType, input.bufferType.verbatimType, "Wrong Structured buffer template parameter type");
            Assert.IsNull(input.errors, "There was errors when parsing parameters");
        }

        [Test]
        public void HLSL_Check_Parameter_ByteAddressBuffer()
        {
            // Arrange
            var hlslCode = string.Format(templateHlslCode, "in", $"ByteAddressBuffer", "buffer", string.Empty);

            // Act
            var function = HLSLFunction.Parse(this.attributesManager, hlslCode).Single();

            // Assert
            var input = function.inputs.ElementAt(1); // The first parameter is a VFXAttributes
            Assert.NotNull(input);
            Assert.AreEqual("ByteAddressBuffer", input.rawType, "Wrong parameter hlsl type");
            Assert.AreEqual(typeof(GraphicsBuffer), input.type, "Wrong parameter csharp type");
            Assert.AreEqual(input.bufferType.actualType, typeof(void), "ByteAddressBuffer must not have a template type");
            Assert.IsNull(input.errors, "There was errors when parsing parameters");
        }

        [Test]
        public void HLSL_Check_Parameter_Unsupported_Type()
        {
            // Arrange
            var hlslCode = string.Format(templateHlslCode, "in", $"float2x2", "mat", string.Empty);

            // Act
            var function = HLSLFunction.Parse(this.attributesManager, hlslCode).Single();

            // Assert
            var input = function.inputs.ElementAt(1); // The first parameter is a VFXAttributes
            Assert.NotNull(input);
            Assert.IsNotEmpty(input.errors, "float2x2 is not a supported type, the parameter should should hold an error");
            Assert.IsInstanceOf<HLSLUnknownParameterType>(input.errors.Single());
        }

        [Test]
        public void HLSL_Check_Parameter_Documentation_Type()
        {
            // Arrange
            var hlslCode =
                "/// offset: this is the offset" + "\n" +
                "/// speedFactor: this is the speedFactor" + "\r\n" +
                "void CustomHLSL(inout VFXAttributes attributes, in float3 offset, in float speedFactor)" + "\n" +
                "{" + "\n" +
                "  attributes.position += offset;" + "\n" +
                "  attributes.velocity *= speedFactor;" + "\n" +
                "}";

            // Act
            var function = HLSLFunction.Parse(this.attributesManager, hlslCode).Single();

            // Assert
            foreach (var parameter in function.inputs.Skip(1)) // Skip the first parameter which is VFXAttributes
            {
                Assert.AreEqual($"this is the {parameter.name}", parameter.tooltip);
            }
        }

        [Test]
        public void HLSL_Check_Return_Documentation()
        {
            // Arrange
            var hlslCode =
                "/// return: myoutslot" + "\n" +
                "float CustomHLSL(in float3 offset)" + "\n" +
                "{" + "\n" +
                "  return offset.x;" + "\n" +
                "}";

            // Act
            var function = HLSLFunction.Parse(this.attributesManager, hlslCode).Single();

            // Assert
            Assert.AreEqual("myoutslot", function.returnName);
        }

        [Test]
        public void HLSL_Check_Hidden_Function()
        {
            // Arrange
            var hlslCode =
                "/// Hidden" + "\n" +
                "void HelperFunction(in float param, out float3 pos)" + "\n" +
                "{" + "\n" +
                "  float3 pos = float3(param, param, param);" + "\n" +
                "}";

            // Act
            var functions = HLSLFunction.Parse(this.attributesManager, hlslCode);

            // Assert
            Assert.IsEmpty(functions);
        }

        [Test]
        public void HLSL_Check_Parse_Include()
        {
            // Arrange
            var includePath = "/path/to/include/file.hlsl";

            var hlslCode =
                $"#include \"{includePath}\"" + "\n" +
                "void HelperFunction(in float param, out float3 pos)" + "\n" +
                "{" + "\n" +
                "  float3 pos = float3(param, param, param);" + "\n" +
                "}";

            // Act
            var includes = HLSLParser.ParseIncludes(hlslCode).ToArray();

            // Assert
            Assert.IsNotEmpty(includes);
            Assert.AreEqual(1, includes.Length);
            Assert.AreEqual(includePath, includes[0]);
        }

        [Test]
        public void HLSL_Check_Space_Before_Parameter()
        {
            // Arrange
            var hlslCode =
                $"void Function( in float param)" + "\n" +
                "{" + "\n" +
                "  float3 pos = float3(param, param, param);" + "\n" +
                "}";

            // Act
            var function = HLSLFunction.Parse(this.attributesManager, hlslCode).Single();

            // Assert
            var input = function.inputs.FirstOrDefault();
            Assert.NotNull(input, "Could not properly parse input parameter");
            Assert.AreEqual("param", input.name);
            Assert.AreEqual(HLSLAccess.IN, input.access);
            Assert.AreEqual(typeof(float), input.type);
            Assert.AreEqual("float", input.rawType);
        }

        [Test]
        public void HLSL_Check_Use_Of_Rand_Macro()
        {
            // Arrange
            var hlslCode =
                $"void Function(in VFXAttributes attributes)" + "\n" +
                "{" + "\n" +
                "  float r = VFXRAND;" + "\n" +
                "}";

            // Act
            var function = HLSLFunction.Parse(this.attributesManager, hlslCode).Single();

            // Assert
            Assert.AreEqual(1, function.attributes.Count);
            Assert.AreEqual("seed", function.attributes.Single().attrib.name);
        }

        [Test]
        public void HLSL_Check_Function_No_Parameter()
        {
            // Arrange
            var hlslCode =
                $"void Function()" + "\n" +
                "{" + "\n" +
                "  float r = 1;" + "\n" +
                "}";

            // Act
            var function = HLSLFunction.Parse(this.attributesManager, hlslCode).Single();

            // Assert
            Assert.AreEqual(0, function.errorList.Count);
            Assert.AreEqual(0, function.inputs.Count);
        }

        [Test]
        public void HLSL_Check_Function_Only_Out_Parameters()
        {
            // Arrange
            var hlslCode =
                $"void Function(out float a, out float b)" + "\n" +
                "{" + "\n" +
                "  a = 1;" + "\n" +
                "  b = 2;" + "\n" +
                "}";

            // Act
            var function = HLSLFunction.Parse(this.attributesManager, hlslCode).Single();

            // Assert
            Assert.AreEqual(0, function.errorList.Count);
            Assert.AreEqual(2, function.inputs.Count);
            Assert.IsTrue(function.inputs.All(x => x.access is HLSLAccess.OUT));
        }

        [Test]
        public void HLSL_Check_Missing_Closing_Curly_Bracket()
        {
            // Arrange
            var hlslCode =
                $"void Function(in float param)\n" +
                "{\n" +
                "  float3 pos = float3(param, param, param);\n";

            // Act
            var functions = HLSLFunction.Parse(this.attributesManager, hlslCode);

            // Assert
            // The function must be detected, but the compute shader would not compile
            CollectionAssert.IsNotEmpty(functions);
        }

        [Test, Description("Covers issue UUM-40706")]
        public void HLSL_Check_Nested_Curly_Bracket()
        {
            // Arrange
            var hlslCode =
                $"void Repro(inout VFXAttributes attributes, in float3 centerBox, in float3 sizeBox, in float deltaTime)" +
                "{\n" +
                "    bool alive = attributes.alive;\n" +
                "    if (alive)\n" +
                "    {\n" +
                "        for (int i = -1; i <= 1; ++i)\n" +
                "        {\n" +
                "            for (int j = -1; j <= 1; ++j)\n" +
                "            {\n" +
                "            }\n" +
                "        }\n" +
                "    }\n" +
                "}\n";

            // Act
            var functions = HLSLFunction.Parse(this.attributesManager, hlslCode);

            // Assert
            CollectionAssert.IsNotEmpty(functions);
        }

        [Test, Description("Covers issue UUM-71490")]
        public void HLSL_Check_Int_Without_Access_Modifier()
        {
            // Arrange
            var hlslCode =
                $"void Repro(inout VFXAttributes attributes, int parameter)" +
                "{\n" +
                "    // Nothing\n" +
                "}\n";

            // Act
            var functions = HLSLFunction.Parse(this.attributesManager, hlslCode).ToArray();

            // Assert
            CollectionAssert.IsNotEmpty(functions);
            var parameters = functions[0].inputs.ToArray();
            Assert.AreEqual(2, parameters.Length);
            Assert.AreEqual("int", parameters[1].rawType);
            Assert.AreEqual(typeof(int), parameters[1].type);
            Assert.AreEqual(HLSLAccess.NONE, parameters[1].access);
        }

        [Test, Description("Covers issue UUM-74375")]
        public void HLSL_Variadic_Attributes()
        {
            // Arrange
            var hlslCode =
                $"void Repro(inout VFXAttributes attributes)" +
                "{\n" +
                "    attributes.scale = float3(1, 2, 3);\n" +
                "}\n";

            // Act
            var functions = HLSLFunction.Parse(this.attributesManager, hlslCode).ToArray();

            // Assert
            CollectionAssert.IsNotEmpty(functions);
            var errors = functions[0].errorList.ToArray();
            Assert.AreEqual(1, errors.Length, "Missing error feedback when using variadic attribute in custom hlsl code");
            Assert.IsInstanceOf<HLSLVFXAttributeIsVariadic>(errors[0]);
        }

        [Test, Description("Covers issue UUM-79389")]
        public void HLSL_Parameters_On_Different_Lines()
        {
            // Arrange
            var hlslCode =
                "void CustomHLSL(inout VFXAttributes attributes" +
                "  , in float3 offset" +
                "  , in float speedFactor)" + "\n" +
                "{" + "\n" +
                "  attributes.position += offset;" + "\n" +
                "  attributes.velocity *= speedFactor;" + "\n" +
                "}";

            // Act
            var function = HLSLFunction.Parse(this.attributesManager, hlslCode).Single();

            // Assert
            Assert.AreEqual(3, function.inputs.Count);
            Assert.AreEqual(new [] { "attributes", "offset", "speedFactor" }, function.inputs.Select(x => x.name));
            Assert.AreEqual(new [] { "VFXAttributes", "float3", "float" }, function.inputs.Select(x => x.rawType));
            Assert.AreEqual(new [] { HLSLAccess.INOUT, HLSLAccess.IN, HLSLAccess.IN }, function.inputs.Select(x => x.access));

        }
    }
}
#endif
