using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    class UnityTexture3DPropertyTests : BlockTestRenderer
    {
        UnityTexture3DPropertyBlockBuilder BuildWithoutNameOverrides()
        {
            var propBuilder = new UnityTexture3DPropertyBlockBuilder
            {
                PropertyAttribute = new PropertyAttributeData(),
            };
            return propBuilder;
        }

        UnityTexture3DPropertyBlockBuilder BuildWithNameOverrides()
        {
            var propBuilder = new UnityTexture3DPropertyBlockBuilder
            {
                PropertyAttribute = new PropertyAttributeData
                {
                    UniformName = "_Texture",
                    DisplayName = "Texture",
                },
            };
            return propBuilder;
        }

        [UnityTest]
        public IEnumerator Texture3DProperty_DefaultPropertyValueUsed_IsExpectedColor()
        {
            var expectedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

            var container = CreateContainer();
            var propBuilder = BuildWithoutNameOverrides();
            var block = propBuilder.Build(container);

            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor);

            yield break;
        }

        [UnityTest]
        public IEnumerator Texture3DProperty_MaterialTextureSet_IsExpectedColor()
        {
            var expectedColor = new Color(0.24f, 0.49f, 0.74f, 1.0f);
            var inputTexture = new Texture3D(1, 1, 1, TextureFormat.ARGB32, false);
            inputTexture.SetPixel(0, 0, 0, expectedColor);
            inputTexture.Apply();

            var container = CreateContainer();
            var propBuilder = BuildWithNameOverrides();
            var block = propBuilder.Build(container);

            SetupMaterialDelegate materialSetupDelegate = m => { m.SetTexture(propBuilder.PropertyAttribute.UniformName, inputTexture); };
            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor, materialSetupDelegate);

            UnityEngine.Object.DestroyImmediate(inputTexture);

            yield break;
        }
    }
}
