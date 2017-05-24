using UnityEngine.Graphing;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.MaterialGraph
{
    [Title("UV/Tri-Planar Mapping")]
    public class UVTriPlanar : Function1Input, IGeneratesFunction, IMayRequireNormal, IMayRequireWorldPosition
    {

        private const string kTextureSlotName = "Texture";

        protected override string GetFunctionName()
        {
            return "unity_triplanar_" + precision;
        }

        protected override MaterialSlot GetInputSlot()
        {
            return new MaterialSlot(InputSlotId, kTextureSlotName, kTextureSlotName, SlotType.Input, SlotValueType.sampler2D, Vector4.zero, false);
        }

        protected override MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, kTextureSlotName, kOutputSlotShaderName, SlotType.Output, SlotValueType.Vector4, Vector4.zero);
        }

        public UVTriPlanar()
        {
            name = "UVTriPlanar";
            UpdateNodeAfterDeserialization();
        }

        protected override string GetFunctionPrototype(string argName)
        {
            return "inline " + precision + outputDimension + " " + GetFunctionName() + " ("
                   + "sampler2D " + argName + ", float3 normal, float3 pos)";
        }

        protected override string GetFunctionCallBody(string inputValue)
        {
            return GetFunctionName() + " (" + inputValue + "_Uniform" + ", IN.worldNormal, IN.worldPos)";
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            base.GeneratePropertyUsages(visitor, generationMode);
        }

        //TODO:Externalize
        //Reference code from:http://www.chilliant.com/rgb2hsv.html
        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();

            var textureSlot = FindInputSlot<MaterialSlot>(InputSlotId);
            if (textureSlot == null)
                return;

            var textureName = "";

            var edges = owner.GetEdges(textureSlot.slotReference).ToList();
            if (edges.Count > 0)
            {
                var edge = edges[0];
                var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(edge.outputSlot.nodeGuid);
                textureName = ShaderGenerator.AdaptNodeOutput(fromNode, edge.outputSlot.slotId, ConcreteSlotValueType.sampler2D, true);
            }
              

            ////////////////////////////////QQQ: Any better way of getting "_Uniform" at the end? //////////////////////////////////
           // outputString.AddShaderChunk("sampler2D " + textureName + "_Uniform;", false);
           // outputString.AddShaderChunk("", false);

            outputString.AddShaderChunk(GetFunctionPrototype("arg"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();


            // use absolute value of normal as texture weights
            outputString.AddShaderChunk("half3 blend = abs(normal);", false);
            // make sure the weights sum up to 1 (divide by sum of x+y+z)
            outputString.AddShaderChunk("blend /= dot(blend, 1.0);", false);


            // read the three texture projections, for x,y,z axes
            outputString.AddShaderChunk("fixed4 cx = tex2D(arg, pos.yz);", false);
            outputString.AddShaderChunk("fixed4 cy = tex2D(arg, pos.xz);", false);
            outputString.AddShaderChunk("fixed4 cz = tex2D(arg, pos.xy);", false);


            // blend the textures based on weights
            outputString.AddShaderChunk("fixed4 c = cx * blend.x + cy * blend.y + cz * blend.z;", false);
            
            outputString.AddShaderChunk("return " + "c;", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

        public bool RequiresNormal()
        {
            return true;
        }

        public bool RequiresWorldPosition()
        {
            return true;
        }
    }
}
