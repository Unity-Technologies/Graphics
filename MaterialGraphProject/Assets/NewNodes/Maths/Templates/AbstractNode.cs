using System; // Need?
using System.Collections.Generic; // Need?
using System.Linq; // Need?
#if UNITY_EDITOR // Need?
using UnityEditor; // Need?
#endif // Need?
using UnityEngine.Graphing;

/// <summary>
/// EXAMPLE ABSTRACT NODE
/// 
/// Does:
/// - Split Vector 4 to 4 individual channels (Input(0) > Uniform (0) > OutputR & OutputG & OutputB & OutputA)
/// - Combine 4 individual channels to Vector4 (InputR & InputG & InputB & InputA > Uniform1 > Output(0)
/// - Perform Gamma correction [just example maths] within a function (Input1 > Function > Output1)
/// </summary>

namespace UnityEngine.MaterialGraph
{
    [Title("Templates/Absract")] //Hierarchy position in node menu
    public class AbsractNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction //One interface for generating body (uniforms) and one for generating functions
    {
        //// Initiatize the node?
        public AbsractNode()
        {
            name = "Absract"; // Name it (all functions and variables use this, no spaces)
            UpdateNodeAfterDeserialization(); // Add/remove slots after deserialize
        }

        /// <summary>
        /// Define slots (Start)
        /// 
        /// Each must have:
        /// 
        /// - Slot name [protected string]
        /// - Slot ID [public int]
        /// - Initialization in UpdateNodeAfterDeserialization()
        /// - Entry in valid slot check int[]
        /// 
        /// This example contains multiple input and output slots to demonstrate Uniform usage and channel split/combine, remove/add as necessary
        /// </summary>

        //// Slot names
        // Inputs
        protected const string kInputSlotName = "Input";
        protected const string kInputSlot1Name = "Input1";
        // Input components
        protected const string kInputSlotRName = "R";
        protected const string kInputSlotGName = "G";
        protected const string kInputSlotBName = "B";
        protected const string kInputSlotAName = "A";
        // Outputs
        protected const string kOutputSlotName = "Output";
        protected const string kOutputSlot1Name = "Output1";
        // Output components
        protected const string kOutputSlotRName = "R";
        protected const string kOutputSlotGName = "G";
        protected const string kOutputSlotBName = "B";
        protected const string kOutputSlotAName = "A";

        //// Slot IDs (Adjust ints when adding/removing IDs)
        // Inputs
        public const int InputSlotId = 0;
        public const int InputSlot1Id = 1;
        // Input components
        public const int InputSlotRId = 2;
        public const int InputSlotGId = 3;
        public const int InputSlotBId = 4;
        public const int InputSlotAId = 5;
        // Outputs
        public const int OutputSlotId = 6;
        public const int OutputSlot1Id = 7;
        // Output components
        public const int OutputSlotRId = 8;
        public const int OutputSlotGId = 9;
        public const int OutputSlotBId = 10;
        public const int OutputSlotAId = 11;

