using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    class UnityTexture2DArrayPropertyTests : BlockTestRenderer
    {
        UnityTexture2DArrayPropertyBlockBuilder BuildWithoutNameOverrides(string defaultValue)
        {
            var propBuilder = new UnityTexture2DArrayPropertyBlockBuilder
            {
                PropertyAttribute = new PropertyAttributeData { DefaultValue = defaultValue },
            };
            return propBuilder;
        }

        UnityTexture2DArrayPropertyBlockBuilder BuildWithNameOverrides(string defaultValue)
        {
            var propBuilder = new UnityTexture2DArrayPropertyBlockBuilder
            {
                PropertyAttribute = new PropertyAttributeData
                {
                    UniformName = "_Value",
                    DisplayName = "Value",
                    DefaultValue = defaultValue
                },
            };
            return propBuilder;
        }

        [UnityTest]
        public IEnumerator UnityTexture2DArrayProperty_DefaultPropertyValueUsed_IsExpectedColor()
        {
            var expectedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

            var container = CreateContainer();
            var propBuilder = BuildWithoutNameOverrides("\"\" {}");
            propBuilder.SampleIndex = 0;
            var block = propBuilder.Build(container);

            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor, errorThreshold: 1);

            yield break;
        }

        [UnityTest]
        public IEnumerator UnityTexture2DArrayProperty_ReadTexture0_IsExpectedColor()
        {
            var expectedColor = new Color(0.1f, 0.2f, 0.3f, 0.4f);
            var inputTexture = new Texture2DArray(1, 1, 2, TextureFormat.ARGB32, false);
            inputTexture.SetPixels(new Color[] {new Color(0.1f, 0.2f, 0.3f, 0.4f) }, 0);
            inputTexture.SetPixels(new Color[] {new Color(0.5f, 0.6f, 0.7f, 0.8f) }, 1);
            inputTexture.Apply();

            var container = CreateContainer();
            var propBuilder = BuildWithNameOverrides("\"\" {}");
            propBuilder.SampleIndex = 0;
            var block = propBuilder.Build(container);

            SetupMaterialDelegate materialSetupDelegate = m => { m.SetTexture(propBuilder.PropertyAttribute.UniformName, inputTexture); };
            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor, materialSetupDelegate, errorThreshold: 1);

            UnityEngine.Object.DestroyImmediate(inputTexture);

            yield break;
        }

        [UnityTest]
        public IEnumerator UnityTexture2DArrayProperty_ReadTexture1_IsExpectedColor()
        {
            var expectedColor = new Color(0.5f, 0.6f, 0.7f, 0.8f);
            var inputTexture = new Texture2DArray(1, 1, 2, TextureFormat.ARGB32, false);
            inputTexture.SetPixels(new Color[] { new Color(0.1f, 0.2f, 0.3f, 0.4f) }, 0);
            inputTexture.SetPixels(new Color[] { new Color(0.5f, 0.6f, 0.7f, 0.8f) }, 1);
            inputTexture.Apply();

            var container = CreateContainer();
            var propBuilder = BuildWithNameOverrides("\"\" {}");
            propBuilder.SampleIndex = 1;
            var block = propBuilder.Build(container);

            SetupMaterialDelegate materialSetupDelegate = m => { m.SetTexture(propBuilder.PropertyAttribute.UniformName, inputTexture); };
            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor, materialSetupDelegate, errorThreshold: 1);

            UnityEngine.Object.DestroyImmediate(inputTexture);

            yield break;
        }
    }
}
