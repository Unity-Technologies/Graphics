using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal
{
    static class EditorCurveBindingUtils
    {
        private static readonly HashSet<string> k_ColorAttributeSuffixes = new HashSet<string>
        {
            "r", "g", "b", "a"
        };

        private static readonly HashSet<string> k_VectorAttributeSuffixes = new HashSet<string>
        {
            "x", "y", "z", "w"
        };

        // All known texture sub-property suffixes
        private static readonly string[] k_TextureSubProperties = new[]
        {
            "_ST",          // Scale and Tiling (Vector4: x=scaleX, y=scaleY, z=offsetX, w=offsetY)
            "_TexelSize",   // Texel size (Vector4: x=1/width, y=1/height, z=width, w=height)
            "_HDR",         // HDR decode values (Vector4)
            "_Bump"         // Bump map scale (usually Float, but can be part of texture property)
        };

        // Matches material property names in the format:
        // "material._PropertyName" or "material._PropertyName.component"
        // Examples: "material._Color.r", "material._MainTex_ST.x", "material._Metallic"
        private static readonly Regex k_MatchMaterialPropertyName = new(@"^material\.([^.]+)(?:\.([rgba]|[xyzw]))?$", RegexOptions.Compiled);

        internal static (string Name, ShaderPropertyType Type) InferShaderProperty(EditorCurveBinding binding)
        {
            var match = k_MatchMaterialPropertyName.Match(binding.propertyName);
            if (!match.Success)
                return (binding.propertyName, ShaderPropertyType.Float);

            var propertyName = match.Groups[1].Value;
            var componentSuffix = match.Groups[2].Value;

            // Priority 1: Check for texture sub-properties
            var (isTexture, textureName) = CheckForTextureProperty(propertyName, componentSuffix);
            if (isTexture)
            {
                return (textureName, ShaderPropertyType.Texture);
            }

            // Priority 2: Check for color components (r, g, b, a)
            if (k_ColorAttributeSuffixes.Contains(componentSuffix))
            {
                return (propertyName, ShaderPropertyType.Color);
            }

            // Priority 3: Default to float
            return (propertyName, ShaderPropertyType.Float);
        }

        private static (bool isTexture, string baseName) CheckForTextureProperty(string propertyName, string componentSuffix)
        {
            // Check each texture sub-property suffix
            foreach (var subProperty in k_TextureSubProperties)
            {
                // Case 1: Property name ends with the sub-property (e.g., "_MainTex_ST")
                if (propertyName.EndsWith(subProperty))
                {
                    var baseName = propertyName.Substring(0, propertyName.Length - subProperty.Length);
                    return (true, baseName);
                }

                // Case 2: Property contains sub-property and has vector component (e.g., "_MainTex_ST" with ".x")
                int subPropertyIndex = propertyName.IndexOf(subProperty);
                if (subPropertyIndex > 0 && k_VectorAttributeSuffixes.Contains(componentSuffix))
                {
                    var baseName = propertyName.Substring(0, subPropertyIndex);
                    return (true, baseName);
                }
            }

            return (false, propertyName);
        }
    }
}