        //// Add/remove slots after deserialize
        public sealed override void UpdateNodeAfterDeserialization()
        {
            //// Add slots
            // Inputs
            AddSlot(new MaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, SlotValueType.Vector4, Vector4.zero)); // Per slot: References to slot display name, shader output name, Id, In/Out, Type and Initial value
            AddSlot(new MaterialSlot(InputSlot1Id, kInputSlot1Name, kInputSlot1Name, SlotType.Input, SlotValueType.Vector4, Vector4.zero)); 
            // Input components
            AddSlot(new MaterialSlot(InputSlotRId, kInputSlotRName, kInputSlotRName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(InputSlotGId, kInputSlotGName, kInputSlotGName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(InputSlotBId, kInputSlotBName, kInputSlotBName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(InputSlotAId, kInputSlotAName, kInputSlotAName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            // Outputs
            AddSlot(new MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, SlotValueType.Vector4, Vector4.zero));
            AddSlot(new MaterialSlot(OutputSlot1Id, kOutputSlot1Name, kOutputSlot1Name, SlotType.Output, SlotValueType.Vector4, Vector4.zero));
            // Output components
            AddSlot(new MaterialSlot(OutputSlotRId, kOutputSlotRName, kOutputSlotRName, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(OutputSlotGId, kOutputSlotGName, kOutputSlotGName, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(OutputSlotBId, kOutputSlotBName, kOutputSlotBName, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(OutputSlotAId, kOutputSlotAName, kOutputSlotAName, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
            // Remove invalid slots
            RemoveSlotsNameNotMatching(validSlots);
        }

        //// Check for valid slots
        protected int[] validSlots
        {
            get { return new[] { InputSlotId, InputSlot1Id, InputSlotRId, InputSlotGId, InputSlotBId, InputSlotAId, OutputSlotId, OutputSlot1Id, OutputSlotRId, OutputSlotGId, OutputSlotBId, OutputSlotAId }; } //Remove/add as needed
        }

        /// <summary>
        /// Define slots (End)
        /// </summary>

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Slot Functions (Start)
        /// 
        /// - Requirements based on intended use of the slot.
        /// - Examples given below
        /// </summary>

        // Get Slot
        // Used to get a MaterialSlot type reference of a slot
        // Get be used to get current value, default value etc to be used when generating shader code
        // One Instance needed per slot needing to access
        protected virtual MaterialSlot GetInputSlot()
        {
            return new MaterialSlot(InputSlotId, GetInputSlotName(), kInputSlotName, SlotType.Input, SlotValueType.Dynamic, Vector4.zero);
        }

        protected virtual MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(InputSlotId, GetInputSlotName(), kInputSlotName, SlotType.Input, SlotValueType.Dynamic, Vector4.zero);
        }

        // Slot Dimension
        // Convert value type to string
        private string input1Dimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindInputSlot<MaterialSlot>(InputSlot1Id).concreteValueType); }
        }

        public string outputDimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType); }
        }

        public string output1Dimension
        {
            get { return ConvertConcreteSlotValueTypeToString(FindOutputSlot<MaterialSlot>(OutputSlot1Id).concreteValueType); }
        }

        // Get Input Slot Name
        // Used to get the name of a slot needed to access the slot (see GetInputSlot())
        protected virtual string GetInputSlotName() { return "Input"; }

        // Used to get individual channels as slots
        // Heavily modified (complicated) to allow accessing channels when using more than one Uniform
        // See commented version below when using a single Uniform
        public override string GetVariableNameForSlot(int slotId)
        {
            string prop; // Need a temporary string to get the Uniform name as dependant on slot
            string slotOutput;
            MaterialSlot slot = FindSlot<MaterialSlot>(slotId); //Get reference to this slot (used when not creating a Uniform)
            MaterialSlot slot1 = FindSlot<MaterialSlot>(GetOutputSlot().id); //Get reference to output slot(0) (Used to combine channels into output(0) without a Uniform)
            switch (slotId)
            {
                // Inputs
                case InputSlotId: //
                    // Taken from AbstractMaterialNode (used to send data through node without defining a Uniform)
                    if (slot == null)
                        throw new ArgumentException(string.Format("Attempting to use MaterialSlot({0}) on node of type {1} where this slot can not be found", slotId, this), "slotId");
                    prop = GetVariableNameForNode() + "_" + slot.shaderOutputName;
                    slotOutput = "";
                    break;
                case InputSlot1Id:
                    prop = propertyName1;
                    slotOutput = "";
                    break;
                case InputSlotRId:
                    // Taken from AbstractMaterialNode (used to send data through node without defining a Uniform)
                    if (slot == null)
                        throw new ArgumentException(string.Format("Attempting to use MaterialSlot({0}) on node of type {1} where this slot can not be found", slotId, this), "slotId");
                    prop = GetVariableNameForNode() + "_" + slot1.shaderOutputName;
                    slotOutput = ".r";
                    break;
                case InputSlotGId:
                    // Taken from AbstractMaterialNode (used to send data through node without defining a Uniform)
                    if (slot == null)
                        throw new ArgumentException(string.Format("Attempting to use MaterialSlot({0}) on node of type {1} where this slot can not be found", slotId, this), "slotId");
                    prop = GetVariableNameForNode() + "_" + slot1.shaderOutputName;
                    slotOutput = ".g";
                    break;
                case InputSlotBId:
                    // Taken from AbstractMaterialNode (used to send data through node without defining a Uniform)
                    if (slot == null)
                        throw new ArgumentException(string.Format("Attempting to use MaterialSlot({0}) on node of type {1} where this slot can not be found", slotId, this), "slotId");
                    prop = GetVariableNameForNode() + "_" + slot1.shaderOutputName;
                    slotOutput = ".b";
                    break;
                case InputSlotAId:
                    // Taken from AbstractMaterialNode (used to send data through node without defining a Uniform)
                    if (slot == null)
                        throw new ArgumentException(string.Format("Attempting to use MaterialSlot({0}) on node of type {1} where this slot can not be found", slotId, this), "slotId");
                    prop = GetVariableNameForNode() + "_" + slot.shaderOutputName;
                    slotOutput = ".a";
                    break;
                // Outputs
                case OutputSlotId:
                    // Taken from AbstractMaterialNode (used to send data through node without defining a Uniform)
                    if (slot == null)
                        throw new ArgumentException(string.Format("Attempting to use MaterialSlot({0}) on node of type {1} where this slot can not be found", slotId, this), "slotId");
                    prop = GetVariableNameForNode() + "_" + slot.shaderOutputName;
                    slotOutput = "";
                    break;
                case OutputSlot1Id:
                    // Taken from AbstractMaterialNode (used to send data through node without defining a Uniform)
                    if (slot == null)
                        throw new ArgumentException(string.Format("Attempting to use MaterialSlot({0}) on node of type {1} where this slot can not be found", slotId, this), "slotId");
                    prop = GetVariableNameForNode() + "_" + slot.shaderOutputName;
                    //prop = propertyName1;
                    slotOutput = "";
                    break;
                case OutputSlotRId:
                    prop = propertyName;
                    slotOutput = ".r";
                    break;
                case OutputSlotGId:
                    prop = propertyName;
                    slotOutput = ".g";
                    break;
                case OutputSlotBId:
                    prop = propertyName;
                    slotOutput = ".b";
                    break;
                case OutputSlotAId:
                    prop = propertyName;
                    slotOutput = ".a";
                    break;
                default:
                    prop = propertyName;
                    slotOutput = "";
                    break;
            }
            return prop + slotOutput;
        }

        // Simplified version for a single Uniform
        // When using a single Uniform the slot outputs will always be a single channel of that Uniform or that full Uniform
        // By appending channels (.r etc) after the "propertyName" variable on specific slot IDs we can control the outputs
        /*public override string GetVariableNameForSlot(int slotId)
        {
            string slotOutput;
            switch (slotId)
            {
                case OutputSlotRId:
                    propertyName =
                    slotOutput = ".r"; // Return only red channel
                    break;
                case OutputSlotGId:
                    slotOutput = ".g"; // Return only green channel
                    break;
                case OutputSlotBId:
                    slotOutput = ".b"; // Return only blue channel
                    break;
                case OutputSlotAId:
                    slotOutput = ".a"; // Return only alpha channel
                    break; 
                default:
                    slotOutput = ""; // Return full Uniform
                    break;
            }
            return propertyName + slotOutput; //Return Uniform name with appended channel name (or blank)
        }*/

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Variables for shader Uniforms (Start)
        /// 
        /// Each must have:
        /// 
        /// - Property name [serialized private & public get/set]
        /// - Description [serialized private & public get/set]
        /// - Property type (of the desired uniform) [public get]
        /// - Value (of type of desired uniform) [serialized private & public get/set]
        /// - Exposed state [serialized private & public get/set]
        /// 
        /// For changes needed for each new Uniform check comments on Uniform 2
        /// </summary>

        //// Uniform 1 (Input Vector 4 > Output 4 individual channels)

        // PropertyName
        // The name of the generated Uniform (note that it includes name [node name], guid [unique identifier] and "_Uniform" suffix)
        [SerializeField]
        private string m_PropertyName = string.Empty;

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

        // Description
        // The ShaderGUI label of exposed properties (used when not exposed?)
        [SerializeField]
        private string m_Description = string.Empty;

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

        // Property type
        // Defines the concrete type of the Uniform
        public PropertyType propertyType
        {
            get
            {
                return PropertyType.Vector4; //Set Uniform type here
            }
        }

        // Value
        // The C# side variable for the Uniforms value
        [SerializeField]
        private Vector4 m_Value; //Set Uniform type here

        public Vector4 value //Set Uniform type here
        {
            get { return m_Value; }
            set
            {
                if (m_Value == value)
                    return;

                m_Value = value;

                if (onModified != null) //What is this variable?
                    onModified(this, ModificationScope.Node); //And what is this?
            }
        }

        // Exposed state
        // Determines whether the Uniform is exposed to the shader GUI (creates Shader Property)
        [SerializeField]
        private PropertyNode.ExposedState m_Exposed = PropertyNode.ExposedState.NotExposed;

        public PropertyNode.ExposedState exposedState
        {
            get
            {
                return PropertyNode.ExposedState.NotExposed;
                /*if (owner is SubGraph)
                    return PropertyNode.ExposedState.NotExposed;

                return m_Exposed;*/
            }
            set
            {
                m_Exposed = PropertyNode.ExposedState.NotExposed;
                /*if (m_Exposed == value)
                    return;

                m_Exposed = value;*/
            }
        }

        ///------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------///

        //// Uniform 1 (because no number = 0)

        // PropertyName
        // The name of the generated Uniform (note that it includes name [node name], guid [unique identifier] and "_Uniform" suffix)
        [SerializeField]
        private string m_PropertyName1 = string.Empty;

        public string propertyName1
        {
            get
            {
                if (exposedState1 == PropertyNode.ExposedState.NotExposed || string.IsNullOrEmpty(m_PropertyName1))
                    return string.Format("{0}_{1}_Uniform1", name, guid.ToString().Replace("-", "_")); //Add 1 to shader side Uniform name in string here

                return m_PropertyName1 + "_Uniform1"; //Add 1 to shader side Uniform name in string here
            }
            set { m_PropertyName1 = value; }
        }

        // Description
        // What does this actually do? Seems to always contain the name...
        [SerializeField]
        private string m_Description1 = string.Empty;

        public string description1
        {
            get
            {
                if (string.IsNullOrEmpty(m_Description1))
                    return m_PropertyName1;

                return m_Description1;
            }
            set { m_Description1 = value; }
        }

        // Property type
        // Defines the concrete type of the Uniform
        public PropertyType propertyType1
        {
            get
            {
                return PropertyType.Vector4; //Set Uniform type here
            }
        }

        // Value
        // The C# side variable for the Uniforms value
        [SerializeField]
        private Vector4 m_Value1; //Set Uniform type here

        public Vector4 value1 //Set Uniform type here
        {
            get { return m_Value1; }
            set
            {
                if (m_Value1 == value)
                    return;

                m_Value1 = value;

                if (onModified != null) //What is this variable?
                    onModified(this, ModificationScope.Node); //And what is this?
            }
        }

        // Exposed state
        // Determines whether the Uniform is exposed to the shader GUI (creates Shader Property)
        [SerializeField]
        private PropertyNode.ExposedState m_Exposed1 = PropertyNode.ExposedState.NotExposed;

        public PropertyNode.ExposedState exposedState1
        {
            get
            {
                return PropertyNode.ExposedState.NotExposed;
                /*if (owner is SubGraph)
                    return PropertyNode.ExposedState.NotExposed;

                return m_Exposed1;*/
            }
            set
            {
                m_Exposed1 = PropertyNode.ExposedState.NotExposed;
                /*if (m_Exposed1 == value)
                    return;

                m_Exposed1 = value;*/
            }
        }

        /// <summary>
        /// Variables for shader Uniforms (End)
        /// </summary>

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Add Uniforms & Properties to Shader (Start)
        /// </summary>

        // Generate a block of Properties
        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            //// Uniform 0
            if (exposedState == PropertyNode.ExposedState.Exposed) // If exposed
                visitor.AddShaderProperty(new VectorPropertyChunk(propertyName, description, m_Value, PropertyChunk.HideState.Visible)); // Add a property
            //// Uniform 1
            if (exposedState1 == PropertyNode.ExposedState.Exposed) // If exposed
                visitor.AddShaderProperty(new VectorPropertyChunk(propertyName1, description1, m_Value1, PropertyChunk.HideState.Visible)); // Add a property
        }

