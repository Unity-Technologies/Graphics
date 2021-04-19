using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [BlackboardInputInfo(30)]
    class GradientShaderProperty : AbstractShaderProperty<Gradient>
    {
        public GradientShaderProperty()
        {
            displayName = "Gradient";
            value = new Gradient();
        }

        public override PropertyType propertyType => PropertyType.Gradient;

        internal override bool isExposable => false;
        internal override bool isRenamable => true;

        internal override bool AllowHLSLDeclaration(HLSLDeclaration decl) => false; // disable UI, nothing to choose

        internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
        {
            Action<ShaderStringBuilder> customDecl = (builder) =>
            {
                builder.AppendLine("Gradient {0}_Definition()", referenceName);
                using (builder.BlockScope())
                {
                    string[] colors = new string[8];
                    for (int i = 0; i < colors.Length; i++)
                        colors[i] = string.Format("g.colors[{0}] = {1}4(0, 0, 0, 0);", i, concretePrecision.ToShaderString());
                    for (int i = 0; i < value.colorKeys.Length; i++)
                        colors[i] = string.Format("g.colors[{0}] = {1}4({2}, {3}, {4}, {5});"
                            , i
                            , concretePrecision.ToShaderString()
                            , NodeUtils.FloatToShaderValue(value.colorKeys[i].color.r)
                            , NodeUtils.FloatToShaderValue(value.colorKeys[i].color.g)
                            , NodeUtils.FloatToShaderValue(value.colorKeys[i].color.b)
                            , NodeUtils.FloatToShaderValue(value.colorKeys[i].time));

                    string[] alphas = new string[8];
                    for (int i = 0; i < alphas.Length; i++)
                        alphas[i] = string.Format("g.alphas[{0}] = {1}2(0, 0);", i, concretePrecision.ToShaderString());
                    for (int i = 0; i < value.alphaKeys.Length; i++)
                        alphas[i] = string.Format("g.alphas[{0}] = {1}2({2}, {3});"
                            , i
                            , concretePrecision.ToShaderString()
                            , NodeUtils.FloatToShaderValue(value.alphaKeys[i].alpha)
                            , NodeUtils.FloatToShaderValue(value.alphaKeys[i].time));

                    builder.AppendLine("Gradient g;");
                    builder.AppendLine("g.type = {0};",
                        (int)value.mode);
                    builder.AppendLine("g.colorsLength = {0};",
                        value.colorKeys.Length);
                    builder.AppendLine("g.alphasLength = {0};",
                        value.alphaKeys.Length);

                    for (int i = 0; i < colors.Length; i++)
                        builder.AppendLine(colors[i]);

                    for (int i = 0; i < alphas.Length; i++)
                        builder.AppendLine(alphas[i]);
                    builder.AppendLine("return g;", true);
                }
                builder.AppendIndentation();
                builder.Append("#define {0} {0}_Definition()", referenceName);
            };

            action(
                new HLSLProperty(HLSLType._CUSTOM, referenceName, HLSLDeclaration.Global, concretePrecision)
                {
                    customDeclaration = customDecl
                });
        }

        internal override string GetPropertyAsArgumentString()
        {
            return "Gradient " + referenceName;
        }

        internal override AbstractMaterialNode ToConcreteNode()
        {
            return new GradientNode { gradient = value };
        }

        internal override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                gradientValue = value
            };
        }

        internal override ShaderInput Copy()
        {
            return new GradientShaderProperty
            {
                displayName = displayName,
                hidden = hidden,
                value = value,
                precision = precision
            };
        }
    }
}
