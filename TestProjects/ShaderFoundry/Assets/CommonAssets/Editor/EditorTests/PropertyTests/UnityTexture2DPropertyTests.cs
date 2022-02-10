using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    class UnityTexture2DPropertyTests : BlockTestRenderer
    {
        UnityTexture2DPropertyBlockBuilder BuildWithoutNameOverrides(string defaultValue)
        {
            var propBuilder = new UnityTexture2DPropertyBlockBuilder
            {
                PropertyAttribute = new PropertyAttributeData { DefaultValue = defaultValue },
            };
            return propBuilder;
        }

        UnityTexture2DPropertyBlockBuilder BuildWithNameOverrides(string defaultValue)
        {
            var propBuilder = new UnityTexture2DPropertyBlockBuilder
            {
                PropertyAttribute = new PropertyAttributeData
                {
                    UniformName = "_Texture",
                    DisplayName = "Texture",
                    DefaultValue = defaultValue
                },
            };
            return propBuilder;
        }

        [UnityTest]
        public IEnumerator Texture2DProperty_DefaultPropertyIsWhite_ExpectedColorIsWhite()
        {
            var expectedColor = new Color(1, 1, 1, 1);

            var container = CreateContainer();
            var propBuilder = BuildWithoutNameOverrides("\"white\" {}");
            var block = propBuilder.Build(container);

            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor);

            yield break;
        }

        [UnityTest]
        public IEnumerator Texture2DProperty_DefaultPropertyIsBlack_ExpectedColorIsBlack()
        {
            var expectedColor = new Color(0, 0, 0, 0);

            var container = CreateContainer();
            var propBuilder = BuildWithoutNameOverrides("\"black\" {}");
            var block = propBuilder.Build(container);

            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor);

            yield break;
        }

        [UnityTest]
        public IEnumerator Texture2DProperty_MaterialTextureSet_IsExpectedColor()
        {
            var expectedColor = new Color(0.25f, 0.5f, 0.75f, 1.0f);
            var inputTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            inputTexture.SetPixel(0, 0, expectedColor);
            inputTexture.Apply();

            var container = CreateContainer();
            var propBuilder = BuildWithNameOverrides("\"\" {}");
            var block = propBuilder.Build(container);

            SetupMaterialDelegate materialSetupDelegate = m => { m.SetTexture(propBuilder.PropertyAttribute.UniformName, inputTexture); };
            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor, materialSetupDelegate, errorThreshold : 1);

            UnityEngine.Object.DestroyImmediate(inputTexture);

            yield break;
        }
    }
}