        // Generate Uniforms for exposed Properties
        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            //// Uniform 0
            if (exposedState == PropertyNode.ExposedState.Exposed) // If exposed
                visitor.AddShaderChunk(precision + "4 " + propertyName + ";", true); // Add a Uniform
            //// Uniform 1
            if (exposedState1 == PropertyNode.ExposedState.Exposed) // If exposed
                visitor.AddShaderChunk(precision + "4 " + propertyName1 + ";", true); // Add a Uniform
        }

        // Generate Frgament Function (?) code
        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            //// Uniform 0
            if (exposedState == PropertyNode.ExposedState.Exposed) //If exposed
              return; // Return (because already added in GeneratePropertyUsages())
            var inputValue = GetSlotValue(InputSlotId, generationMode); // Get the input (0) slot value
            visitor.AddShaderChunk(precision + "4 " + propertyName + " = " + inputValue + ";", false); // Add a Uniform as = input (0) slot value
            //// Uniform 1
            if (exposedState1 == PropertyNode.ExposedState.Exposed) //If exposed
                return; // Return (because already added in GeneratePropertyUsages())
            var inputValue1 = GetSlotValue(InputSlot1Id, generationMode); // Get the input (0) slot value
            visitor.AddShaderChunk(precision + "4 " + propertyName1 + " = " + inputValue1 + ";", false); // Add a Uniform as = input (0) slot value

