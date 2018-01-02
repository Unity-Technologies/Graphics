using UnityEditor.Graphing;
using System.Collections.Generic;
using System.Linq;
using System;

/*namespace UnityEditor.ShaderGraph
{
    [Title("Art", "Filters", "Convolution")]
    public class ConvolutionFilterNode : Function2Input, IGeneratesFunction, IGeneratesBodyCode, IMayRequireMeshUV
    {
        private const string kUVSlotName = "UV";
        private const string kTextureSlotName = "Texture";
        private const int kNumConvolutionVector4 = 7;

        [SerializeField]
        private Vector4[] m_ConvolutionFilter = new Vector4[kNumConvolutionVector4]
            { new Vector4(1,1,1,1),
              new Vector4(1,1,1,1),
              new Vector4(1,1,1,1),
              new Vector4(1,1,1,1),
              new Vector4(1,1,1,1),
              new Vector4(1,1,1,1),
              new Vector4(1,0,0,25.0f) };

        private void GetPositionInData(int row, int col, out int vectorIndex, out int vectorOffset)
        {
            row = Math.Max(row, 0);
            col = Math.Max(col, 0);

            row = Math.Min(row, 4);
            col = Math.Min(col, 4);

            int valueIndex = col * 5 + row;
            vectorIndex = valueIndex / 4;
            vectorOffset = valueIndex % 4;
        }

        public float GetConvolutionDivisor()
        {
            return m_ConvolutionFilter[6].w;
        }

        public void SetConvolutionDivisor(float value)
        {
            if (value == m_ConvolutionFilter[6].w)
                return;

            m_ConvolutionFilter[6].w = value;

            Dirty(ModificationScope.Node);
        }

        public float GetConvolutionWeight(int row, int col)
        {
            int vectorIndex;
            int vectorOffset;
            GetPositionInData(row, col, out vectorIndex, out vectorOffset);

            switch(vectorOffset)
            {
                case 0: return m_ConvolutionFilter[vectorIndex].x;
                case 1: return m_ConvolutionFilter[vectorIndex].y;
                case 2: return m_ConvolutionFilter[vectorIndex].z;
                default: return m_ConvolutionFilter[vectorIndex].w;
            }
        }

        public void SetConvolutionWeight(int row, int col, float value)
        {
            float prevValue = GetConvolutionWeight(row, col);

            if (value == prevValue)
                return;

            int vectorIndex;
            int vectorOffset;
            GetPositionInData(row, col, out vectorIndex, out vectorOffset);

            switch (vectorOffset)
            {
                case 0: m_ConvolutionFilter[vectorIndex].x = value; break;
                case 1: m_ConvolutionFilter[vectorIndex].y = value; break;
                case 2: m_ConvolutionFilter[vectorIndex].z = value; break;
                default: m_ConvolutionFilter[vectorIndex].w = value; break;
            }

            Dirty(ModificationScope.Node);
        }

        protected override string GetFunctionName()
        {
            return "unity_convolution_" + precision;
        }

        protected override MaterialSlot GetInputSlot1()
        {
            return new MaterialSlot(InputSlot1Id, kTextureSlotName, kTextureSlotName, SlotType.Input, SlotValueType.Texture2D, Vector4.zero, false);
        }

        protected override MaterialSlot GetInputSlot2()
        {
            return new MaterialSlot(InputSlot2Id, kUVSlotName, kUVSlotName, SlotType.Input, SlotValueType.Vector2, Vector4.zero, false);
        }

        protected override MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, kOutputSlotShaderName, kOutputSlotShaderName, SlotType.Output, SlotValueType.Vector4, Vector4.zero);
        }

        public ConvolutionFilterNode()
        {
            name = "Convolution";
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
                return PreviewMode.Preview2D;
            }
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (generationMode.IsPreview())
            {
                string propGuid = GetVariableNameForNode();
                for (int i = 0; i < kNumConvolutionVector4; ++i)
                {
                    visitor.AddShaderChunk(precision + "4 " + GetPropertyName(i, propGuid) + ";", true);
                }
            }
        }

        private string GetPropertyName(int index, string nodeGuid)
        {
            return "convolutionFilter" + index + "_" + nodeGuid;
        }

        public override void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            NodeUtils.SlotConfigurationExceptionIfBadConfiguration(this, new[] { InputSlot1Id, InputSlot2Id }, new[] { OutputSlotId });

            if (!generationMode.IsPreview())
            {
                var propGuid = GetVariableNameForNode();
                for (int i = 0; i < kNumConvolutionVector4; ++i)
                {
                    visitor.AddShaderChunk(precision + "4 " + GetPropertyName(i, propGuid) + "=" + precision + "4 (" + m_ConvolutionFilter[i].x + ", " + m_ConvolutionFilter[i].y + ", " + m_ConvolutionFilter[i].z + ", " + m_ConvolutionFilter[i].w + ");", true);
                }
            }

            string samplerName = GetSlotValue(InputSlot1Id, generationMode);

            //uv
            var uvSlot = FindInputSlot<MaterialSlot>(InputSlot2Id);
            if (uvSlot == null)
                return;

            var baseUV = string.Format("{0}.xy", UVChannel.uv0.GetUVName());
            var uvEdges = owner.GetEdges(uvSlot.slotReference).ToList();
            if (uvEdges.Count > 0)
            {
                var uvEdge = uvEdges[0];
                var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(uvEdge.outputSlot.nodeGuid);
                baseUV = ShaderGenerator.AdaptNodeOutput(fromNode, uvEdge.outputSlot.slotId, ConcreteSlotValueType.Vector2, true);
            }

            //texelSize
            string texelSize = samplerName + "_TexelSize.xy";
            visitor.AddShaderChunk(precision + "4 " + GetVariableNameForSlot(OutputSlotId) + " = " + GetFunctionCallBody(samplerName, baseUV, texelSize) + ";", true);


        }

        protected string GetFunctionCallBody(string samplerName, string baseUv, string texelSize)
        {
            var propGuid = GetVariableNameForNode();

            return GetFunctionName() + " (" + samplerName + ", " + baseUv + ", "
                 + GetPropertyName(0, propGuid) + ", "
                 + GetPropertyName(1, propGuid) + ", "
                 + GetPropertyName(2, propGuid) + ", "
                 + GetPropertyName(3, propGuid) + ", "
                 + GetPropertyName(4, propGuid) + ", "
                 + GetPropertyName(5, propGuid) + ", "
                 + GetPropertyName(6, propGuid) + ", "
                 + texelSize + ")";
        }

        protected override string GetFunctionPrototype(string arg1Name, string arg2Name)
        {
            var propGuid = GetVariableNameForNode();

            return "inline " + precision + "4 " + GetFunctionName() + " ("
                   + "sampler2D " + arg1Name + ", "
                   + precision + "2 " + arg2Name + ", "
                   + precision + "4 weights0,"
                   + precision + "4 weights1,"
                   + precision + "4 weights2,"
                   + precision + "4 weights3,"
                   + precision + "4 weights4,"
                   + precision + "4 weights5,"
                   + precision + "4 weights6, float2 texelSize)";
        }


        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();

            outputString.AddShaderChunk(GetFunctionPrototype("textSampler", "baseUv"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            outputString.AddShaderChunk("fixed4 fetches = fixed4(0,0,0,0);", false);
            outputString.AddShaderChunk("fixed weight = 1;", false);

            string[] channelNames = { ".x", ".y", ".z", ".w" };
            for(int col=0; col < 5; ++col)
            {
                for(int row=0; row < 5; ++row)
                {
                    int valueIndex = col * 5 + row;
                    int vectorIndex = valueIndex / 4;
                    int vectorOffset = valueIndex % 4;
                    outputString.AddShaderChunk("weight = weights" + vectorIndex + channelNames[vectorOffset] + ";", false);
                    outputString.AddShaderChunk("fetches += weight * tex2D(textSampler, baseUv + texelSize * fixed2("+(row-2)+","+(col-2)+"));", false);
                }
            }

            outputString.AddShaderChunk("fetches /= weights6.w;", false);
            outputString.AddShaderChunk("return fetches;", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

        //prevent validation errors when a sampler2D input is missing
        //use on any input requiring a Texture2DNode
        public override void ValidateNode()
        {
            base.ValidateNode();
            var slot = FindInputSlot<MaterialSlot>(InputSlot1Id);
            if (slot == null)
                return;

            var edges = owner.GetEdges(slot.slotReference).ToList();
            hasError |= edges.Count == 0;
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);
            var propGuid = GetVariableNameForNode();
            for (int i = 0; i < kNumConvolutionVector4; ++i)
            {
                properties.Add(new PreviewProperty { m_Name = GetPropertyName(i, propGuid), m_PropType = PropertyType.Vector4, m_Vector4 = m_ConvolutionFilter[i] });
            }
        }

        public bool RequiresMeshUV(UVChannel channel)
        {
            if (channel != UVChannel.uv0)
            {
                return false;
            }

            var uvSlot = FindInputSlot<MaterialSlot>(InputSlot2Id);
            if (uvSlot == null)
                return true;

            var edges = owner.GetEdges(uvSlot.slotReference).ToList();
            return edges.Count == 0;
        }
    }
}*/
