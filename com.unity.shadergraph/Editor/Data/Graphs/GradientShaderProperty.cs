using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

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
                    colorKeys += $"$precision4({NodeUtils.FloatToShaderValue(gradient.colorKeys[i].color.r)}, " +
                        $"{NodeUtils.FloatToShaderValue(gradient.colorKeys[i].color.g)}, " +
                        $"{NodeUtils.FloatToShaderValue(gradient.colorKeys[i].color.b)}, " +
                        $"{NodeUtils.FloatToShaderValue(gradient.colorKeys[i].time)})";
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
                    alphaKeys += $"$precision2({NodeUtils.FloatToShaderValue(gradient.alphaKeys[i].alpha)}, {NodeUtils.FloatToShaderValue(gradient.alphaKeys[i].time)})";
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

    [Serializable]
    class GradientShaderProperty : AbstractShaderProperty<Gradient>
    {
        public GradientShaderProperty()
        {
            displayName = "Gradient";
            value = new Gradient();
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Gradient; }
        }

        public override Vector4 defaultValue
        {
            get { return new Vector4(); }
        }

        public override bool isBatchable
        {
            get { return false; }
        }

        public override bool isExposable
        {
            get { return false; }
        }

        public override bool isRenamable
        {
            get { return true; }
        }

        public override string GetPropertyBlockString()
        {
            return string.Empty;
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            ShaderStringBuilder s = new ShaderStringBuilder();
            s.AppendLine("Gradient {0}_Definition()", referenceName);
            using (s.BlockScope())
            {
                string[] colors = new string[8];
                for (int i = 0; i < colors.Length; i++)
                    colors[i] = string.Format("g.colors[{0}] = {1}4(0, 0, 0, 0);", i, concretePrecision.ToShaderString());
                for (int i = 0; i < value.colorKeys.Length; i++)
                    colors[i] = string.Format("g.colors[{0}] = {1}4({2}, {3}, {4}, {5});"
                        , i
                        , concretePrecision.ToShaderString()
                        , value.colorKeys[i].color.r
                        , value.colorKeys[i].color.g
                        , value.colorKeys[i].color.b
                        , value.colorKeys[i].time);

                string[] alphas = new string[8];
                for (int i = 0; i < alphas.Length; i++)
                    alphas[i] = string.Format("g.alphas[{0}] = {1}2(0, 0);", i, concretePrecision.ToShaderString());
                for (int i = 0; i < value.alphaKeys.Length; i++)
                    alphas[i] = string.Format("g.alphas[{0}] = {1}2({2}, {3});"
                        , i
                        , concretePrecision.ToShaderString()
                        , value.alphaKeys[i].alpha
                        , value.alphaKeys[i].time);

                s.AppendLine("Gradient g;");
                s.AppendLine("g.type = {0};",
                    (int)value.mode);
                s.AppendLine("g.colorsLength = {0};",
                    value.colorKeys.Length);
                s.AppendLine("g.alphasLength = {0};",
                    value.alphaKeys.Length);

                for (int i = 0; i < colors.Length; i++)
                    s.AppendLine(colors[i]);

                for (int i = 0; i < alphas.Length; i++)
                    s.AppendLine(alphas[i]);
                s.AppendLine("return g;", true);
            }
            s.AppendLine("#define {0} {0}_Definition()", referenceName);
            return s.ToString();
        }

        public override string GetPropertyAsArgumentString()
        {
            return "Gradient " + referenceName;
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(PropertyType.Gradient)
            {
                name = referenceName,
                gradientValue = value
            };
        }

        public override AbstractMaterialNode ToConcreteNode()
        {
            return new GradientNode { gradient = value };
        }

        public override AbstractShaderProperty Copy()
        {
            return new GradientShaderProperty
            {
                value = value
            };
        }
    }
}
