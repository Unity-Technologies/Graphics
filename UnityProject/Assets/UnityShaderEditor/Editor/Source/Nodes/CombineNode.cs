using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Title("Art/Combine Node")]
    class CombineNode : Function2Input, IGeneratesFunction
    {
        public override void OnCreate()
        {
            name = "CombineNode";
            base.OnCreate();
        }

        // Based on information from:
        // http://photoblogstop.com/photoshop/photoshop-blend-modes-explained
        // http://www.venture-ware.com/kevin/coding/lets-learn-math-photoshop-blend-modes/
        // http://mouaif.wordpress.com/2009/01/05/photoshop-math-with-glsl-shaders/
        // http://dunnbypaul.net/blends/
        public enum Operation
        {
            Darken,
            Multiply,
            ColorBurn,
            LinearBurn,
            // TODO: DarkerColor (Darken, but based on luminosity)
            Lighten,
            Screen,
            ColorDodge,
            LinearDodge,
            // TODO: LighterColor (Lighten, but based on luminosity)
            Overlay,
            SoftLight,
            HardLight,
            VividLight,
            LinearLight,
            PinLight,
            HardMix,
            Difference,
            Exclusion,
            Subtract,
            Divide,
        }

        [SerializeField]
        private Operation m_Operation;

        private static readonly string[] kOpNames = new string[] {
            "darken", "mul", "cburn", "lburn",
            "lighten", "screen", "cdodge", "ldodge",
            "overlay", "softl", "hardl", "vividl", "linearl", "pinl", "hardmix",
            "diff", "excl", "sub", "div"
        };

        protected override string GetFunctionName() { return "unity_combine_" + kOpNames[(int)m_Operation] + "_" + precision; }


        protected void AddOperationBody(ShaderGenerator visitor, string combineName, string body, string combinePrecision)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk("inline " + combinePrecision + "4 unity_combine_" + combineName + "_" + combinePrecision + " (" + combinePrecision + "4 arg1, " + combinePrecision + "4 arg2)", false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk(body, false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

       /* public override void NodeUI()
        {
            base.NodeUI();

            EditorGUI.BeginChangeCheck();
            m_Operation = (Operation)EditorGUILayout.EnumPopup(m_Operation);
            if (EditorGUI.EndChangeCheck())
                RegeneratePreviewShaders();
        }*/

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            foreach (var precision in m_PrecisionNames)
            {
                // Darken group
                AddOperationBody(visitor, kOpNames[(int)Operation.Darken], "return min(arg1, arg2);", precision);
                AddOperationBody(visitor, kOpNames[(int)Operation.Multiply], "return arg1 * arg2;", precision);
                AddOperationBody(visitor, kOpNames[(int)Operation.ColorBurn], "return 1 - (1-arg1)/(arg2+1e-5);", precision);
                AddOperationBody(visitor, kOpNames[(int)Operation.LinearBurn], "return arg1 + arg2 - 1;", precision);

                // Lighten group
                AddOperationBody(visitor, kOpNames[(int)Operation.Lighten], "return max(arg1, arg2);", precision);
                AddOperationBody(visitor, kOpNames[(int)Operation.Screen], "return 1- (1-arg1) * (1-arg2);", precision);
                AddOperationBody(visitor, kOpNames[(int)Operation.ColorDodge], "return arg1/(1-arg2+1e-5);", precision);
                AddOperationBody(visitor, kOpNames[(int)Operation.LinearDodge], "return arg1 + arg2;", precision);

                // Contrast group
                AddOperationBody(visitor, kOpNames[(int)Operation.Overlay], "return (arg1 < 0.5)? arg1*arg2*2: 1-(1-arg1)*(1-arg2)*2;", precision);
                AddOperationBody(visitor, kOpNames[(int)Operation.SoftLight],
                    "return (1-arg1)*arg1*arg2 + arg1*(1- (1-arg1)*(1-arg2));", precision);
                AddOperationBody(visitor, kOpNames[(int)Operation.HardLight], "return (arg2 < 0.5)? arg1*arg2*2: 1-(1-arg1)*(1-arg2)*2;", precision);
                AddOperationBody(visitor, kOpNames[(int)Operation.VividLight],
                    "return (arg2 < 0.5)? 1- (1-arg1)/(2*arg2+1e-5): arg1/(1-2*(arg2-0.5)+1e-5);", precision);
                AddOperationBody(visitor, kOpNames[(int)Operation.LinearLight], "return (arg2 < 0.5)? arg1+(2*arg2)-1: arg1+2*(arg2-0.5);", precision);
                AddOperationBody(visitor, kOpNames[(int)Operation.PinLight], "return (arg2 < 0.5)? min(arg1, 2*arg2): max(arg1, 2*(arg2-0.5));", precision);
                AddOperationBody(visitor, kOpNames[(int)Operation.HardMix], "return (arg2 < 1-arg1)? " + precision + "(0):" + precision + "(1);", precision);

                // Inversion group
                AddOperationBody(visitor, kOpNames[(int)Operation.Difference], "return abs(arg2-arg1);", precision);
                AddOperationBody(visitor, kOpNames[(int)Operation.Exclusion], "return arg1 + arg2 - arg1*arg2*2;", precision);
                AddOperationBody(visitor, kOpNames[(int)Operation.Subtract], "return max(arg2-arg1, 0.0);", precision);
                AddOperationBody(visitor, kOpNames[(int)Operation.Divide], "return arg1 / (arg2+1e-5);", precision);
            }
        }
    }
}
