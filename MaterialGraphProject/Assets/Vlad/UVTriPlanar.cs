using UnityEngine.Graphing;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.MaterialGraph
{
    [Title("UV/Tri-Planar Mapping")]
    public class UVTriPlanar : Function2Input, IGeneratesFunction, IMayRequireNormal, IMayRequireWorldPosition
    {

        private const string kTextureSlotName = "Texture";
        private const string kBlendSlotName = "Blend";

        protected override string GetFunctionName()
        {
            return "unity_triplanar_" + precision;
        }

        protected override MaterialSlot GetInputSlot1()
        {
            return new MaterialSlot(InputSlot1Id, kTextureSlotName, kTextureSlotName, SlotType.Input, SlotValueType.sampler2D, Vector4.zero, false);
        }

        protected override MaterialSlot GetInputSlot2()
        {
            return new MaterialSlot(InputSlot2Id, kBlendSlotName, kBlendSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.one);
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

		public override bool hasPreview
		{
			get { return true; }
		}

		public override PreviewMode previewMode
		{
			get
			{
				return PreviewMode.Preview3D;
			}
		}

		protected override string GetFunctionPrototype(string arg1Name, string arg2Name)
        {
            return "inline " + precision + outputDimension + " " + GetFunctionName() + " ("
                   + "sampler2D " + arg1Name + ", " + precision + " " + arg2Name + ", float3 normal, float3 pos)";
        }

        protected override string GetFunctionCallBody(string input1Value, string input2Value)
        {
            return GetFunctionName() + " (" + input1Value + ", " + input2Value + ", IN.worldNormal, IN.worldPos)";
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

            var textureSlot = FindInputSlot<MaterialSlot>(InputSlot1Id);
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
              

            outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();


            // use absolute value of normal as texture weights
            outputString.AddShaderChunk("half3 blend = abs(normal);", false);

            //control the influence of the blend
            outputString.AddShaderChunk("blend = lerp(0, 1, arg2);", false);

            // make sure the weights sum up to 1 (divide by sum of x+y+z)
            outputString.AddShaderChunk("blend /= dot(blend, 1.0);", false);

            // read the three texture projections, for x,y,z axes
            outputString.AddShaderChunk("fixed4 cx = tex2D(arg1, pos.yz);", false);
            outputString.AddShaderChunk("fixed4 cy = tex2D(arg1, pos.xz);", false);
            outputString.AddShaderChunk("fixed4 cz = tex2D(arg1, pos.xy);", false);


            // blend the textures based on weights
            outputString.AddShaderChunk("fixed4 c = cx * blend.x + cy * blend.y + cz * blend.z;", false);
            
            outputString.AddShaderChunk("return " + "c;", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

        //prevent validation errors when a sampler2D input is missing
        //use on any input requiring a TextureAssetNode
        public override void ValidateNode()
        {
            base.ValidateNode();
            var slot = FindInputSlot<MaterialSlot>(InputSlot1Id);
            if (slot == null)
                return;

            var edges = owner.GetEdges(slot.slotReference).ToList();
            hasError |= edges.Count == 0;
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