            string input1Value = GetSlotValue(InputSlot1Id, generationMode);
            visitor.AddShaderChunk(precision + output1Dimension + " " + GetVariableNameForSlot(OutputSlot1Id) + " = " + GetFunctionCallBody(input1Value) + ";", true);

            /*string inputRValue = GetSlotValue(InputSlotRId, generationMode);
            string inputGValue = GetSlotValue(InputSlotGId, generationMode);
            string inputBValue = GetSlotValue(InputSlotBId, generationMode);
            visitor.AddShaderChunk(precision + outputDimension + " " + GetVariableNameForSlot(OutputSlotId) + " = " + GetFunctionCallBody(inputRValue, inputGValue, inputBValue) + ";", true);*/
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Function (Start)
        /// </summary>
        /// <returns></returns>

        protected string GetFunctionName()
        {
            return "unity_abstractfunction_" + precision + "4";
        }

        protected virtual string GetFunctionPrototype(string arg1Name)
        {
            return "inline " + precision + outputDimension + " " + GetFunctionName() + " ("
                   + precision + input1Dimension + " " + arg1Name + ")";
        }

        protected virtual string GetFunctionCallBody(string input1Value)
        {
            return GetFunctionName() + " (" + input1Value + ")";
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("arg1"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk("return pow(arg1, 2.2);", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

        /*protected virtual string GetFunctionPrototype(string arg1Name, string arg2Name, string arg3Name)
        {
            return "inline " + precision + outputDimension + " " + GetFunctionName() + " ("
                   + precision + input1Dimension + " " + arg1Name + ", "
                   + precision + input2Dimension + " " + arg2Name + ", "
                   + precision + input2Dimension + " " + arg3Name + ")";
        }*/

        /*protected virtual string GetFunctionCallBody(string input1Value, string input2Value, string input3Value)
        {
            return GetFunctionName() + " (" + input1Value + ", " + input2Value + ", " +input2Value + ")";
        }*/

        /*public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2", "arg3"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk("return "+precision+"3 (arg1, arg2, arg3);", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }*/

        /// <summary>
        /// Function (End)
        /// </summary>

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Preview (Start)
        /// Doesnt seem to work properly
        /// Investigate
        /// </summary>

        public PreviewProperty GetPreviewProperty() // Override when inheriting from PropertyNode
        {
            return new PreviewProperty
            {
                m_Name = propertyName,
                m_PropType = PropertyType.Vector4,
                m_Vector4 = m_Value
            };
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(GetPreviewProperty());
        }

        /// <summary>
        /// Preview (End)
        /// </summary>
    }
}
