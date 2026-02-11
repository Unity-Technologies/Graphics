using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal interface IBuiltInToURP2dMaterialUpgrader
    {
    }

    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    class BuiltInToURP2DMaterialUpgraderProvider : IMaterialUpgradersProvider
    {
        public IEnumerable<MaterialUpgrader> GetUpgraders()
        {
            yield return new SpritesDefaultUpgrader("Sprites-Default");
            yield return new SpritesDefaultUpgrader("Sprites-Mask");
            yield return new DefaultMaterialUpgrader();
        }
    }

    class DefaultMaterialUpgrader : MaterialUpgrader, IBuiltInToURP2dMaterialUpgrader
    {
        public DefaultMaterialUpgrader()
        {
            RenameShader("Standard", "Universal Render Pipeline/2D/Mesh2D-Lit-Default");
        }
    }

    class SpritesDefaultUpgrader : MaterialUpgrader, IBuiltInToURP2dMaterialUpgrader
    {
        public SpritesDefaultUpgrader(string shaderName)
        {
            if (shaderName.Equals("Sprites-Default"))
            {
                Shader newShader = null;
                Renderer2DData data = Light2DEditorUtility.GetRenderer2DData();
                if (data != null)
                    newShader = data.GetDefaultMaterial(DefaultMaterialType.Sprite).shader;
                else
                    newShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");

                RenameShader("Sprites/Diffuse", newShader.name);
            }
            else if (shaderName.Equals("Sprites-Mask"))
            {
                RenameShader("Sprites/Mask", "Universal Render Pipeline/2D/Sprite-Mask");
            }
        }
    }

    internal interface IURP3DToURP2dMaterialUpgrader
    {
    }

    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    class URP3DToURP2DMaterialUpgraderProvider : IMaterialUpgradersProvider
    {
        public IEnumerable<MaterialUpgrader> GetUpgraders()
        {
            yield return new LitMaterialUpgrader();
            yield return new SimpleLitMaterialUpgrader();
        }
    }

    class LitMaterialUpgrader : MaterialUpgrader, IURP3DToURP2dMaterialUpgrader
    {
        public LitMaterialUpgrader()
        {
            RenameShader("Universal Render Pipeline/Lit", "Universal Render Pipeline/2D/Mesh2D-Lit-Default");
        }
    }

    class SimpleLitMaterialUpgrader : MaterialUpgrader, IURP3DToURP2dMaterialUpgrader
    {
        public SimpleLitMaterialUpgrader()
        {
            RenameShader("Universal Render Pipeline/Simple Lit", "Universal Render Pipeline/2D/Mesh2D-Lit-Default");
        }
    }
}
