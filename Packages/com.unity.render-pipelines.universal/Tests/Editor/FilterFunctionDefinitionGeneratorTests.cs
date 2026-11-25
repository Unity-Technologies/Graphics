using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEngine.Rendering.Universal.Tests
{
    // Tests FilterFunctionDefinitionGenerator: Blackboard property -> FilterFunctionDefinition parameter
    // mapping, and the pass setup that lets UI Toolkit draw it instanced.
    class FilterFunctionDefinitionGeneratorTests
    {
        static (GraphData graph, UniversalFilterSubTarget subTarget) NewFilterGraph()
        {
            var graph = new GraphData();
            graph.AddContexts();

            var target = new UniversalTarget();
            graph.InitializeOutputs(new Target[] { target }, null);
            Assert.IsTrue(target.TrySetActiveSubTarget(typeof(UniversalFilterSubTarget)));

            graph.OnEnable();
            graph.ValidateGraph();

            var subTarget = (UniversalFilterSubTarget)target.activeSubTarget;
            return (graph, subTarget);
        }

        static Material NewDummyMaterial() => new Material(Shader.Find("Hidden/InternalErrorShader"));

        [Test]
        public void Generate_MapsFloatAndColorProperties_ToParametersAndBindings()
        {
            var (graph, _) = NewFilterGraph();

            var floatProperty = new Vector1ShaderProperty { displayName = "Amount", generatePropertyBlock = true, value = 0.4f };
            graph.AddGraphInput(floatProperty);

            var colorProperty = new ColorShaderProperty { displayName = "Tint", generatePropertyBlock = true, value = Color.red };
            graph.AddGraphInput(colorProperty);

            var definition = FilterFunctionDefinitionGenerator.Generate(graph, NewDummyMaterial(), "TestFilter");

            Assert.AreEqual(2, definition.parameters.Length, "Expected one parameter per exposed float/color property.");

            Assert.AreEqual("Amount", definition.parameters[0].name);
            Assert.AreEqual(FilterParameterType.Float, definition.parameters[0].interpolationDefaultValue.type);
            Assert.AreEqual(0.4f, definition.parameters[0].interpolationDefaultValue.floatValue);

            Assert.AreEqual("Tint", definition.parameters[1].name);
            Assert.AreEqual(FilterParameterType.Color, definition.parameters[1].interpolationDefaultValue.type);
            Assert.AreEqual(Color.red, definition.parameters[1].interpolationDefaultValue.colorValue);

            Assert.AreEqual(1, definition.passes.Length);
            var bindings = definition.passes[0].parameterBindings;
            Assert.AreEqual(2, bindings.Length);
            Assert.AreEqual(0, bindings[0].index);
            Assert.AreEqual(floatProperty.referenceName, bindings[0].name);
            Assert.AreEqual(1, bindings[1].index);
            Assert.AreEqual(colorProperty.referenceName, bindings[1].name);
        }

        [Test]
        public void Generate_NonExposedProperty_IsSkipped()
        {
            var (graph, _) = NewFilterGraph();

            var hiddenProperty = new Vector1ShaderProperty { displayName = "Hidden", generatePropertyBlock = false, value = 1f };
            graph.AddGraphInput(hiddenProperty);

            var definition = FilterFunctionDefinitionGenerator.Generate(graph, NewDummyMaterial(), "TestFilter");

            Assert.AreEqual(0, definition.parameters.Length);
            Assert.AreEqual(0, definition.passes[0].parameterBindings.Length);
        }

        [Test]
        public void Generate_MoreThanFourExposedProperties_KeepsOnlyFourAndWarns()
        {
            var (graph, _) = NewFilterGraph();

            for (int i = 0; i < 5; i++)
                graph.AddGraphInput(new Vector1ShaderProperty { displayName = $"P{i}", generatePropertyBlock = true, value = i });

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("exposes more than 4 parameters"));

            var definition = FilterFunctionDefinitionGenerator.Generate(graph, NewDummyMaterial(), "TestFilter");

            Assert.AreEqual(4, definition.parameters.Length);
        }

        [Test]
        public void Generate_LeavesTheParametersToTheBindings()
        {
            // A settings callback would make UI Toolkit draw the pass one element at a time
            var (graph, _) = NewFilterGraph();
            graph.AddGraphInput(new Vector1ShaderProperty { displayName = "Amount", generatePropertyBlock = true });

            var definition = FilterFunctionDefinitionGenerator.Generate(graph, NewDummyMaterial(), "TestFilter");

            Assert.IsNull(definition.passes[0].applySettingsCallback);
            Assert.IsNull(definition.passes[0].writeParametersCallback);
        }

        [Test]
        public void Generate_EnablesInstancingOnTheMaterial()
        {
            var (graph, _) = NewFilterGraph();
            var material = NewDummyMaterial();
            try
            {
                FilterFunctionDefinitionGenerator.Generate(graph, material, "TestFilter");

                Assert.IsTrue(material.enableInstancing);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void InstancedParameters_DeclareEveryExposedNumberPerInstance()
        {
            var (graph, _) = NewFilterGraph();
            var numbers = new (AbstractShaderProperty property, string type)[]
            {
                (new Vector1ShaderProperty { displayName = "Amount", generatePropertyBlock = true }, "float"),
                (new BooleanShaderProperty { displayName = "Enabled", generatePropertyBlock = true }, "float"),
                (new Vector2ShaderProperty { displayName = "Offset", generatePropertyBlock = true }, "float2"),
                (new Vector3ShaderProperty { displayName = "Axis", generatePropertyBlock = true }, "float3"),
                (new Vector4ShaderProperty { displayName = "Bounds", generatePropertyBlock = true }, "float4"),
                (new ColorShaderProperty { displayName = "Tint", generatePropertyBlock = true }, "float4"),
            };
            foreach (var (property, _) in numbers)
            {
                graph.AddGraphInput(property);
            }
            var texture = new Texture2DShaderProperty { displayName = "Mask", generatePropertyBlock = true };
            graph.AddGraphInput(texture);

            var builder = new ShaderStringBuilder();
            new FilterInstancedParametersProperty(graph.properties)
                .ForeachHLSLProperty(GenerationMode.ForReals, declaration => declaration.AppendTo(builder));
            var code = builder.ToCodeBlock();

            foreach (var (property, type) in numbers)
            {
                StringAssert.Contains($"UNITY_DEFINE_INSTANCED_PROP({type}, {property.referenceName})", code);
            }
            StringAssert.DoesNotContain(texture.referenceName, code);
        }

        [Test]
        public void Generate_ReadWriteMargins_ReflectSubTargetConfiguration()
        {
            var (graph, subTarget) = NewFilterGraph();
            subTarget.filterData.readMargin = 12f;
            subTarget.filterData.writeMargin = 7f;

            var definition = FilterFunctionDefinitionGenerator.Generate(graph, NewDummyMaterial(), "TestFilter");
            var pass = definition.passes[0];

            // readMargins is internal with no grant for this assembly; read it via reflection.
            var readMargins = (PostProcessingMargins)typeof(PostProcessingPass)
                .GetProperty("readMargins", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(pass);
            Assert.AreEqual(12f, readMargins.left);
            Assert.AreEqual(12f, readMargins.top);
            Assert.AreEqual(12f, readMargins.right);
            Assert.AreEqual(12f, readMargins.bottom);

            Assert.AreEqual(7f, pass.writeMargins.left);
            Assert.AreEqual(7f, pass.writeMargins.top);
            Assert.AreEqual(7f, pass.writeMargins.right);
            Assert.AreEqual(7f, pass.writeMargins.bottom);
        }
    }
}
