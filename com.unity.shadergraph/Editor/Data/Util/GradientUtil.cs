using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    static class GradientUtils
    {
        public static string GetGradientValue(Gradient gradient, bool inline, string delimiter = ";")
        {
            string colorKeys = "";
            for(int i = 0; i < 8; i++)
            {
                if(i < gradient.colorKeys.Length)
                {
                    colorKeys += string.Format("$precision4({0}, {1}, {2}, {3})"
                        , gradient.colorKeys[i].color.r
                        , gradient.colorKeys[i].color.g
                        , gradient.colorKeys[i].color.b
                        , gradient.colorKeys[i].time);
                }
                else
                    colorKeys += "$precision4(0, 0, 0, 0)";
                if(i < 7)
                    colorKeys += ",";
            }

            string alphaKeys = "";
            for(int i = 0; i < 8; i++)
            {
                if(i < gradient.alphaKeys.Length)
                {
                    alphaKeys += string.Format("$precision2({0}, {1})"
                        , gradient.alphaKeys[i].alpha
                        , gradient.alphaKeys[i].time);
                }
                else
                    alphaKeys += "$precision2(0, 0)";
                if(i < 7)
                    alphaKeys += ",";
            }

            if(inline)
            {
                return string.Format("NewGradient({0}, {1}, {2}, {3}, {4}){5}"
                    , (int)gradient.mode
                    , gradient.colorKeys.Length
                    , gradient.alphaKeys.Length
                    , colorKeys
                    , alphaKeys
                    , delimiter);
            }
            else
            {
                return string.Format("{{{0}, {1}, {2}, {{{3}}}, {{{4}}}}}{5}"
                    , (int)gradient.mode
                    , gradient.colorKeys.Length
                    , gradient.alphaKeys.Length
                    , colorKeys
                    , alphaKeys
                    , delimiter);
            }
        }

        public static string GetGradientForPreview(string name)
        {
            string colorKeys = "";
            for(int i = 0; i < 8; i++)
            {
                colorKeys += string.Format("{0}_ColorKey{1}", name, i);
                if(i < 7)
                    colorKeys += ",";
            }

            string alphaKeys = "";
            for(int i = 0; i < 8; i++)
            {
                alphaKeys += string.Format("{0}_AlphaKey{1}", name, i);
                if(i < 7)
                    alphaKeys += ",";
            }

            return string.Format("NewGradient({0}_Type, {0}_ColorsLength, {0}_AlphasLength, {1}, {2})"
                , name
                , colorKeys
                , alphaKeys);
        }

        public static void GetGradientPropertiesForPreview(PropertyCollector properties, string name, Gradient value)
        {
            properties.AddShaderProperty(new Vector1ShaderProperty()
            {
                overrideReferenceName = string.Format("{0}_Type", name),
                value = (int)value.mode,
                generatePropertyBlock = false
            });

            properties.AddShaderProperty(new Vector1ShaderProperty()
            {
                overrideReferenceName = string.Format("{0}_ColorsLength", name),
                value = value.colorKeys.Length,
                generatePropertyBlock = false
            });

            properties.AddShaderProperty(new Vector1ShaderProperty()
            {
                overrideReferenceName = string.Format("{0}_AlphasLength", name),
                value = value.alphaKeys.Length,
                generatePropertyBlock = false
            });

            for (int i = 0; i < 8; i++)
            {
                properties.AddShaderProperty(new Vector4ShaderProperty()
                {
                    overrideReferenceName = string.Format("{0}_ColorKey{1}", name, i),
                    value = i < value.colorKeys.Length ? GradientUtils.ColorKeyToVector(value.colorKeys[i]) : Vector4.zero,
                    generatePropertyBlock = false
                });
            }

            for (int i = 0; i < 8; i++)
            {
                properties.AddShaderProperty(new Vector2ShaderProperty()
                {
                    overrideReferenceName = string.Format("{0}_AlphaKey{1}", name, i),
                    value = i < value.alphaKeys.Length ? GradientUtils.AlphaKeyToVector(value.alphaKeys[i]) : Vector2.zero,
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
