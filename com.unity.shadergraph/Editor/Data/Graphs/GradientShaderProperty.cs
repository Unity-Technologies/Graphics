using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class GradientShaderProperty : AbstractShaderProperty<Gradient>
    {
        public GradientShaderProperty()
        {
            displayName = "Gradient";
            value = new Gradient();
        }
        
        public override PropertyType propertyType => PropertyType.Gradient;
        
        public override bool isBatchable => false;
        public override bool isExposable => false;
        public override bool isRenamable => true;
        
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
        
        public override AbstractMaterialNode ToConcreteNode()
        {
            return new GradientNode { gradient = value };
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                gradientValue = value
            };
        }

        public override ShaderInput Copy()
        {
            return new GradientShaderProperty
            {
                displayName = displayName,
                hidden = hidden,
                value = value
            };
        }
    }
}
