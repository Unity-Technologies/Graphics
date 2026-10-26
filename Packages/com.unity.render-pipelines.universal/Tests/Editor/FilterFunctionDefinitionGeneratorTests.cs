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
    // mapping, and the generated applySettingsCallback's runtime behavior.
    class FilterFunctionDefinitionGeneratorTests
    {
        // FilterPassContext setters are internal to the engine module; reflection avoids an IVT grant needing an engine rebuild.
        static FilterPassContext MakeContext(FilterFunction filterFunction, bool readsGamma = false)
        {
            object boxed = new FilterPassContext();
            typeof(FilterPassContext).GetProperty(nameof(FilterPassContext.filterFunction))
                .GetSetMethod(nonPublic: true).Invoke(boxed, new object[] { filterFunction });
            typeof(FilterPassContext).GetProperty(nameof(FilterPassContext.readsGamma))
                .GetSetMethod(nonPublic: true).Invoke(boxed, new object[] { readsGamma });
            return (FilterPassContext)boxed;
        }

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
        public void Generate_ApplySettingsCallback_WritesLiveParameterValuesIntoPropertyBlock()
        {
            // parameterBindings alone is metadata; only applySettingsCallback pushes values at runtime.
            var (graph, _) = NewFilterGraph();

            var floatProperty = new Vector1ShaderProperty { displayName = "Amount", generatePropertyBlock = true, value = 0.1f };
            graph.AddGraphInput(floatProperty);

            var definition = FilterFunctionDefinitionGenerator.Generate(graph, NewDummyMaterial(), "TestFilter");
            Assert.IsNotNull(definition.passes[0].applySettingsCallback,
                "applySettingsCallback must be set -- parameterBindings alone is not applied at runtime.");

            // A *different* live value than the property's baked-in default (0.1f), to prove the callback
            // reads the live FilterFunction value rather than the definition's own default.
            var liveFunction = new FilterFunction(definition);
            liveFunction.AddParameter(new FilterParameter(0.75f));

            var context = MakeContext(liveFunction);
            var mpb = new MaterialPropertyBlock();
            definition.passes[0].applySettingsCallback(mpb, context);

            Assert.AreEqual(0.75f, mpb.GetFloat(floatProperty.referenceName));
        }

        [Test]
        public void Generate_ApplySettingsCallback_WritesLiveColorValue()
        {
            var (graph, _) = NewFilterGraph();

            var colorProperty = new ColorShaderProperty { displayName = "Tint", generatePropertyBlock = true, value = Color.black };
            graph.AddGraphInput(colorProperty);

            var definition = FilterFunctionDefinitionGenerator.Generate(graph, NewDummyMaterial(), "TestFilter");

            var liveFunction = new FilterFunction(definition);
            liveFunction.AddParameter(new FilterParameter(Color.cyan));

            var context = MakeContext(liveFunction);
            var mpb = new MaterialPropertyBlock();
            definition.passes[0].applySettingsCallback(mpb, context);

            Assert.AreEqual(Color.cyan, mpb.GetColor(colorProperty.referenceName));
        }

        [Test]
        public void Generate_ApplySettingsCallback_UnderSpecifiedFilterFunction_LeavesMaterialDefaults()
        {
            // The public FilterFunction ctor does not pad missing parameters; the callback must skip them.
            var (graph, _) = NewFilterGraph();

            var colorProperty = new ColorShaderProperty { displayName = "Tint", generatePropertyBlock = true, value = Color.red };
            graph.AddGraphInput(colorProperty);

            var definition = FilterFunctionDefinitionGenerator.Generate(graph, NewDummyMaterial(), "TestFilter");

            var liveFunction = new FilterFunction(definition); // no AddParameter: parameterCount == 0

            var mpb = new MaterialPropertyBlock();
            definition.passes[0].applySettingsCallback(mpb, MakeContext(liveFunction));

            Assert.IsTrue(mpb.isEmpty,
                "Bindings without a live parameter value must not be written; the material's own defaults (the blackboard values) must show through.");
        }

        [Test]
        public void Generate_ApplySettingsCallback_ReadsGamma_WritesRawSRGBColor()
        {
            // Assert.Ignore, not Assume: UTR reports Inconclusive as a failure, and PRV projects run in Gamma.
            if (QualitySettings.activeColorSpace != ColorSpace.Linear)
                Assert.Ignore("Only meaningful in a Linear color-space project, where SetColor linearizes.");

            var (graph, _) = NewFilterGraph();

            var gray = new Color(0.5f, 0.5f, 0.5f, 1f);
            var colorProperty = new ColorShaderProperty { displayName = "Tint", generatePropertyBlock = true, value = gray };
            graph.AddGraphInput(colorProperty);

            var definition = FilterFunctionDefinitionGenerator.Generate(graph, NewDummyMaterial(), "TestFilter");

            var liveFunction = new FilterFunction(definition);
            liveFunction.AddParameter(new FilterParameter(gray));

            var mpb = new MaterialPropertyBlock();
            definition.passes[0].applySettingsCallback(mpb, MakeContext(liveFunction, readsGamma: true));

            var stored = mpb.GetVector(colorProperty.referenceName);
            Assert.AreEqual(0.5f, stored.x, 1e-3f,
                "A gamma-reading pass must receive the raw sRGB value, not a linearized one.");
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
