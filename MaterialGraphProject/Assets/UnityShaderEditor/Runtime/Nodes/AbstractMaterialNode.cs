using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public abstract class AbstractMaterialNode : SerializableNode, IGenerateProperties
    {
        [NonSerialized]
        private bool m_HasError;

        public string precision
        {
            get { return "half"; }
        }

        public string[] m_PrecisionNames = { "half" };
       
        // Nodes that want to have a preview area can override this and return true
        public virtual bool hasPreview
        {
            get { return false; }
        }

        public virtual PreviewMode previewMode
        {
            get { return PreviewMode.Preview2D; }
        }

        protected virtual bool generateDefaultInputs
        {
            get { return true; }
        }

        public override bool hasError
        {
            get { return m_HasError; }
            protected set { m_HasError = value; }
        }

        public IEnumerable<MaterialSlot> materialSlots
        {
            get { return slots.OfType<MaterialSlot>(); }
        }

        public IEnumerable<MaterialSlot> materialInputSlots
        {
            get { return inputSlots.OfType<MaterialSlot>(); }
        }

        public IEnumerable<MaterialSlot> materialOuputSlots
        {
            get { return outputSlots.OfType<MaterialSlot>(); }
        }

        protected AbstractMaterialNode(IGraph theOwner) : base(theOwner)
        {
            version = 0;
        }
        
        public virtual void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {}

        public virtual void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode, ConcreteSlotValueType slotValueType)
        {
            if (!generateDefaultInputs)
                return;

            if (!generationMode.IsPreview())
                return;

            foreach (var inputSlot in materialInputSlots)
            {
                var edges = owner.GetEdges(GetSlotReference(inputSlot.name));
                if (edges.Any())
                    continue;

                inputSlot.GeneratePropertyUsages(visitor, generationMode, inputSlot.concreteValueType, this);
            }
        }

        protected string GetSlotValue(MaterialSlot inputSlot, GenerationMode generationMode)
        {
            var edges = owner.GetEdges(GetSlotReference(inputSlot.name)).ToArray();

            if (edges.Length > 0)
            {
                var fromSocketRef = edges[0].outputSlot;
                var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(fromSocketRef.nodeGuid);
                if (fromNode == null)
                    return string.Empty;
               
                var slot = fromNode.FindOutputSlot(fromSocketRef.slotName) as MaterialSlot;
                if (slot == null)
                    return string.Empty;

                return ShaderGenerator.AdaptNodeOutput(fromNode, slot, generationMode, inputSlot.concreteValueType);
            }

            return inputSlot.GetDefaultValue(generationMode, inputSlot.concreteValueType, this);
        }

        public MaterialSlot FindMaterialInputSlot(string name)
        {
            var slot = FindInputSlot(name);
            if (slot == null)
                return null;

            if (slot is MaterialSlot)
                return slot as MaterialSlot;

            Debug.LogErrorFormat("Input Slot: {0} exists but is not of type {1}", name, typeof(MaterialSlot));
            return null;
        }

        public MaterialSlot FindMaterialOutputSlot(string name)
        {
            var slot = FindOutputSlot(name);
            if (slot == null)
                return null;

            if (slot is MaterialSlot)
                return slot as MaterialSlot;

            Debug.LogErrorFormat("Output Slot: {0} exists but is not of type {1}", name, typeof(MaterialSlot));
            return null;
        }
        
        private ConcreteSlotValueType FindCommonChannelType(ConcreteSlotValueType @from, ConcreteSlotValueType to)
        {
            if (ImplicitConversionExists(@from, to))
                return to;

            return ConcreteSlotValueType.Error;
        }

        private static ConcreteSlotValueType ToConcreteType(SlotValueType svt)
        {
            switch (svt)
            {
                case SlotValueType.Vector1:
                    return ConcreteSlotValueType.Vector1;
                case SlotValueType.Vector2:
                    return ConcreteSlotValueType.Vector2;
                case SlotValueType.Vector3:
                    return ConcreteSlotValueType.Vector3;
                case SlotValueType.Vector4:
                    return ConcreteSlotValueType.Vector4;
            }
            return ConcreteSlotValueType.Error;
        }

        private static bool ImplicitConversionExists(ConcreteSlotValueType from, ConcreteSlotValueType to)
        {
            return from >= to || from == ConcreteSlotValueType.Vector1;
        }

        protected virtual ConcreteSlotValueType ConvertDynamicInputTypeToConcrete(IEnumerable<ConcreteSlotValueType> inputTypes)
        {
            var concreteSlotValueTypes = inputTypes as IList<ConcreteSlotValueType> ?? inputTypes.ToList();
            if (concreteSlotValueTypes.Any(x => x == ConcreteSlotValueType.Error))
                return ConcreteSlotValueType.Error;

            var inputTypesDistinct = concreteSlotValueTypes.Distinct().ToList();
            switch (inputTypesDistinct.Count)
            {
                case 0:
                    return ConcreteSlotValueType.Vector1;
                case 1:
                    return inputTypesDistinct.FirstOrDefault();
                default:
                    // find the 'minumum' channel width excluding 1 as it can promote
                    inputTypesDistinct.RemoveAll(x => x == ConcreteSlotValueType.Vector1);
                    var ordered = inputTypesDistinct.OrderBy(x => x);
                    if (ordered.Any())
                        return ordered.FirstOrDefault();
                    break;
            }
            return ConcreteSlotValueType.Error;
        }

        public override void ValidateNode()
        {
            var isInError = false;

            // all children nodes needs to be updated first
            // so do that here
            foreach (var inputSlot in inputSlots)
            {
                var edges = owner.GetEdges(GetSlotReference(inputSlot.name));
                foreach (var edge in edges)
                {
                    var fromSocketRef = edge.outputSlot;
                    var outputNode = owner.GetNodeFromGuid(fromSocketRef.nodeGuid);
                    if (outputNode == null)
                        continue;
                    
                    outputNode.ValidateNode();
                    if (outputNode.hasError)
                        isInError = true;
                }
            }
            
            var dynamicInputSlotsToCompare = new Dictionary<MaterialSlot, ConcreteSlotValueType>();
            var skippedDynamicSlots = new List<MaterialSlot>();

            // iterate the input slots
            foreach (var inputSlot in materialInputSlots)
            {
                var inputType = inputSlot.valueType;
                // if there is a connection
                var edges = owner.GetEdges(GetSlotReference(inputSlot.name)).ToList();
                if (!edges.Any())
                {
                    if (inputType != SlotValueType.Dynamic)
                        inputSlot.concreteValueType = ToConcreteType(inputType);
                    else
                        skippedDynamicSlots.Add(inputSlot);
                    continue;
                }

                // get the output details
                var outputSlotRef = edges[0].outputSlot;
                var outputNode = owner.GetNodeFromGuid(outputSlotRef.nodeGuid);
                if (outputNode == null)
                    continue;

                var outputSlot = outputNode.FindOutputSlot(outputSlotRef.slotName) as MaterialSlot;
                if (outputSlot == null)
                    continue;
         
                var outputConcreteType = outputSlot.concreteValueType;

                // if we have a standard connection... just check the types work!
                if (inputType != SlotValueType.Dynamic)
                {
                    var inputConcreteType = ToConcreteType(inputType);
                    inputSlot.concreteValueType = FindCommonChannelType(outputConcreteType, inputConcreteType);
                    continue;
                }

                // dynamic input... depends on output from other node.
                // we need to compare ALL dynamic inputs to make sure they
                // are compatable.
                dynamicInputSlotsToCompare.Add(inputSlot, outputConcreteType);
            }

            // we can now figure out the dynamic slotType
            // from here set all the 
            var dynamicType = ConvertDynamicInputTypeToConcrete(dynamicInputSlotsToCompare.Values);
            foreach (var dynamicKvP in dynamicInputSlotsToCompare)
                dynamicKvP.Key.concreteValueType= dynamicType;
            foreach (var skippedSlot in skippedDynamicSlots)
                skippedSlot.concreteValueType = dynamicType;

            var inputError = materialInputSlots.Any(x => x.concreteValueType == ConcreteSlotValueType.Error);

            // configure the output slots now
            // their slotType will either be the default output slotType
            // or the above dynanic slotType for dynamic nodes
            // or error if there is an input error
            foreach (var outputSlot in materialOuputSlots)
            {
                if (inputError)
                {
                    outputSlot.concreteValueType = ConcreteSlotValueType.Error;
                    continue;
                }

                if (outputSlot.valueType == SlotValueType.Dynamic)
                {
                    outputSlot.concreteValueType = dynamicType;
                    continue;
                }
                outputSlot.concreteValueType = ToConcreteType(outputSlot.valueType);
            }

            isInError |= inputError;
            isInError |= materialOuputSlots.Any(x => x.concreteValueType == ConcreteSlotValueType.Error);
            isInError |= CalculateNodeHasError();
            hasError = isInError;

            if (!hasError)
            {
                ++version;
            }
        }

        public int version { get; private set; }

        //True if error
        protected virtual bool CalculateNodeHasError()
        {
            return false;
        }

        public static string ConvertConcreteSlotValueTypeToString(ConcreteSlotValueType slotValue)
        {
            switch (slotValue)
            {
                case ConcreteSlotValueType.Vector1:
                    return string.Empty;
                case ConcreteSlotValueType.Vector2:
                    return "2";
                case ConcreteSlotValueType.Vector3:
                    return "3";
                case ConcreteSlotValueType.Vector4:
                    return "4";
                default:
                    return "Error";
            }
        }

        /*
        public virtual bool DrawSlotDefaultInput(Rect rect, MaterialSlot inputSlot)
        {
            var inputSlotType = inputSlot.concreteValueType;
            return inputSlot.OnGUI(rect, inputSlotType);
        }
        
      */

        public virtual void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            var validSlots = materialInputSlots.ToArray();

            for (var index = 0; index < validSlots.Length; index++)
            {
                var s = validSlots[index];
                var edges = owner.GetEdges(GetSlotReference(s.name));
                if (edges.Any())
                    continue;

                var pp = new PreviewProperty
                {
                    m_Name = GetDefaultInputNameForSlot(s),
                    m_PropType = PropertyType.Vector4,
                    m_Vector4 = s.currentValue
                };
                properties.Add(pp);
            }
        }
        
        public virtual string GetOutputVariableNameForSlot(MaterialSlot s)
        {
            if (s.isInputSlot) Debug.LogError("Attempting to use input MaterialSlot (" + s + ") for output!");
            if (!materialSlots.Contains(s)) Debug.LogError("Attempting to use MaterialSlot (" + s + ") for output on a node that does not have this MaterialSlot!");

            return GetVariableNameForNode() + "_" + s.name;
        }

        public virtual string GetDefaultInputNameForSlot(MaterialSlot s)
        {
            if (s.isOutputSlot) Debug.LogError("Attempting to use output MaterialSlot (" + s + ") for default input!");
            if (!materialSlots.Contains(s)) Debug.LogError("Attempting to use MaterialSlot (" + s + ") for default input on a node that does not have this MaterialSlot!");

            return GetVariableNameForNode() + "_" + s.name;
        }

        public virtual string GetVariableNameForNode()
        {
            return name + "_" + guid.ToString().Replace("-", "_");
        }
        
        public sealed override void AddSlot(ISlot slot)
        {
            if (!(slot is MaterialSlot))
            {
                Debug.LogWarningFormat("Trying to add slot {0} to Material node {1}, but it is not a {2}", slot, this, typeof(MaterialSlot));
                return;
            }

            base.AddSlot(slot);

            var addingSlot = (MaterialSlot) slot;
            var foundSlot = (MaterialSlot)slots.FirstOrDefault(x => x.name == slot.name);
            
            // if the default and current are the same, change the current
            // to the new default.
            if (addingSlot.defaultValue == foundSlot.currentValue)
                foundSlot.currentValue = addingSlot.defaultValue;

            foundSlot.defaultValue = addingSlot.defaultValue;
            foundSlot.valueType = addingSlot.valueType;
        }
    }
}
