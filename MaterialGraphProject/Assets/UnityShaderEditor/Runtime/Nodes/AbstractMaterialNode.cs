using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public abstract class AbstractMaterialNode : SerializableNode, IGenerateProperties
    {
        public enum OutputPrecision
        {
            @fixed,
            @half,
            @float
        }

        [NonSerialized]
        private bool m_HasError;

        public OutputPrecision precision
        {
            get { return m_OutputPrecision; }
            set { m_OutputPrecision = value; }
        }

        [SerializeField]
        private OutputPrecision m_OutputPrecision = OutputPrecision.half;

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

        protected AbstractMaterialNode()
        {
            version = 0;
        }

        public virtual void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {}

        public virtual void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (!generateDefaultInputs)
                return;

            if (!generationMode.IsPreview())
                return;

            foreach (var inputSlot in GetInputSlots<MaterialSlot>())
            {
                var edges = owner.GetEdges(inputSlot.slotReference);
                if (edges.Any())
                    continue;

                inputSlot.GeneratePropertyUsages(visitor, generationMode);
            }
        }

        public string GetSlotValue(int inputSlotId, GenerationMode generationMode)
        {
            var inputSlot = FindSlot<MaterialSlot>(inputSlotId);
            if (inputSlot == null)
                return string.Empty;

            var edges = owner.GetEdges(inputSlot.slotReference).ToArray();

            if (edges.Any())
            {
                var fromSocketRef = edges[0].outputSlot;
                var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(fromSocketRef.nodeGuid);
                if (fromNode == null)
                    return string.Empty;

                var slot = fromNode.FindOutputSlot<MaterialSlot>(fromSocketRef.slotId);
                if (slot == null)
                    return string.Empty;

                return ShaderGenerator.AdaptNodeOutput(fromNode, slot.id, slot.concreteValueType);
            }

            return inputSlot.GetDefaultValue(generationMode);
        }

        private ConcreteSlotValueType FindCommonChannelType(ConcreteSlotValueType from, ConcreteSlotValueType to)
        {
            if (ImplicitConversionExists(from, to))
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

        private ConcreteSlotValueType ConvertDynamicInputTypeToConcrete(IEnumerable<ConcreteSlotValueType> inputTypes)
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
            foreach (var inputSlot in GetInputSlots<MaterialSlot>())
            {
                var edges = owner.GetEdges(inputSlot.slotReference);
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
            foreach (var inputSlot in GetInputSlots<MaterialSlot>())
            {
                var inputType = inputSlot.valueType;
                // if there is a connection
                var edges = owner.GetEdges(inputSlot.slotReference).ToList();
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

                var outputSlot = outputNode.FindOutputSlot<MaterialSlot>(outputSlotRef.slotId);
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
                dynamicKvP.Key.concreteValueType = dynamicType;
            foreach (var skippedSlot in skippedDynamicSlots)
                skippedSlot.concreteValueType = dynamicType;

            var inputError = GetInputSlots<MaterialSlot>().Any(x => x.concreteValueType == ConcreteSlotValueType.Error);

            // configure the output slots now
            // their slotType will either be the default output slotType
            // or the above dynanic slotType for dynamic nodes
            // or error if there is an input error
            foreach (var outputSlot in GetOutputSlots<MaterialSlot>())
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
            isInError |= GetOutputSlots<MaterialSlot>().Any(x => x.concreteValueType == ConcreteSlotValueType.Error);
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
            var validSlots = GetInputSlots<MaterialSlot>().ToArray();

            for (var index = 0; index < validSlots.Length; index++)
            {
                var s = validSlots[index];
                var edges = owner.GetEdges(s.slotReference);
                if (edges.Any())
                    continue;

                var pp = new PreviewProperty
                {
                    m_Name = GetVariableNameForSlot(s.id),
                    m_PropType = PropertyType.Vector4,
                    m_Vector4 = s.currentValue
                };
                properties.Add(pp);
            }
        }

        public virtual string GetVariableNameForSlot(int slotId)
        {
            var slot = FindSlot<MaterialSlot>(slotId);
            if (slot == null)
                throw new ArgumentException(string.Format("Attempting to use MaterialSlot({0}) on node of type {1} where this slot can not be found", slotId, this), "slotId");

            return GetVariableNameForNode() + "_" + slot.shaderOutputName;
        }

        public virtual string GetVariableNameForNode()
        {
            return name + "_" + guid.ToString().Replace("-", "_");
        }

        public sealed override void AddSlot(ISlot slot)
        {
            if (!(slot is MaterialSlot))
                throw new ArgumentException(string.Format("Trying to add slot {0} to Material node {1}, but it is not a {2}", slot, this, typeof(MaterialSlot)));

            var addingSlot = (MaterialSlot)slot;
            var foundSlot = FindSlot<MaterialSlot>(slot.id);

            // this will remove the old slot and add a new one
            // if an old one was found. This allows updating values
            base.AddSlot(slot);

            if (foundSlot == null)
                return;

            // preserve the old current value.
            addingSlot.currentValue = foundSlot.currentValue;
        }
    }
}
