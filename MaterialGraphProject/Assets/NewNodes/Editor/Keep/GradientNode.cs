using System.Reflection;
using System.Text;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Procedural/Gradient Editor")]
    public class GradientNode : CodeFunctionNode
    {
        [SerializeField]
        private Gradient m_gradient;

        public Gradient gradient
        {
            get { return m_gradient; }
            set
            {
                if (m_gradient == value)
                    return;

                m_gradient = value;
                if (onModified != null)
                    onModified(this, ModificationScope.Graph);
            }
        }

        public void UpdateGradient()
        {
            if (onModified != null)
            {
                onModified(this, ModificationScope.Graph);
            }

            // Debug.Log("UPDATED GRAPH");
        }

        public GradientNode()
        {
            name = "Gradient";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Gradient", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        string Unity_Gradient(
            [Slot(0, Binding.None)] Vector1 value,
            [Slot(1, Binding.None)] out Vector4 result)
        {
            result = Vector4.zero;


            GradientColorKey[] colorkeys = m_gradient.colorKeys;
            GradientAlphaKey[] alphakeys = m_gradient.alphaKeys;

            //Start
            StringBuilder outputString = new StringBuilder();
            string start = @"
{
";
            outputString.Append(start);
            //Color
            Color c;
            float cp;
            for (int i = 0; i < colorkeys.Length; i++)
            {
                c = colorkeys[i].color;
                cp = colorkeys[i].time;
                outputString.AppendLine(string.Format("\t{{precision}}3 color{0}=float3({1},{2},{3});", i, c.r, c.g, c.b));
                outputString.AppendLine(string.Format("\t{{precision}} colorp{0}={1};", i, cp));
            }

            outputString.AppendLine("\t{precision}3 gradcolor = color0;");

            for (int i = 0; i < colorkeys.Length - 1; i++)
            {
                int j = i + 1;
                outputString.AppendLine(string.Format("\t{{precision}} colorLerpPosition{0}=smoothstep(colorp{0},colorp{1},value);", i, j));
                outputString.AppendLine(string.Format("\tgradcolor = lerp(gradcolor,color{0},colorLerpPosition{1});", j, i));
            }

            //Alpha
            float a;
            float ap;
            for (int i = 0; i < alphakeys.Length; i++)
            {
                a = alphakeys[i].alpha;
                ap = alphakeys[i].time;
                outputString.AppendLine(string.Format("\t{{precision}} alpha{0}={1};", i, a));
                outputString.AppendLine(string.Format("\t{{precision}} alphap{0}={1};", i, ap));
            }

            outputString.AppendLine("\t{precision} gradalpha = alpha0;");

            for (int i = 0; i < alphakeys.Length - 1; i++)
            {
                int j = i + 1;
                outputString.AppendLine(string.Format("\t{{precision}} alphaLerpPosition{0}=smoothstep(alphap{0},alphap{1},value);", i, j));
                outputString.AppendLine(string.Format("\tgradalpha = lerp(gradalpha,alpha{0},alphaLerpPosition{1});", j, i));
            }

            //Result
            outputString.AppendLine("\tresult = float4(gradcolor,gradalpha);");
            outputString.AppendLine("}");

            return outputString.ToString();
        }
    }
}
