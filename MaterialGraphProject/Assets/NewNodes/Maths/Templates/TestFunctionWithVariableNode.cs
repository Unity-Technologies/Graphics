using UnityEngine.Graphing;
using System.Collections.Generic;

namespace UnityEngine.MaterialGraph
{
    [Title("Test/Function With Variable")]
    public class TestFunctionWithVariableNode : AbstractMaterialNode, IGeneratesFunction, IGeneratesBodyCode
    {
        public TestFunctionWithVariableNode()
        {
            name = "FunctionWithVariable";
            UpdateNodeAfterDeserialization(); //from helper
        }
        
        protected string GetFunctionName() //was override from helper
        {
            return "unity_functionwithvariable_" + precision;
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("arg1"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk("return arg1;", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

        /// <summary>
        /// START FROM PROPERTY NODE
        /// </summary>

        [SerializeField]
        private Vector2 m_Value;

        public PropertyType propertyType
        {
            get { return PropertyType.Vector2; }
        }

        [SerializeField]
        private string m_PropertyName = string.Empty;

        [SerializeField]
        private string m_Description = string.Empty;

        public override bool hasPreview { get { return true; } }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);
            properties.Add(GetPreviewProperty());
        }

        public Vector2 value
        {
            get { return m_Value; }
            set
            {
                if (m_Value == value)
                    return;

                m_Value = value;

                if (onModified != null)
                    onModified(this, ModificationScope.Nothing);
            }
        }

        public string description
        {
            get
            {
                if (string.IsNullOrEmpty(m_Description))
                    return m_PropertyName;

                return m_Description;
            }
            set { m_Description = value; }
        }

        public string propertyName
        {
            get
            {
                if (exposedState == PropertyNode.ExposedState.NotExposed || string.IsNullOrEmpty(m_PropertyName))
                    return string.Format("{0}_{1}_Uniform", name, guid.ToString().Replace("-", "_"));

                return m_PropertyName + "_Uniform";
            }
            set { m_PropertyName = value; }
        }

        /// <summary>
        /// START GENERATE MATERIAL PROPERTY
        /// </summary>

        [SerializeField]
        private PropertyNode.ExposedState m_Exposed = PropertyNode.ExposedState.NotExposed;

        public PropertyNode.ExposedState exposedState
        {
            get
            {
                if (owner is SubGraph)
                    return PropertyNode.ExposedState.NotExposed;

                return m_Exposed;
            }
            set
            {
                if (m_Exposed == value)
                    return;

                m_Exposed = value;
            }
        }

        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            if (exposedState == PropertyNode.ExposedState.Exposed)
                visitor.AddShaderProperty(new VectorPropertyChunk(propertyName, description, m_Value, PropertyChunk.HideState.Visible));
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (exposedState == PropertyNode.ExposedState.Exposed || generationMode.IsPreview())
                visitor.AddShaderChunk(precision + "2 " + propertyName + ";", true);
        }

        public PreviewProperty GetPreviewProperty()
        {
            return new PreviewProperty
            {
                m_Name = propertyName,
                m_PropType = PropertyType.Vector2,
                m_Vector4 = m_Value
            };
        }

        /// <summary>
        /// END GENERATE MATERIAL PROPERTY
        /// </summary>
        /// 
        void IGeneratesBodyCode.GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            visitor.AddShaderChunk(precision + "2 " + "THETHING" + " = " + precision + "2 (" + m_Value.x + ", " + m_Value.y + ");", true); //This generates a local variable in the fragment function
        }

        /*void IGeneratesFunction.GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            //if (exposedState == ExposedState.Exposed || generationMode.IsPreview())
            //  return;
            NodeUtils.SlotConfigurationExceptionIfBadConfiguration(this, new[] { InputSlotId }, new[] { OutputSlotId });
            var inputValue = GetSlotValue(InputSlotId, generationMode);
            
            visitor.AddShaderChunk(precision + outputDimension + " " + GetVariableNameForSlot(OutputSlotId) + " = " + GetFunctionCallBody(inputValue) + ";", true);
            ///
        }*/

        /// <summary>
        /// END FROM PROPERTY NODE
        /// </summary>

        /// <summary>
        /// START COPY FROM FUNCTION HELPER
        /// </summary>

        protected const string kInputSlotShaderName = "Input";
        protected const string kOutputSlotShaderName = "Output";

        public const int InputSlotId = 0;
        public const int OutputSlotId = 1;

        protected string GetFunctionPrototype(string argName)
        {
            return "inline " + precision + outputDimension + " " + GetFunctionName() + " ("
                   + precision + inputDimension + " " + argName + ")";
        }

        public string outputDimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType); }
        }
        public string inputDimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType); }
        }

        protected string GetFunctionCallBody(string inputValue)
        {
            return GetFunctionName() + " (" + inputValue + ")";
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(GetInputSlot());
            AddSlot(GetOutputSlot());
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected int[] validSlots
        {
            get { return new[] { InputSlotId, OutputSlotId }; }
        }

        protected virtual MaterialSlot GetInputSlot()
        {
            return new MaterialSlot(InputSlotId, GetInputSlotName(), kInputSlotShaderName, SlotType.Input, SlotValueType.Dynamic, Vector4.zero);
        }

        protected virtual MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, SlotType.Output, SlotValueType.Dynamic, Vector4.zero);
        }

        protected virtual string GetInputSlotName() { return "Input"; }
        protected virtual string GetOutputSlotName() { return "Output"; }

        /// <summary>
        /// END COPY FROM FUNCTION HELPER
        /// </summary>
    }
}
