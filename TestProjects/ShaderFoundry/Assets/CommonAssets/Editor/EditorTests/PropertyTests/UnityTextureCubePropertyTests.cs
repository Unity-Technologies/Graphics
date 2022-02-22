using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    class UnityTextureCubePropertyTests : BlockTestRenderer
    {
        UnityTextureCubePropertyBlockBuilder BuildWithoutNameOverrides()
        {
            var propBuilder = new UnityTextureCubePropertyBlockBuilder
            {
                PropertyAttribute = new PropertyAttributeData(),
            };
            return propBuilder;
        }

        UnityTextureCubePropertyBlockBuilder BuildWithNameOverrides()
        {
            var propBuilder = new UnityTextureCubePropertyBlockBuilder
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
        public IEnumerator TextureCubeProperty_DefaultPropertyValueUsed_IsExpectedColor()
        {
            var expectedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

            var container = CreateContainer();
            var propBuilder = BuildWithoutNameOverrides();
            var block = propBuilder.Build(container);

            TestSurfaceBlockIsConstantColor(container, propBuilder.BlockName, block, expectedColor);

            yield break;
        }

        [UnityTest]
        public IEnumerator TextureCubeProperty_MaterialTextureSet_IsExpectedColor()
        {
            var expectedColor = new Color(0.25f, 0.5f, 0.75f, 1.0f);
            var inputTexture = new Cubemap(1, TextureFormat.ARGB32, false);
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
