using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.ShaderFoundry;
using UnityEditor.ShaderGraph.Defs;
using UnityEditor.ShaderGraph.Generation;
using UnityEngine;
using static UnityEditor.ShaderGraph.Generation.Interpreter;
using static UnityEditor.ShaderGraph.GraphDelta.ContextEntryEnumTags;

namespace UnityEditor.ShaderGraph.GraphDelta.UnitTests
{
    // TODO: This belongs in Unity.ShaderGraph.Generation.Tests.
    // It's temporarily in Unity.ShaderGraph.GraphDelta.Tests because the Editor doesn't have an InternalsVisibleTo
    // necessary to use internal Shader Foundry classes from Unity.ShaderGraph.Generation.Tests.
    [TestFixture]
    static class TempGenerationTests
    {
        private static GraphHandler graph;
        private static Registry registry;

        [SetUp]
        public static void Setup()
        {
            registry = new Registry();
            registry.Register<GraphType>();
            registry.Register<GraphTypeAssignment>();
            registry.Register<PropertyContext>();
            registry.Register<Defs.ShaderGraphContext>();
            registry.Register<BaseTextureType>();
            var contextKey = Registry.ResolveKey<Defs.ShaderGraphContext>();
            var propertyKey = Registry.ResolveKey<PropertyContext>();
            graph = new GraphHandler(registry);

            //graph.AddContextNode("VertIn");
            //graph.AddContextNode("VertOut");
            graph.AddContextNode(propertyKey);
            graph.AddContextNode(contextKey);

            //graph.RebuildContextData("VertIn", GetTarget(), "UniversalPipeline", "VertexDescription", true);
            //graph.RebuildContextData("VertOut", GetTarget(), "UniversalPipeline", "VertexDescription", false);
            graph.RebuildContextData(propertyKey.Name, GetTarget(), "UniversalPipeline", "SurfaceDescription", true);
        }

        static internal Target GetTarget()
        {
            var targetTypes = TypeCache.GetTypesDerivedFrom<Target>();
            foreach (var type in targetTypes)
            {
                if (type.IsAbstract || type.IsGenericType || !type.IsClass || type.Name != "UniversalTarget")
                    continue;

                var target = (Target)Activator.CreateInstance(type);
                if (!target.isHidden)
                    return target;
            }

            return null;
        }

        static PortHandler MakeIncludedReferableEntry(ITypeDescriptor type, string fieldName)
        {
            var propertyKey = Registry.ResolveKey<PropertyContext>();
            var propContext = graph.GetNode(propertyKey.Name);
            ContextBuilder.AddReferableEntry(propContext, type, fieldName, registry, PropertyBlockUsage.Included);
            return propContext.GetPort(fieldName);
        }

        static List<StructField> GetBuiltInputs(PortHandler port)
        {
            var container = new ShaderContainer();
            var outputs = new VariableRegistry();
            var inputs = new VariableRegistry(); 
            var textures = new List<(string, Texture)>();

            Interpreter.BuildPropertyAttributes(port, registry, container, ref outputs, ref inputs, ref textures);

            return inputs.ToList();
        }

        [Test]
        public static void TestBuildPropertyAttributes_GraphType_Color()
        {
            var vec3NoColorPort = MakeIncludedReferableEntry(TYPE.Vec3, "Test_Vec3_NoColor");
            var vec3NoColorField = GetBuiltInputs(vec3NoColorPort).First();
            Assert.IsFalse(vec3NoColorField.Attributes.Any(a => a.Name.Equals("Color")), $"Vec3 without {kIsColor} should not have Color attribute");

            var vec3ColorPort = MakeIncludedReferableEntry(TYPE.Vec3, "Test_Vec3_Color");
            vec3ColorPort.AddField(kIsColor, true);
            var vec3ColorField = GetBuiltInputs(vec3ColorPort).First();
            Assert.IsTrue(vec3ColorField.Attributes.Any(a => a.Name.Equals("Color")), $"Vec3 with {kIsColor} should have Color attribute");
        }

