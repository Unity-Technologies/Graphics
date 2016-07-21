using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Art/Combine Node")]
    public class CombineNode : Function2Input, IGeneratesFunction
    {
        public CombineNode()
        {
            name = "CombineNode";
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

        private static readonly string[] kOpNames = {
            "darken", "mul", "cburn", "lburn",
            "lighten", "screen", "cdodge", "ldodge",
            "overlay", "softl", "hardl", "vividl", "linearl", "pinl", "hardmix",
            "diff", "excl", "sub", "div"
        };

        public Operation operation
        {
            get { return m_Operation; }
            set { m_Operation = value; }
        }

        protected override string GetFunctionName() { return "unity_combine_" + kOpNames[(int)m_Operation] + "_" + precision; }

        protected void AddOperationBody(ShaderGenerator visitor, string combineName, string body)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk("inline " + precision + "4 unity_combine_" + combineName + "_" + precision + " (" + precision + "4 arg1, " + precision + "4 arg2)", false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk(body, false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
        
        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
                // Darken group
                AddOperationBody(visitor, kOpNames[(int)Operation.Darken], "return min(arg1, arg2);");
                AddOperationBody(visitor, kOpNames[(int)Operation.Multiply], "return arg1 * arg2;");
                AddOperationBody(visitor, kOpNames[(int)Operation.ColorBurn], "return 1 - (1-arg1)/(arg2+1e-5);");
                AddOperationBody(visitor, kOpNames[(int)Operation.LinearBurn], "return arg1 + arg2 - 1;");

                // Lighten group
                AddOperationBody(visitor, kOpNames[(int)Operation.Lighten], "return max(arg1, arg2);");
                AddOperationBody(visitor, kOpNames[(int)Operation.Screen], "return 1- (1-arg1) * (1-arg2);");
                AddOperationBody(visitor, kOpNames[(int)Operation.ColorDodge], "return arg1/(1-arg2+1e-5);");
                AddOperationBody(visitor, kOpNames[(int)Operation.LinearDodge], "return arg1 + arg2;");

                // Contrast group
                AddOperationBody(visitor, kOpNames[(int)Operation.Overlay], "return (arg1 < 0.5)? arg1*arg2*2: 1-(1-arg1)*(1-arg2)*2;");
                AddOperationBody(visitor, kOpNames[(int)Operation.SoftLight],"return (1-arg1)*arg1*arg2 + arg1*(1- (1-arg1)*(1-arg2));");
                AddOperationBody(visitor, kOpNames[(int)Operation.HardLight], "return (arg2 < 0.5)? arg1*arg2*2: 1-(1-arg1)*(1-arg2)*2;");
                AddOperationBody(visitor, kOpNames[(int)Operation.VividLight],"return (arg2 < 0.5)? 1- (1-arg1)/(2*arg2+1e-5): arg1/(1-2*(arg2-0.5)+1e-5);");
                AddOperationBody(visitor, kOpNames[(int)Operation.LinearLight], "return (arg2 < 0.5)? arg1+(2*arg2)-1: arg1+2*(arg2-0.5);");
                AddOperationBody(visitor, kOpNames[(int)Operation.PinLight], "return (arg2 < 0.5)? min(arg1, 2*arg2): max(arg1, 2*(arg2-0.5));");
                AddOperationBody(visitor, kOpNames[(int)Operation.HardMix], "return (arg2 < 1-arg1)? " + precision + "(0):" + precision + "(1);");

                // Inversion group
                AddOperationBody(visitor, kOpNames[(int)Operation.Difference], "return abs(arg2-arg1);");
                AddOperationBody(visitor, kOpNames[(int)Operation.Exclusion], "return arg1 + arg2 - arg1*arg2*2;");
                AddOperationBody(visitor, kOpNames[(int)Operation.Subtract], "return max(arg2-arg1, 0.0);");
                AddOperationBody(visitor, kOpNames[(int)Operation.Divide], "return arg1 / (arg2+1e-5);");
        }
    }
}
