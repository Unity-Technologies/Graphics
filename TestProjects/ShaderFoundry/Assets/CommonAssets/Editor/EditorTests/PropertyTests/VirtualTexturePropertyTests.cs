using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using static UnityEditor.ShaderFoundry.VirtualTextureLayerAttribute;
using VirtualTextureLayerData = UnityEditor.ShaderFoundry.UnitTests.VirtualTexturePropertyBlockBuilder.LayerData;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    class VirtualTexturePropertyTests : BlockTestRenderer
    {
        VirtualTexturePropertyBlockBuilder BuildWithNoOverrides(int layerCount, int layerToSample)
        {
            var propBuilder = new VirtualTexturePropertyBlockBuilder(layerCount)
            {
                LayerToSample = layerToSample,
            };
            return propBuilder;
        }

        static string BuildTextureDefaultValueString(string textureName)
        {
            return $"\"{textureName}\" {{}}";
        }

        static string GetTextureDefaultName(VirtualTexturePropertyBlockBuilder propBuilder, int layerIndex)
        {
            var defaultValueExpression = propBuilder.CachedLayers[layerIndex].TextureName;
            if (defaultValueExpression == null)
                return null;

            // Parse the texture name from within the quotes
            var start = defaultValueExpression.IndexOf('"');
            var end = defaultValueExpression.IndexOf('"', start + 1);
            return defaultValueExpression.Substring(start + 1, end - start - 1);
        }

        static List<Color> BuildDefaultColors(int layerCount)
        {
            // Just create some layer values that are spread out
            var layerColors = new List<Color>();
            for (var i = 0; i < layerCount; ++i)
                layerColors.Add(new Color(i, i + 0.25f, i + 0.5f, i + 0.75f));
            return layerColors;
        }

        static List<Texture2D> BuildTexturesFromColors(IEnumerable<Color> colors)
        {
            var textures = new List<Texture2D>();
            foreach (var color in colors)
            {
                var texture = new Texture2D(1, 1, TextureFormat.RGBAFloat, false);
                texture.SetPixel(0, 0, color);
                texture.Apply();
                textures.Add(texture);
            }
            return textures;
        }

        void DestroyTextures(IEnumerable<Texture2D> textures)
        {
            foreach (var texture in textures)
                UnityEngine.Object.DestroyImmediate(texture);
        }

        void SetupMaterial(Material material, int layerCount, VirtualTexturePropertyBlockBuilder propBuilder, List<Texture2D> textures)
        {
            for (var i = 0; i < layerCount; ++i)
            {
                var uniformName = propBuilder.CachedLayers[i].UniformName;
                material.SetTexture(uniformName, textures[i]);
            }
        }

        [TestCase(2, 0)]
        [TestCase(2, 1)]
        public void VirtualTextureProperty_NoDefaultSpecified_LayerIsExpectedColor(int layerCount, int layerToSample)
        {
            var expectedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            var propBuilder = BuildWithNoOverrides(layerCount, layerToSample);
            VirtualTextureProperty_SampleTextureAtLayer_IsExpectedColor(propBuilder, expectedColor, errorThreshold: 1);
        }

        [TestCase(2, 0)]
        [TestCase(2, 1)]
        public void VirtualTextureProperty_PropertyDefaultIsWhite_LayerIsExpectedColor(int layerCount, int layerToSample)
        {
            var expectedColor = new Color(1, 1, 1, 1);
            var propBuilder = BuildWithNoOverrides(layerCount, layerToSample);
            propBuilder.PropertyAttribute.DefaultValue = BuildTextureDefaultValueString("white");
            VirtualTextureProperty_SampleTextureAtLayer_IsExpectedColor(propBuilder, expectedColor, errorThreshold: 1);
        }

        [TestCase(2, 0)]
        [TestCase(2, 1)]
        public void VirtualTextureProperty_ReadTextures_IsExpectedColor(int layerCount, int layerToSample)
        {
            var layerColors = BuildDefaultColors(layerCount);
            var expectedColor = layerColors[layerToSample];
            var propBuilder = BuildWithNoOverrides(layerCount, layerToSample);
            VirtualTextureProperty_SampleTextureAtLayer_IsExpectedColor(propBuilder, layerColors, expectedColor, errorThreshold: 1);
        }

        // Samples the texture at the given layer. Note: This does not set anything on the material
        public void VirtualTextureProperty_SampleTextureAtLayer_IsExpectedColor(VirtualTexturePropertyBlockBuilder propBuilder, Color expectedColor, int errorThreshold = 0)
        {
            var container = CreateContainer();
            var block = propBuilder.Build(container);
            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor, errorThreshold: errorThreshold);
        }

        [Test]
        public void VirtualTextureProperty_TextureTypeDefault()
        {
            var color = new Color(0, 0.25f, 0.5f, 0.75f);
            var layerData = new VirtualTextureLayerData { TextureType = LayerTextureType.Default };
            VirtualTextureProperty_CheckLayerResult(2, 0, layerData, color, color);
        }

        [Test]
        public void VirtualTextureProperty_TextureTypeNormalObjectSpace()
        {
            var color = new Color(0, 0.25f, 0.5f, 0.75f);
            var expectedColor = color.UnpackNormalRGB();
            var layerData = new VirtualTextureLayerData { TextureType = LayerTextureType.NormalObjectSpace };
            VirtualTextureProperty_CheckLayerResult(2, 0, layerData, color, expectedColor);
        }

        [Test]
        public void VirtualTextureProperty_TextureTypeNormalTangentSpace()
        {
            var color = new Color(0, 0.25f, 0.5f, 0.75f);
            var expectedColor = color.UnpackNormalmapRGorAG();
            var layerData = new VirtualTextureLayerData { TextureType = LayerTextureType.NormalTangentSpace };
            VirtualTextureProperty_CheckLayerResult(2, 0, layerData, color, expectedColor);
        }

        [TestCase(4, 0)]
        [TestCase(4, 1)]
        [TestCase(4, 2)]
        [TestCase(4, 3)]
        public void VirtualTextureProperty_OverrideLayerNames_SampleLayerIsExpectedColor(int layerCount, int layerToSample)
        {
            var layerColors = BuildDefaultColors(layerCount);
            var expectedColor = layerColors[layerToSample];

            var propBuilder = BuildWithNoOverrides(layerCount, layerToSample);
            for (var i = 0; i < layerCount; ++i)
            {
                propBuilder.Layers[i] = new VirtualTextureLayerData
                {
                    UniformName = $"_Layer{i}",
                    DisplayName = $"Layer{i}",
                };
            }

            VirtualTextureProperty_SampleTextureAtLayer_IsExpectedColor(propBuilder, layerColors, expectedColor, errorThreshold: 1);
        }

        public void VirtualTextureProperty_CheckLayerResult(int layerCount, int layerIndex, VirtualTextureLayerData layerData, Color layerColor, Color expectedColor)
        {
            var layerColors = BuildDefaultColors(layerCount);
            layerColors[layerIndex] = layerColor;

            var propBuilder = BuildWithNoOverrides(layerCount, layerIndex);
            propBuilder.Layers[layerIndex] = layerData;

            VirtualTextureProperty_SampleTextureAtLayer_IsExpectedColor(propBuilder, layerColors, expectedColor, 2);
        }

        public void VirtualTextureProperty_SampleTextureAtLayer_IsExpectedColor(VirtualTexturePropertyBlockBuilder propBuilder, List<Color> colors, Color expectedColor, int errorThreshold = 0)
        {
            var layerTextures = BuildTexturesFromColors(colors);

            var container = CreateContainer();
            var block = propBuilder.Build(container);

            SetupMaterialDelegate materialSetupDelegate = m => SetupMaterial(m, propBuilder.LayerCount, propBuilder, layerTextures);
            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor, materialSetupDelegate, errorThreshold: errorThreshold);

            DestroyTextures(layerTextures);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void VirtualTextureProperty_OverrideLayerCount_CorrectUniformsDeclared(int layerCount)
        {
            var layerToSample = 0;
            var propBuilder = BuildWithNoOverrides(layerCount, layerToSample);
            var container = CreateContainer();
            var block = propBuilder.Build(container);

            var shader = BuildSimpleSurfaceBlockShaderObject(container, propBuilder.BlockName, block);
            Assert.NotNull(shader);
            var propertyCount = ShaderUtil.GetPropertyCount(shader);
            Assert.AreEqual(layerCount, propertyCount);
            UnityEngine.Object.DestroyImmediate(shader);
        }

        [Test]
        public void VirtualTexture_NoLayerCountSpecified_ResultIsTwoLayers()
        {
            var layerCount = 5;
            var container = CreateContainer();

            var shaderAttributes = new List<ShaderAttribute>();
            shaderAttributes.Add(VirtualTexturePropertyBlockBuilder.BuildVirtualTextureAttribute(container, null));

            var propBuilder = BuildWithNoOverrides(layerCount, 0);
            var block = propBuilder.BuildWithAttributeOverrides(container, shaderAttributes);

            var shader = BuildSimpleSurfaceBlockShaderObject(container, propBuilder.BlockName, block);
            Assert.NotNull(shader);
            var propertyCount = ShaderUtil.GetPropertyCount(shader);
            Assert.AreEqual(2, propertyCount);
            UnityEngine.Object.DestroyImmediate(shader);
        }

        [TestCase("_MyProperty", "MyProperty", "white")]
        [TestCase("_MyProperty", null, null)]
        [TestCase(null, "MyProperty", null)]
        [TestCase(null, null, "white")]
        public void VirtualTextureProperty_OverridePropertyValues_ExpectedDeclarations(string uniformName, string displayName, string textureName)
        {
            var layerCount = 2;
            var layerToSample = 0;
            var propBuilder = BuildWithNoOverrides(layerCount, layerToSample);
            propBuilder.PropertyAttribute.UniformName = uniformName;
            propBuilder.PropertyAttribute.DisplayName = displayName;
            propBuilder.PropertyAttribute.DefaultValue = BuildTextureDefaultValueString(textureName);

            var container = CreateContainer();
            var block = propBuilder.Build(container);
            var shader = BuildSimpleSurfaceBlockShaderObject(container, propBuilder.BlockName, block);
            Assert.NotNull(shader);

            for (var layerIndex = 0; layerIndex < layerCount; ++layerIndex)
            {
                var expectedUniformName = propBuilder.CachedLayers[layerIndex].UniformName;
                var expectedDisplayName = propBuilder.CachedLayers[layerIndex].DisplayName;
                var expectedTextureDefaultName = GetTextureDefaultName(propBuilder, layerIndex);
                ValidateLayerTextureProperty(shader, layerIndex, expectedUniformName, expectedDisplayName, expectedTextureDefaultName);
            }
            UnityEngine.Object.DestroyImmediate(shader);
        }

        [TestCase(0, "_Layer0", "Layer0", "white")]
        [TestCase(1, "_Layer1", "Layer1", "gray")]
        [TestCase(2, "_Layer2", "Layer2", "black")]
        [TestCase(3, "_Layer3", "Layer3", "white")]
        [TestCase(0, null, null, null)]
        public void VirtualTextureProperty_OverrideLayerData_ValidatePropertyDeclarations(int layerIndex, string uniformName, string displayName, string textureName)
        {
            string defaultTextureName = "red";
            var propBuilder = BuildWithNoOverrides(4, layerIndex);
            propBuilder.PropertyAttribute.DefaultValue = BuildTextureDefaultValueString(defaultTextureName);
            // Set the layer override values
            propBuilder.Layers[layerIndex] = new VirtualTextureLayerData
            {
                UniformName = uniformName,
                DisplayName = displayName,
                TextureName = BuildTextureDefaultValueString(textureName),
            };

            var container = CreateContainer();
            var block = propBuilder.Build(container);
            var shader = BuildSimpleSurfaceBlockShaderObject(container, propBuilder.BlockName, block);
            Assert.NotNull(shader);

            for (var i = 0; i < propBuilder.LayerCount; ++i)
            {
                // Only the specified layer should be different. All other layers should have default declarations.
                var expectedUniformName = propBuilder.CachedLayers[i].UniformName;
                var expectedDisplayName = propBuilder.CachedLayers[i].DisplayName;
                var expectedTextureDefaultName = GetTextureDefaultName(propBuilder, i);

                ValidateLayerTextureProperty(shader, i, expectedUniformName, expectedDisplayName, expectedTextureDefaultName);
            }

            UnityEngine.Object.DestroyImmediate(shader);
        }

        static void ValidateLayerTextureProperty(Shader shader, int layerIndex, string expectedUniformName, string expectedDisplayName, string expectedTextureDefaultName)
        {
            var propIndex = shader.FindPropertyIndex(expectedUniformName);
            Assert.AreNotEqual(-1, propIndex, $"Layer {layerIndex}: Failed to find property by name '{expectedUniformName}'.");
            Assert.AreEqual(UnityEngine.Rendering.ShaderPropertyType.Texture, shader.GetPropertyType(propIndex), $"Layer {layerIndex}: Property type didn't match expected.");
            Assert.AreEqual(expectedUniformName, shader.GetPropertyName(propIndex), $"Layer {layerIndex}: Uniform name didn't match expected.");
            Assert.AreEqual(expectedDisplayName, shader.GetPropertyDescription(propIndex), $"Layer {layerIndex}: Display name didn't match expected.");
            Assert.AreEqual(expectedTextureDefaultName, shader.GetPropertyTextureDefaultName(propIndex), $"Layer {layerIndex}: Texture default name didn't match expected.");
            Assert.AreEqual(UnityEngine.Rendering.ShaderPropertyFlags.NoScaleOffset, shader.GetPropertyFlags(propIndex), $"Layer {layerIndex}: Expected flag 'NoScaleOffset' not found on property.");

            var expectedAttributes = new List<string> { $"TextureStack._VirtualTexture({layerIndex})" };
            var propertyAttributes = shader.GetPropertyAttributes(propIndex).ToList();
            foreach (var expectedAttribute in expectedAttributes)
                Assert.AreNotEqual(-1, propertyAttributes.IndexOf(expectedAttribute), $"Layer {layerIndex}: Expected attribute '{expectedAttribute}` not found property.");
        }

        [Test]
        public void VirtualTexture_LayerCountTooLarge_ErrorIsReported()
        {
            var layerCount = 5;
            var expectedError = $"Parameter {VirtualTextureAttribute.LayerCountParamName} at position {0} must be in the range of [0, {VirtualTextureAttribute.MaxLayerCount}).";
            var container = CreateContainer();

            var shaderAttributes = new List<ShaderAttribute>();
            shaderAttributes.Add(VirtualTexturePropertyBlockBuilder.BuildVirtualTextureAttribute(container, layerCount.ToString()));

            var propBuilder = BuildWithNoOverrides(layerCount, 0);
            var block = propBuilder.BuildWithAttributeOverrides(container, shaderAttributes);
            BuildAndExpectException(container, block, propBuilder.BlockName, expectedError);
        }

        [Test]
        public void VirtualTexture_LayerCountIsNegative_ErrorIsReported()
        {
            var layerCount = -1;
            var expectedError = $"Parameter {VirtualTextureAttribute.LayerCountParamName} at position {0} must be in the range of [0, {VirtualTextureAttribute.MaxLayerCount}).";
            var container = CreateContainer();

            var shaderAttributes = new List<ShaderAttribute>();
            shaderAttributes.Add(VirtualTexturePropertyBlockBuilder.BuildVirtualTextureAttribute(container, layerCount.ToString()));

            var propBuilder = BuildWithNoOverrides(layerCount, 0);
            var block = propBuilder.BuildWithAttributeOverrides(container, shaderAttributes);
            BuildAndExpectException(container, block, propBuilder.BlockName, expectedError);
        }

        [Test]
        public void VirtualTexture_LayerAttributeIndexIsNegative_IsError()
        {
            var layerCount = 2;
            var expectedError = $"Parameter {VirtualTextureLayerAttribute.IndexParamName} must be in the range of [{0}, {layerCount}).";
            var container = CreateContainer();

            var shaderAttributes = new List<ShaderAttribute>();
            shaderAttributes.Add(VirtualTexturePropertyBlockBuilder.BuildVirtualTextureAttribute(container, layerCount.ToString()));
            shaderAttributes.Add(VirtualTexturePropertyBlockBuilder.BuildLayerAttribute(container, "-1", null, null, null, null));

            var propBuilder = BuildWithNoOverrides(layerCount, 0);
            var block = propBuilder.BuildWithAttributeOverrides(container, shaderAttributes);
            BuildAndExpectException(container, block, propBuilder.BlockName, expectedError);
        }

        [Test]
        public void VirtualTexture_LayerAttributeIndexIsTooLarge_IsError()
        {
            var layerCount = 2;
            var expectedError = $"Parameter {VirtualTextureLayerAttribute.IndexParamName} must be in the range of [{0}, {layerCount}).";
            var container = CreateContainer();

            var shaderAttributes = new List<ShaderAttribute>();
            shaderAttributes.Add(VirtualTexturePropertyBlockBuilder.BuildVirtualTextureAttribute(container, layerCount.ToString()));
            shaderAttributes.Add(VirtualTexturePropertyBlockBuilder.BuildLayerAttribute(container, layerCount.ToString(), null, null, null, null));

            var propBuilder = BuildWithNoOverrides(layerCount, 0);
            var block = propBuilder.BuildWithAttributeOverrides(container, shaderAttributes);
            BuildAndExpectException(container, block, propBuilder.BlockName, expectedError);
        }

        [Test]
        public void VirtualTexture_LayerAttributeIndexNotSpecified_IsError()
        {
            var layerCount = 2;
            var expectedError = $"{VirtualTextureLayerAttribute.AttributeName}: Required parameter {VirtualTextureLayerAttribute.IndexParamName} was not found.";
            var container = CreateContainer();

            var shaderAttributes = new List<ShaderAttribute>();
            shaderAttributes.Add(VirtualTexturePropertyBlockBuilder.BuildVirtualTextureAttribute(container, null));
            shaderAttributes.Add(VirtualTexturePropertyBlockBuilder.BuildLayerAttribute(container, null, null, null, null, null));

            var propBuilder = BuildWithNoOverrides(layerCount, 0);
            var block = propBuilder.BuildWithAttributeOverrides(container, shaderAttributes);
            BuildAndExpectException(container, block, propBuilder.BlockName, expectedError);
        }

        [Test]
        public void VirtualTexture_LayerDefinedMultipleTimes_IsError()
        {
            var layerCount = 2;
            var layerIndex = 0;
            var expectedError = $"Multiple {VirtualTextureLayerAttribute.AttributeName} attributes with {VirtualTextureLayerAttribute.IndexParamName} of {layerIndex} cannot be specified.";
            var container = CreateContainer();

            var shaderAttributes = new List<ShaderAttribute>();
            shaderAttributes.Add(VirtualTexturePropertyBlockBuilder.BuildVirtualTextureAttribute(container, null));
            shaderAttributes.Add(VirtualTexturePropertyBlockBuilder.BuildLayerAttribute(container, layerIndex.ToString(), null, null, null, null));
            shaderAttributes.Add(VirtualTexturePropertyBlockBuilder.BuildLayerAttribute(container, layerIndex.ToString(), null, null, null, null));

            var propBuilder = BuildWithNoOverrides(layerCount, 0);
            var block = propBuilder.BuildWithAttributeOverrides(container, shaderAttributes);
            BuildAndExpectException(container, block, propBuilder.BlockName, expectedError);
        }

        [Test]
        public void VirtualTexture_LayerAttributeIndexNotInteger_IsError()
        {
            var layerCount = 2;
            var expectedError = $"Parameter {VirtualTextureLayerAttribute.IndexParamName} at position 0 must be an integer.";
            var container = CreateContainer();

            var shaderAttributes = new List<ShaderAttribute>();
            shaderAttributes.Add(VirtualTexturePropertyBlockBuilder.BuildVirtualTextureAttribute(container, null));
            shaderAttributes.Add(VirtualTexturePropertyBlockBuilder.BuildLayerAttribute(container, "Foo", null, null, null, null));

            var propBuilder = BuildWithNoOverrides(layerCount, 0);
            var block = propBuilder.BuildWithAttributeOverrides(container, shaderAttributes);
            BuildAndExpectException(container, block, propBuilder.BlockName, expectedError);
        }

        [Test]
        public void VirtualTexture_LayerAttributeTextureTypeNotValidEnum_IsError()
        {
            const string textureType = "SomeFakeType";
            var layerCount = 2;
            var expectedError = $"Parameter {VirtualTextureLayerAttribute.TextureTypeParamName} at index 1 with value {textureType} must be a valid {typeof(LayerTextureType).Name} enum value.";
            var container = CreateContainer();

            var shaderAttributes = new List<ShaderAttribute>();
            shaderAttributes.Add(VirtualTexturePropertyBlockBuilder.BuildVirtualTextureAttribute(container, null));
            shaderAttributes.Add(VirtualTexturePropertyBlockBuilder.BuildLayerAttribute(container, "0", null, null, null, textureType));

            var propBuilder = BuildWithNoOverrides(layerCount, 0);
            var block = propBuilder.BuildWithAttributeOverrides(container, shaderAttributes);
            BuildAndExpectException(container, block, propBuilder.BlockName, expectedError);
        }

        public void BuildAndExpectException(ShaderContainer container, Block block, string shaderName, string expectedExceptionMessage)
        {
            TestDelegate testDelegate = () => BuildSimpleSurfaceBlockShader(container, shaderName, block);
            var exception = Assert.Throws<System.Exception>(testDelegate);
            Assert.AreEqual(expectedExceptionMessage, exception.Message);
        }
    }
}
