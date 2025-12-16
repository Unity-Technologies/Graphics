using System.Collections;
using NUnit.Framework;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal.Tools
{
    [TestFixture]
    [Category("Graphics Tools")]
    class EditorCurveBindingsTests
    {
        private static IEnumerable BindingsTestCases()
        {
            // Default -> float
            yield return new TestCaseData(
                "nonMatchingProperty"
            ).Returns((ShaderPropertyType.Float, "nonMatchingProperty"));

            // Floats
            yield return new TestCaseData(
                "material._Metallic"
            ).Returns((ShaderPropertyType.Float, "_Metallic"));

            yield return new TestCaseData(
                "material._Smoothness"
            ).Returns((ShaderPropertyType.Float, "_Smoothness"));

            yield return new TestCaseData(
                "material._BumpScale"
            ).Returns((ShaderPropertyType.Float, "_BumpScale"));

            yield return new TestCaseData(
                "material._OcclusionStrength"
            ).Returns((ShaderPropertyType.Float, "_OcclusionStrength"));

            yield return new TestCaseData(
                "material._Metallic.x"
            ).Returns((ShaderPropertyType.Float, "_Metallic"));

            yield return new TestCaseData(
                "material._Metallic.y"
            ).Returns((ShaderPropertyType.Float, "_Metallic"));

            yield return new TestCaseData(
                "material._Metallic.z"
            ).Returns((ShaderPropertyType.Float, "_Metallic"));

            yield return new TestCaseData(
                "material._Metallic.w"
            ).Returns((ShaderPropertyType.Float, "_Metallic"));

            // Colors
            yield return new TestCaseData(
                "material._Color.r"
            ).Returns((ShaderPropertyType.Color, "_Color"));

            yield return new TestCaseData(
                "material._Color.g"
            ).Returns((ShaderPropertyType.Color, "_Color"));

            yield return new TestCaseData(
                "material._Color.b"
            ).Returns((ShaderPropertyType.Color, "_Color"));

            yield return new TestCaseData(
                "material._Color.a"
            ).Returns((ShaderPropertyType.Color, "_Color"));

            yield return new TestCaseData(
                "material._EmissionColor.r"
            ).Returns((ShaderPropertyType.Color, "_EmissionColor"));

            yield return new TestCaseData(
                "material._EmissionColor.g"
            ).Returns((ShaderPropertyType.Color, "_EmissionColor"));

            yield return new TestCaseData(
                "material._EmissionColor.b"
            ).Returns((ShaderPropertyType.Color, "_EmissionColor"));

            yield return new TestCaseData(
                "material._EmissionColor.a"
            ).Returns((ShaderPropertyType.Color, "_EmissionColor"));

            yield return new TestCaseData(
                "material._SpecColor.r"
            ).Returns((ShaderPropertyType.Color, "_SpecColor"));

            // Textures - _ST (Scale and Tiling)
            yield return new TestCaseData(
                "material._MainTex_ST"
            ).Returns((ShaderPropertyType.Texture, "_MainTex"));

            yield return new TestCaseData(
                "material._MainTex_ST.x"
            ).Returns((ShaderPropertyType.Texture, "_MainTex"));

            yield return new TestCaseData(
                "material._MainTex_ST.y"
            ).Returns((ShaderPropertyType.Texture, "_MainTex"));

            yield return new TestCaseData(
                "material._MainTex_ST.z"
            ).Returns((ShaderPropertyType.Texture, "_MainTex"));

            yield return new TestCaseData(
                "material._MainTex_ST.w"
            ).Returns((ShaderPropertyType.Texture, "_MainTex"));

            yield return new TestCaseData(
                "material._BumpMap_ST"
            ).Returns((ShaderPropertyType.Texture, "_BumpMap"));

            yield return new TestCaseData(
                "material._BumpMap_ST.x"
            ).Returns((ShaderPropertyType.Texture, "_BumpMap"));

            yield return new TestCaseData(
                "material._MetallicGlossMap_ST.z"
            ).Returns((ShaderPropertyType.Texture, "_MetallicGlossMap"));

            yield return new TestCaseData(
                "material._OcclusionMap_ST.w"
            ).Returns((ShaderPropertyType.Texture, "_OcclusionMap"));

            yield return new TestCaseData(
                "material._EmissionMap_ST"
            ).Returns((ShaderPropertyType.Texture, "_EmissionMap"));

            yield return new TestCaseData(
                "material._DetailAlbedoMap_ST.x"
            ).Returns((ShaderPropertyType.Texture, "_DetailAlbedoMap"));

            // Textures - _TexelSize
            yield return new TestCaseData(
                "material._MainTex_TexelSize"
            ).Returns((ShaderPropertyType.Texture, "_MainTex"));

            yield return new TestCaseData(
                "material._MainTex_TexelSize.x"
            ).Returns((ShaderPropertyType.Texture, "_MainTex"));

            yield return new TestCaseData(
                "material._MainTex_TexelSize.y"
            ).Returns((ShaderPropertyType.Texture, "_MainTex"));

            yield return new TestCaseData(
                "material._MainTex_TexelSize.z"
            ).Returns((ShaderPropertyType.Texture, "_MainTex"));

            yield return new TestCaseData(
                "material._MainTex_TexelSize.w"
            ).Returns((ShaderPropertyType.Texture, "_MainTex"));

            yield return new TestCaseData(
                "material._BumpMap_TexelSize"
            ).Returns((ShaderPropertyType.Texture, "_BumpMap"));

            yield return new TestCaseData(
                "material._BumpMap_TexelSize.x"
            ).Returns((ShaderPropertyType.Texture, "_BumpMap"));

            // Textures - _HDR
            yield return new TestCaseData(
                "material._MainTex_HDR"
            ).Returns((ShaderPropertyType.Texture, "_MainTex"));

            yield return new TestCaseData(
                "material._MainTex_HDR.x"
            ).Returns((ShaderPropertyType.Texture, "_MainTex"));

            yield return new TestCaseData(
                "material._MainTex_HDR.y"
            ).Returns((ShaderPropertyType.Texture, "_MainTex"));

            yield return new TestCaseData(
                "material._MainTex_HDR.z"
            ).Returns((ShaderPropertyType.Texture, "_MainTex"));

            yield return new TestCaseData(
                "material._MainTex_HDR.w"
            ).Returns((ShaderPropertyType.Texture, "_MainTex"));

            yield return new TestCaseData(
                "material._EmissionMap_HDR"
            ).Returns((ShaderPropertyType.Texture, "_EmissionMap"));

            yield return new TestCaseData(
                "material._EmissionMap_HDR.x"
            ).Returns((ShaderPropertyType.Texture, "_EmissionMap"));

            // Textures - _Bump (less common, but valid)
            yield return new TestCaseData(
                "material._BumpMap_Bump"
            ).Returns((ShaderPropertyType.Texture, "_BumpMap"));

            yield return new TestCaseData(
                "material._BumpMap_Bump.x"
            ).Returns((ShaderPropertyType.Texture, "_BumpMap"));

            yield return new TestCaseData(
                "material._DetailNormalMap_Bump"
            ).Returns((ShaderPropertyType.Texture, "_DetailNormalMap"));

            // Edge cases - properties with underscores
            yield return new TestCaseData(
                "material._My_Custom_Property"
            ).Returns((ShaderPropertyType.Float, "_My_Custom_Property"));

            yield return new TestCaseData(
                "material._My_Custom_Property.x"
            ).Returns((ShaderPropertyType.Float, "_My_Custom_Property"));

            yield return new TestCaseData(
                "material._My_Custom_Color.r"
            ).Returns((ShaderPropertyType.Color, "_My_Custom_Color"));

            yield return new TestCaseData(
                "material._My_Custom_Texture_ST"
            ).Returns((ShaderPropertyType.Texture, "_My_Custom_Texture"));

            yield return new TestCaseData(
                "material._My_Custom_Texture_ST.x"
            ).Returns((ShaderPropertyType.Texture, "_My_Custom_Texture"));

            // Common URP properties
            yield return new TestCaseData(
                "material._BaseMap_ST"
            ).Returns((ShaderPropertyType.Texture, "_BaseMap"));

            yield return new TestCaseData(
                "material._BaseColor.r"
            ).Returns((ShaderPropertyType.Color, "_BaseColor"));

            yield return new TestCaseData(
                "material._SpecularColor.g"
            ).Returns((ShaderPropertyType.Color, "_SpecularColor"));

            // Common HDRP properties
            yield return new TestCaseData(
                "material._BaseColorMap_ST"
            ).Returns((ShaderPropertyType.Texture, "_BaseColorMap"));

            yield return new TestCaseData(
                "material._NormalMap_ST.x"
            ).Returns((ShaderPropertyType.Texture, "_NormalMap"));

            yield return new TestCaseData(
                "material._MaskMap_ST"
            ).Returns((ShaderPropertyType.Texture, "_MaskMap"));

            yield return new TestCaseData(
                "material._DetailMap_ST.y"
            ).Returns((ShaderPropertyType.Texture, "_DetailMap"));

            // Properties with numbers
            yield return new TestCaseData(
                "material._Texture2D_ST"
            ).Returns((ShaderPropertyType.Texture, "_Texture2D"));

            yield return new TestCaseData(
                "material._Texture2D_ST.x"
            ).Returns((ShaderPropertyType.Texture, "_Texture2D"));

            yield return new TestCaseData(
                "material._Layer0_BaseColor.r"
            ).Returns((ShaderPropertyType.Color, "_Layer0_BaseColor"));

            yield return new TestCaseData(
                "material._Layer1_BaseMap_ST"
            ).Returns((ShaderPropertyType.Texture, "_Layer1_BaseMap"));

            // Vector properties (not color, not texture sub-property)
            yield return new TestCaseData(
                "material._CustomVector.x"
            ).Returns((ShaderPropertyType.Float, "_CustomVector"));

            yield return new TestCaseData(
                "material._CustomVector.y"
            ).Returns((ShaderPropertyType.Float, "_CustomVector"));
        }


        [Test, TestCaseSource(nameof(BindingsTestCases))]
        public (ShaderPropertyType, string) DoTest(string property)
        {
            var binding = new EditorCurveBinding
            {
                propertyName = property
            };
            var (name, type) = EditorCurveBindingUtils.InferShaderProperty(binding);
            return (type, name);
        }
    }
}
