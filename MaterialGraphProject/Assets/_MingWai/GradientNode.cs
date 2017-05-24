using System.Collections;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Procedural/Gradient Editor")]
    public class GradientNode : Function1Input, IGeneratesFunction
    {
        [SerializeField]
        private Gradient m_gradient;

        
        public Gradient gradient
        {
            get { return m_gradient; }
            set
            {

                if (m_gradient == value)
                {
                    return;
                }

                m_gradient = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
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
            UpdateNodeAfterDeserialization();
        }

        protected override string GetFunctionName()
        {
            return "unity_Gradient_" + precision;
        }

        protected override string GetInputSlotName()
        {
            return "Value";
        }

        protected override MaterialSlot GetInputSlot()
        {
            return new MaterialSlot(InputSlotId, GetInputSlotName(), kInputSlotShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Vector1, Vector2.zero);
        }

        protected override MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, UnityEngine.Graphing.SlotType.Output, SlotValueType.Vector4, Vector2.zero);
        }

        private void GNF(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();

            GradientColorKey[] colorkeys = m_gradient.colorKeys;
            GradientAlphaKey[] alphakeys = m_gradient.alphaKeys;

            //Start

            outputString.AddShaderChunk(GetFunctionPrototype("v"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            //Color

            Color c;
            float cp;
            for (int i = 0; i < colorkeys.Length; i++)
            {
                c = colorkeys[i].color;
                cp = colorkeys[i].time;
                outputString.AddShaderChunk("float3 color" + i + "=float3(" + c.r + "," + c.g + "," + c.b + ");", false);
                outputString.AddShaderChunk("float colorp" + i + "=" + cp + ";", false);
            }

            outputString.AddShaderChunk("float3 gradcolor = color0;", false);

            for (int i = 0; i < colorkeys.Length - 1; i++)
            {
                int j = i + 1;
                outputString.AddShaderChunk("float colorLerpPosition" + i + "=smoothstep(colorp" + i + ",colorp" + j + ",v);", false);
                outputString.AddShaderChunk("gradcolor = lerp(gradcolor,color" + j + ",colorLerpPosition" + i + ");", false);
            }

            //Alpha

            float a;
            float ap;
            for (int i = 0; i < alphakeys.Length; i++)
            {
                a = alphakeys[i].alpha;
                ap = alphakeys[i].time;
                outputString.AddShaderChunk("float alpha" + i + "=" + a + ";", false);
                outputString.AddShaderChunk("float alphap" + i + "=" + ap + ";", false);
            }

            outputString.AddShaderChunk("float gradalpha = alpha0;", false);

            for (int i = 0; i < alphakeys.Length - 1; i++)
            {
                int j = i + 1;
                outputString.AddShaderChunk("float alphaLerpPosition" + i + "=smoothstep(alphap" + i + ",alphap" + j + ",v);", false);
                outputString.AddShaderChunk("gradalpha = lerp(gradalpha,alpha" + j + ",alphaLerpPosition" + i + ");", false);
            }

            //Result

            outputString.AddShaderChunk("return float4(gradcolor,gradalpha);", false);

            //End

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);
            visitor.AddShaderChunk(outputString.GetShaderString(0), true);

            //yield return null;
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            GNF(visitor, generationMode);
            

        }
    }
}
