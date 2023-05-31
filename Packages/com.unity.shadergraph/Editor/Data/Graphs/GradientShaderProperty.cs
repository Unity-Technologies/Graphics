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
                StringBuilder colors = new("{");
                StringBuilder alphas = new("{");
                for (int i = 0; i < 8; ++i)
                {
                    if (i < value.colorKeys.Length)
                        colors.AppendFormat("{0}4({1},{2},{3},{4})"
                            , concretePrecision.ToShaderString()
                            , NodeUtils.FloatToShaderValue(value.colorKeys[i].color.r)
                            , NodeUtils.FloatToShaderValue(value.colorKeys[i].color.g)
                            , NodeUtils.FloatToShaderValue(value.colorKeys[i].color.b)
                            , NodeUtils.FloatToShaderValue(value.colorKeys[i].time));
                    else colors.AppendFormat("{0}4(0,0,0,0)", concretePrecision.ToShaderString());

                    if (i < value.alphaKeys.Length)
                        alphas.AppendFormat("{0}2({1},{2})"
                            , concretePrecision.ToShaderString()
                            , NodeUtils.FloatToShaderValue(value.alphaKeys[i].alpha)
                            , NodeUtils.FloatToShaderValue(value.alphaKeys[i].time));
                    else alphas.AppendFormat("{0}2(0,0)", concretePrecision.ToShaderString());

                    if (i < 7)
                    {
                        colors.Append(",");
                        alphas.Append(",");
                    }
                    else
                    {
                        colors.Append("}");
                        alphas.Append("}");
                    }
                }

                builder.AppendLine("static Gradient {0} = {{{1},{2},{3},{4},{5}}};"
                    , referenceName
                    , (int)value.mode
                    , value.colorKeys.Length
                    , value.alphaKeys.Length
                    , colors.ToString()
                    , alphas.ToString());
            };

            action(
                new HLSLProperty(HLSLType._CUSTOM, referenceName, HLSLDeclaration.Global, concretePrecision)
                {
                    customDeclaration = customDecl
                });
        }

        internal override string GetPropertyAsArgumentString(string precisionString)
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
                value = value,
            };
        }
    }
}
