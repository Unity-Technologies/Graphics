using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Colors;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    abstract class AbstractMaterialNode : ISerializationCallbackReceiver, IGroupItem
    {
        protected static List<MaterialSlot> s_TempSlots = new List<MaterialSlot>();
        protected static List<IEdge> s_TempEdges = new List<IEdge>();
        protected static List<PreviewProperty> s_TempPreviewProperties = new List<PreviewProperty>();

        [NonSerialized]
        private Guid m_Guid;

        [SerializeField]
        private string m_GuidSerialized;

        [NonSerialized]
        Guid m_GroupGuid;

        [SerializeField]
        string m_GroupGuidSerialized;

        [SerializeField]
        private string m_Name;

        [SerializeField]
        protected int m_NodeVersion;

        [SerializeField]
        private DrawState m_DrawState;

        [NonSerialized]
        bool m_HasError;

        [NonSerialized]
        private List<ISlot> m_Slots = new List<ISlot>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableSlots = new List<SerializationHelper.JSONSerializedElement>();

        public Identifier tempId { get; set; }

        public GraphData owner { get; set; }

        OnNodeModified m_OnModified;

        public void RegisterCallback(OnNodeModified callback)
        {
            m_OnModified += callback;
        }

        public void UnregisterCallback(OnNodeModified callback)
        {
            m_OnModified -= callback;
        }

        public void Dirty(ModificationScope scope)
        {
            if (m_OnModified != null)
                m_OnModified(this, scope);
        }

        public Guid guid
        {
            get { return m_Guid; }
        }

        public Guid groupGuid
        {
            get { return m_GroupGuid; }
            set { m_GroupGuid = value; }
        }

        public string name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public virtual string documentationURL => NodeUtils.GetDocumentationString(this);

        public virtual bool canDeleteNode
        {
            get { return owner != null && guid != owner.activeOutputNodeGuid; }
        }

        public DrawState drawState
        {
            get { return m_DrawState; }
            set
            {
                m_DrawState = value;
                Dirty(ModificationScope.Layout);
            }
        }

        public virtual bool canSetPrecision
        {
            get { return true; }
        }

        private ConcretePrecision m_ConcretePrecision = ConcretePrecision.Float;

        public ConcretePrecision concretePrecision
        {
            get => m_ConcretePrecision;
            set => m_ConcretePrecision = value;
        }

        [SerializeField]
        private Precision m_Precision = Precision.Inherit;

        public Precision precision 
        {
            get => m_Precision;
            set => m_Precision = value;
        }

        [SerializeField]
        bool m_PreviewExpanded = true;

        public bool previewExpanded
        {
            get { return m_PreviewExpanded; }
            set
            {
                if (previewExpanded == value)
                    return;
                m_PreviewExpanded = value;
                Dirty(ModificationScope.Node);
            }
        }

        // Nodes that want to have a preview area can override this and return true
        public virtual bool hasPreview
        {
            get { return false; }
        }

        public virtual PreviewMode previewMode
        {
            get { return PreviewMode.Preview2D; }
        }

        public virtual bool allowedInSubGraph
        {
            get { return true; }
        }

        public virtual bool allowedInMainGraph
        {
            get { return true; }
        }

        public virtual bool allowedInLayerGraph
        {
            get { return true; }
        }

        public virtual bool hasError
        {
            get { return m_HasError; }
            protected set { m_HasError = value; }
        }

        string m_DefaultVariableName;
        string m_NameForDefaultVariableName;
        Guid m_GuidForDefaultVariableName;

        string defaultVariableName
        {
            get
            {
                if (m_NameForDefaultVariableName != name || m_GuidForDefaultVariableName != guid)
                {
                    m_DefaultVariableName = string.Format("{0}_{1}", NodeUtils.GetHLSLSafeName(name ?? "node"), GuidEncoder.Encode(guid));
                    m_NameForDefaultVariableName = name;
                    m_GuidForDefaultVariableName = guid;
                }
                return m_DefaultVariableName;
            }
        }

        #region Custom Colors

        [SerializeField]
        CustomColorData m_CustomColors = new CustomColorData();

        public bool TryGetColor(string provider, ref Color color)
        {
            return m_CustomColors.TryGetColor(provider, out color);
        }

        public void ResetColor(string provider)
        {
            m_CustomColors.Remove(provider);
        }

        public void SetColor(string provider, Color color)
        {
            m_CustomColors.Set(provider, color);
        }
        #endregion

        protected AbstractMaterialNode()
        {
            m_DrawState.expanded = true;
            m_Guid = Guid.NewGuid();
            version = 0;
        }

        public Guid RewriteGuid()
        {
            m_Guid = Guid.NewGuid();
            return m_Guid;
        }

        public void GetInputSlots<T>(List<T> foundSlots) where T : ISlot
        {
            foreach (var slot in m_Slots)
            {
                if (slot.isInputSlot && slot is T)
                    foundSlots.Add((T)slot);
            }
        }

        public void GetOutputSlots<T>(List<T> foundSlots) where T : ISlot
        {
            foreach (var slot in m_Slots)
            {
                if (slot.isOutputSlot && slot is T)
                    foundSlots.Add((T)slot);
            }
        }

        public void GetSlots<T>(List<T> foundSlots) where T : ISlot
        {
            foreach (var slot in m_Slots)
            {
                if (slot is T)
                    foundSlots.Add((T)slot);
            }
        }

        public virtual void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            foreach (var inputSlot in this.GetInputSlots<MaterialSlot>())
            {
                var edges = owner.GetEdges(inputSlot.slotReference);
                if (edges.Any())
                    continue;

                inputSlot.AddDefaultProperty(properties, generationMode);
            }
        }

        public string GetSlotValue(int inputSlotId, GenerationMode generationMode, ConcretePrecision concretePrecision)
        {
            string slotValue = GetSlotValue(inputSlotId, generationMode);
            return slotValue.Replace(PrecisionUtil.Token, concretePrecision.ToShaderString());
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

                return ShaderGenerator.AdaptNodeOutput(fromNode, slot.id, inputSlot.concreteValueType);
            }

            return inputSlot.GetDefaultValue(generationMode);
        }
        
        public static ConcreteSlotValueType ConvertDynamicVectorInputTypeToConcrete(IEnumerable<ConcreteSlotValueType> inputTypes)
        {
            var concreteSlotValueTypes = inputTypes as IList<ConcreteSlotValueType> ?? inputTypes.ToList();

            var inputTypesDistinct = concreteSlotValueTypes.Distinct().ToList();
            switch (inputTypesDistinct.Count)
            {
                case 0:
                    return ConcreteSlotValueType.Vector1;
                case 1:
                    if(SlotValueHelper.AreCompatible(SlotValueType.DynamicVector, inputTypesDistinct.First()))
                        return inputTypesDistinct.First();
                    break;
                default:
                    // find the 'minumum' channel width excluding 1 as it can promote
                    inputTypesDistinct.RemoveAll(x => x == ConcreteSlotValueType.Vector1);
                    var ordered = inputTypesDistinct.OrderByDescending(x => x);
                    if (ordered.Any())
                        return ordered.FirstOrDefault();
                    break;
            }
            return ConcreteSlotValueType.Vector1;
        }

        public static ConcreteSlotValueType ConvertDynamicMatrixInputTypeToConcrete(IEnumerable<ConcreteSlotValueType> inputTypes)
        {
            var concreteSlotValueTypes = inputTypes as IList<ConcreteSlotValueType> ?? inputTypes.ToList();

            var inputTypesDistinct = concreteSlotValueTypes.Distinct().ToList();
            switch (inputTypesDistinct.Count)
            {
                case 0:
                    return ConcreteSlotValueType.Matrix2;
                case 1:
                    return inputTypesDistinct.FirstOrDefault();
                default:
                    var ordered = inputTypesDistinct.OrderByDescending(x => x);
                    if (ordered.Any())
                        return ordered.FirstOrDefault();
                    break;
            }
            return ConcreteSlotValueType.Matrix2;
        }

        protected const string k_validationErrorMessage = "Error found during node validation";

        public virtual bool ValidateConcretePrecision(ref string errorMessage)
        {
            // If Node has a precision override use that
            if (precision != Precision.Inherit)
            {
                m_ConcretePrecision = precision.ToConcrete();
                return false;
            }

            // Get inputs
            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);

            // If no inputs were found use the precision of the Graph
            // This can be removed when parameters are considered as true inputs
            if (s_TempSlots.Count == 0)
            {
                m_ConcretePrecision = owner.concretePrecision;
                return false;
            }

            // Otherwise compare precisions from inputs
            var precisionsToCompare = new List<int>();
            bool isInError = false;

            foreach (var inputSlot in s_TempSlots)
            {
                // If input port doesnt have an edge use the Graph's precision for that input
                var edges = owner.GetEdges(inputSlot.slotReference).ToList();
                if (!edges.Any())
                {
                    precisionsToCompare.Add((int)owner.concretePrecision);
                    continue;
                }

                // Get output node from edge
                var outputSlotRef = edges[0].outputSlot;
                var outputNode = owner.GetNodeFromGuid(outputSlotRef.nodeGuid);
                if (outputNode == null)
                {
                    errorMessage = string.Format("Failed to find Node with Guid {0}", outputSlotRef.nodeGuid);
                    isInError = true;
                    continue;
                }

                // Use precision from connected Node
                precisionsToCompare.Add((int)outputNode.concretePrecision);
            }

            // Use highest precision from all input sources
            m_ConcretePrecision = (ConcretePrecision)precisionsToCompare.OrderBy(x => x).First();

            // Clean up
            s_TempSlots.Clear();
            return isInError;
        }

        public virtual void ValidateNode()
        {
            var isInError = false;
            var errorMessage = k_validationErrorMessage;

            var dynamicInputSlotsToCompare = DictionaryPool<DynamicVectorMaterialSlot, ConcreteSlotValueType>.Get();
            var skippedDynamicSlots = ListPool<DynamicVectorMaterialSlot>.Get();

            var dynamicMatrixInputSlotsToCompare = DictionaryPool<DynamicMatrixMaterialSlot, ConcreteSlotValueType>.Get();
            var skippedDynamicMatrixSlots = ListPool<DynamicMatrixMaterialSlot>.Get();

            // iterate the input slots
            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            foreach (var inputSlot in s_TempSlots)
            {
                inputSlot.hasError = false;
                // if there is a connection
                var edges = owner.GetEdges(inputSlot.slotReference).ToList();
                if (!edges.Any())
                {
                    if (inputSlot is DynamicVectorMaterialSlot)
                        skippedDynamicSlots.Add(inputSlot as DynamicVectorMaterialSlot);
                    if (inputSlot is DynamicMatrixMaterialSlot)
                        skippedDynamicMatrixSlots.Add(inputSlot as DynamicMatrixMaterialSlot);
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

                if (outputSlot.hasError)
                {
                    inputSlot.hasError = true;
                    continue;
                }

                var outputConcreteType = outputSlot.concreteValueType;
                // dynamic input... depends on output from other node.
                // we need to compare ALL dynamic inputs to make sure they
                // are compatible.
                if (inputSlot is DynamicVectorMaterialSlot)
                {
                    dynamicInputSlotsToCompare.Add((DynamicVectorMaterialSlot)inputSlot, outputConcreteType);
                    continue;
                }
                else if (inputSlot is DynamicMatrixMaterialSlot)
                {
                    dynamicMatrixInputSlotsToCompare.Add((DynamicMatrixMaterialSlot)inputSlot, outputConcreteType);
                    continue;
                }
            }

            // we can now figure out the dynamic slotType
            // from here set all the
            var dynamicType = ConvertDynamicVectorInputTypeToConcrete(dynamicInputSlotsToCompare.Values);
            foreach (var dynamicKvP in dynamicInputSlotsToCompare)
                dynamicKvP.Key.SetConcreteType(dynamicType);
            foreach (var skippedSlot in skippedDynamicSlots)
                skippedSlot.SetConcreteType(dynamicType);

            // and now dynamic matrices
            var dynamicMatrixType = ConvertDynamicMatrixInputTypeToConcrete(dynamicMatrixInputSlotsToCompare.Values);
            foreach (var dynamicKvP in dynamicMatrixInputSlotsToCompare)
                dynamicKvP.Key.SetConcreteType(dynamicMatrixType);
            foreach (var skippedSlot in skippedDynamicMatrixSlots)
                skippedSlot.SetConcreteType(dynamicMatrixType);

            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            var inputError = s_TempSlots.Any(x => x.hasError);

            // configure the output slots now
            // their slotType will either be the default output slotType
            // or the above dynamic slotType for dynamic nodes
            // or error if there is an input error
            s_TempSlots.Clear();
            GetOutputSlots(s_TempSlots);
            foreach (var outputSlot in s_TempSlots)
            {
                outputSlot.hasError = false;

                if (inputError)
                {
                    outputSlot.hasError = true;
                    continue;
                }

                if (outputSlot is DynamicVectorMaterialSlot)
                {
                    (outputSlot as DynamicVectorMaterialSlot).SetConcreteType(dynamicType);
                    continue;
                }
                else if (outputSlot is DynamicMatrixMaterialSlot)
                {
                    (outputSlot as DynamicMatrixMaterialSlot).SetConcreteType(dynamicMatrixType);
                    continue;
                }
            }

            isInError |= inputError;
            s_TempSlots.Clear();
            GetOutputSlots(s_TempSlots);
            isInError |= s_TempSlots.Any(x => x.hasError);
            isInError |= CalculateNodeHasError(ref errorMessage);
            isInError |= ValidateConcretePrecision(ref errorMessage);
            hasError = isInError;

            if (isInError)
            {
                ((GraphData) owner).AddValidationError(tempId, errorMessage);
            }
            else
            {
                ++version;
            }

            ListPool<DynamicVectorMaterialSlot>.Release(skippedDynamicSlots);
            DictionaryPool<DynamicVectorMaterialSlot, ConcreteSlotValueType>.Release(dynamicInputSlotsToCompare);

            ListPool<DynamicMatrixMaterialSlot>.Release(skippedDynamicMatrixSlots);
            DictionaryPool<DynamicMatrixMaterialSlot, ConcreteSlotValueType>.Release(dynamicMatrixInputSlotsToCompare);
        }

        public int version { get; set; }
        public virtual bool canCopyNode => true;
        //True if error
        protected virtual bool CalculateNodeHasError(ref string errorMessage)
        {
            foreach (var slot in this.GetInputSlots<MaterialSlot>())
            {
                if (slot.isConnected)
                {
                    var edge = owner.GetEdges(slot.slotReference).First();
                    var outputNode = owner.GetNodeFromGuid(edge.outputSlot.nodeGuid);
                    var outputSlot = outputNode.GetOutputSlots<MaterialSlot>().First(s => s.id == edge.outputSlot.slotId);
                    if (!slot.IsCompatibleWith(outputSlot))
                    {
                        errorMessage = $"Slot {slot.RawDisplayName()} cannot accept input of type {outputSlot.concreteValueType}.";
                        return true;
                    }
                }
            }

            return false;
        }

        public virtual void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            foreach (var s in s_TempSlots)
            {
                s_TempPreviewProperties.Clear();
                s_TempEdges.Clear();
                owner.GetEdges(s.slotReference, s_TempEdges);
                if (s_TempEdges.Any())
                    continue;

                s.GetPreviewProperties(s_TempPreviewProperties, GetVariableNameForSlot(s.id));
                for (int i = 0; i < s_TempPreviewProperties.Count; i++)
                {
                    if (s_TempPreviewProperties[i].name == null)
                        continue;

                    properties.Add(s_TempPreviewProperties[i]);
                }
            }
        }

        public virtual string GetVariableNameForSlot(int slotId)
        {
            var slot = FindSlot<MaterialSlot>(slotId);
            if (slot == null)
                throw new ArgumentException(string.Format("Attempting to use MaterialSlot({0}) on node of type {1} where this slot can not be found", slotId, this), "slotId");
            return string.Format("_{0}_{1}_{2}", GetVariableNameForNode(), NodeUtils.GetHLSLSafeName(slot.shaderOutputName), unchecked((uint)slotId));
        }

        public virtual string GetVariableNameForNode()
        {
            return defaultVariableName;
        }

        public void AddSlot(ISlot slot)
        {
            if (!(slot is MaterialSlot))
                throw new ArgumentException(string.Format("Trying to add slot {0} to Material node {1}, but it is not a {2}", slot, this, typeof(MaterialSlot)));

            var addingSlot = (MaterialSlot)slot;
            var foundSlot = FindSlot<MaterialSlot>(slot.id);

            // this will remove the old slot and add a new one
            // if an old one was found. This allows updating values
            m_Slots.RemoveAll(x => x.id == slot.id);
            m_Slots.Add(slot);
            slot.owner = this;

            OnSlotsChanged();

            if (foundSlot == null)
                return;

            addingSlot.CopyValuesFrom(foundSlot);
        }

        public void RemoveSlot(int slotId)
        {
            // Remove edges that use this slot
            // no owner can happen after creation
            // but before added to graph
            if (owner != null)
            {
                var edges = owner.GetEdges(GetSlotReference(slotId));

                foreach (var edge in edges.ToArray())
                    owner.RemoveEdge(edge);
            }

            //remove slots
            m_Slots.RemoveAll(x => x.id == slotId);

            OnSlotsChanged();
        }

        protected virtual void OnSlotsChanged()
        {
            Dirty(ModificationScope.Topological);
            owner?.ClearErrorsForNode(this);
        }

        public void RemoveSlotsNameNotMatching(IEnumerable<int> slotIds, bool supressWarnings = false)
        {
            var invalidSlots = m_Slots.Select(x => x.id).Except(slotIds);

            foreach (var invalidSlot in invalidSlots.ToArray())
            {
                if (!supressWarnings)
                    Debug.LogWarningFormat("Removing Invalid MaterialSlot: {0}", invalidSlot);
                RemoveSlot(invalidSlot);
            }
        }

        public SlotReference GetSlotReference(int slotId)
        {
            var slot = FindSlot<ISlot>(slotId);
            if (slot == null)
                throw new ArgumentException("Slot could not be found", "slotId");
            return new SlotReference(guid, slotId);
        }

        public T FindSlot<T>(int slotId) where T : ISlot
        {
            foreach (var slot in m_Slots)
            {
                if (slot.id == slotId && slot is T)
                    return (T)slot;
            }
            return default(T);
        }

        public T FindInputSlot<T>(int slotId) where T : ISlot
        {
            foreach (var slot in m_Slots)
            {
                if (slot.isInputSlot && slot.id == slotId && slot is T)
                    return (T)slot;
            }
            return default(T);
        }

        public T FindOutputSlot<T>(int slotId) where T : ISlot
        {
            foreach (var slot in m_Slots)
            {
                if (slot.isOutputSlot && slot.id == slotId && slot is T)
                    return (T)slot;
            }
            return default(T);
        }

        public virtual IEnumerable<ISlot> GetInputsWithNoConnection()
        {
            return this.GetInputSlots<ISlot>().Where(x => !owner.GetEdges(GetSlotReference(x.id)).Any());
        }

        public virtual void OnBeforeSerialize()
        {
            m_GuidSerialized = m_Guid.ToString();
            m_GroupGuidSerialized = m_GroupGuid.ToString();
            m_SerializableSlots = SerializationHelper.Serialize<ISlot>(m_Slots);
        }

        public virtual void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(m_GuidSerialized))
                m_Guid = new Guid(m_GuidSerialized);
            else
                m_Guid = Guid.NewGuid();

            if (m_NodeVersion != GetCompiledNodeVersion())
            {
                UpgradeNodeWithVersion(m_NodeVersion, GetCompiledNodeVersion());
                m_NodeVersion = GetCompiledNodeVersion();
            }

            if (!string.IsNullOrEmpty(m_GroupGuidSerialized))
                m_GroupGuid = new Guid(m_GroupGuidSerialized);
            else
                m_GroupGuid = Guid.Empty;

            m_Slots = SerializationHelper.Deserialize<ISlot>(m_SerializableSlots, GraphUtil.GetLegacyTypeRemapping());
            m_SerializableSlots = null;
            foreach (var s in m_Slots)
                s.owner = this;

            UpdateNodeAfterDeserialization();
        }

        public virtual void UpdateNodeAfterDeserialization()
        {}

        public virtual int GetCompiledNodeVersion() => 0;

        public virtual void UpgradeNodeWithVersion(int from, int to)
        {}

        public bool IsSlotConnected(int slotId)
        {
            var slot = FindSlot<MaterialSlot>(slotId);
            return slot != null && owner.GetEdges(slot.slotReference).Any();
        }
    }
}