        [Test]
        public static void TestBuildPropertyAttributes_GraphType_Hdr()
        {
            var vec3NoColorPort = MakeIncludedReferableEntry(TYPE.Vec3, "Test_Vec3_NoColor_Hdr");
            vec3NoColorPort.AddField(kIsHdr, true);
            var vec3NoColorField = GetBuiltInputs(vec3NoColorPort).First();
            Assert.IsFalse(vec3NoColorField.Attributes.Any(a => a.Name.Equals("HDR")), $"Vec3 with {kIsHdr} but without {kIsColor} should not have HDR attribute");

            var vec3ColorPort = MakeIncludedReferableEntry(TYPE.Vec3, "Test_Vec3_Color_Hdr");
            vec3ColorPort.AddField(kIsColor, true);
            vec3ColorPort.AddField(kIsHdr, true);
            var vec3ColorField = GetBuiltInputs(vec3ColorPort).First();
            Assert.IsTrue(vec3ColorField.Attributes.Any(a => a.Name.Equals("HDR")), $"Vec3 with {kIsHdr} and {kIsColor} should have HDR attribute");
        }

        [Test]
        public static void TestBuildPropertyAttributes_Texture_UseTilingOffset()
        {
            var textureNoTilingOffsetPort = MakeIncludedReferableEntry(TYPE.Texture2D, "Test_Texture2D_NoTilingOffset");
            var textureNoTilingOffsetField = GetBuiltInputs(textureNoTilingOffsetPort).First();
            Assert.IsTrue(textureNoTilingOffsetField.Attributes.Any(a => a.Name.Equals("NoScaleOffset")), $"Texture2D without {kTextureUseTilingOffset} should have NoScaleOffset attribute");
            Assert.IsFalse(textureNoTilingOffsetField.Attributes.Any(a => a.Name.Equals("ScaleOffset")), $"Texture2D without {kTextureUseTilingOffset} should not have ScaleOffset attribute");

            var textureTilingOffsetPort = MakeIncludedReferableEntry(TYPE.Texture2D, "Test_Texture2D_TilingOffset");
            textureTilingOffsetPort.AddField(kTextureUseTilingOffset, true);
            var textureTilingOffsetField = GetBuiltInputs(textureTilingOffsetPort).First();
            Assert.IsTrue(textureTilingOffsetField.Attributes.Any(a => a.Name.Equals("ScaleOffset")), $"Texture2D with {kTextureUseTilingOffset} should have ScaleOffset attribute");
            Assert.IsFalse(textureTilingOffsetField.Attributes.Any(a => a.Name.Equals("NoScaleOffset")), $"Texture2D with {kTextureUseTilingOffset} should not have NoScaleOffset attribute");
        }

        static (TextureDefaultType, string)[] s_TextureDefaultValueCases =
        {
            (TextureDefaultType.White, "\"white\" {}"),
            (TextureDefaultType.Black, "\"black\" {}"),
            (TextureDefaultType.Grey, "\"grey\" {}"),
            (TextureDefaultType.NormalMap, "\"bump\" {}"),
            (TextureDefaultType.LinearGrey, "\"linearGrey\" {}"),
            (TextureDefaultType.Red, "\"red\" {}"),
        };

        [Test]
        [TestCaseSource(nameof(s_TextureDefaultValueCases))]
        public static void TestGetDefaultValueString_Texture((TextureDefaultType, string) testCase)
        {
            var (type, defaultValue) = testCase;
            var port = MakeIncludedReferableEntry(TYPE.Texture2D, "Test_Texture2D_TextureDefaultType");
            port.AddField(kTextureDefaultType, type);
            Assert.AreEqual(defaultValue, port.GetDefaultValueString(registry, new ShaderContainer()), $"Default value for texture with type {type} should be {defaultValue}");
        }
    }
}
