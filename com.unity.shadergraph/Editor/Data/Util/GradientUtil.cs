using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    static class GradientUtil
    {
        public static string GetGradientValue(Gradient gradient, string delimiter = ";")
        {
            string colorKeys = "";
            for(int i = 0; i < 8; i++)
            {
                if(i < gradient.colorKeys.Length)
                    colorKeys += $"$precision4({gradient.colorKeys[i].color.r}, {gradient.colorKeys[i].color.g}, {gradient.colorKeys[i].color.b}, {gradient.colorKeys[i].time})";
                else
                    colorKeys += "$precision4(0, 0, 0, 0)";
                if(i < 7)
                    colorKeys += ",";
            }

            string alphaKeys = "";
            for(int i = 0; i < 8; i++)
            {
                if(i < gradient.alphaKeys.Length)
                    alphaKeys += $"$precision2({gradient.alphaKeys[i].alpha}, {gradient.alphaKeys[i].time})";
                else
                    alphaKeys += "$precision2(0, 0)";
                if(i < 7)
                    alphaKeys += ",";
            }

            return $"NewGradient({(int)gradient.mode}, {gradient.colorKeys.Length}, {gradient.alphaKeys.Length}, {colorKeys}, {alphaKeys}){delimiter}";
        }

        public static string GetGradientForPreview(string name)
        {
            string colorKeys = "";
            for(int i = 0; i < 8; i++)
            {
                colorKeys += $"{name}_ColorKey{i}";
                if(i < 7)
                    colorKeys += ",";
            }

            string alphaKeys = "";
            for(int i = 0; i < 8; i++)
            {
                alphaKeys += $"{name}_AlphaKey{i}";
                if(i < 7)
                    alphaKeys += ",";
            }

            return $"NewGradient({name}_Type, {name}_ColorsLength, {name}_AlphasLength, {colorKeys}, {alphaKeys})";
        }

        public static void GetGradientPropertiesForPreview(PropertyCollector properties, string name, Gradient value)
        {
            properties.AddShaderProperty(new Vector1ShaderProperty()
            {
                overrideReferenceName = $"{name}_Type",
                value = (int)value.mode,
                generatePropertyBlock = false
            });

            properties.AddShaderProperty(new Vector1ShaderProperty()
            {
                overrideReferenceName = $"{name}_ColorsLength",
                value = value.colorKeys.Length,
                generatePropertyBlock = false
            });

            properties.AddShaderProperty(new Vector1ShaderProperty()
            {
                overrideReferenceName = $"{name}_AlphasLength",
                value = value.alphaKeys.Length,
                generatePropertyBlock = false
            });

            for (int i = 0; i < 8; i++)
            {
                properties.AddShaderProperty(new Vector4ShaderProperty()
                {
                    overrideReferenceName = $"{name}_ColorKey{i}",
                    value = i < value.colorKeys.Length ? GradientUtil.ColorKeyToVector(value.colorKeys[i]) : Vector4.zero,
                    generatePropertyBlock = false
                });
            }

            for (int i = 0; i < 8; i++)
            {
                properties.AddShaderProperty(new Vector2ShaderProperty()
                {
                    overrideReferenceName = $"{name}_AlphaKey{i}",
                    value = i < value.alphaKeys.Length ? GradientUtil.AlphaKeyToVector(value.alphaKeys[i]) : Vector2.zero,
                    generatePropertyBlock = false
                });
            }
        }

        public static bool CheckEquivalency(Gradient A, Gradient B)
        {
            var currentMode = A.mode;
            var currentColorKeys = A.colorKeys;
            var currentAlphaKeys = A.alphaKeys;

            var newMode = B.mode;
            var newColorKeys = B.colorKeys;
            var newAlphaKeys = B.alphaKeys;

            if (currentMode != newMode || currentColorKeys.Length != newColorKeys.Length || currentAlphaKeys.Length != newAlphaKeys.Length)
            {
                return false;
            }
            else
            {
                for (var i = 0; i < currentColorKeys.Length; i++)
                {
                    if (currentColorKeys[i].color != newColorKeys[i].color || Mathf.Abs(currentColorKeys[i].time - newColorKeys[i].time) > 1e-9)
                        return false;
                }

                for (var i = 0; i < currentAlphaKeys.Length; i++)
                {
                    if (Mathf.Abs(currentAlphaKeys[i].alpha - newAlphaKeys[i].alpha) > 1e-9 || Mathf.Abs(currentAlphaKeys[i].time - newAlphaKeys[i].time) > 1e-9)
                        return false;
                }
            }
            return true;
        }

        public static Vector4 ColorKeyToVector(GradientColorKey key)
        {
            return new Vector4(key.color.r, key.color.g, key.color.b, key.time);
        }

        public static Vector2 AlphaKeyToVector(GradientAlphaKey key)
        {
            return new Vector2(key.alpha, key.time);
        }
    }
}
