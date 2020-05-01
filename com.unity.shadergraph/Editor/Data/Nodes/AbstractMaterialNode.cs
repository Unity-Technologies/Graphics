using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Colors;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Serialization;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    abstract class AbstractMaterialNode : JsonObject, IGroupItem
    {
        [SerializeField]
        JsonRef<GroupData> m_Group = null;

        [SerializeField]
        private string m_Name;

        [SerializeField]
        private int m_NodeVersion;

        [SerializeField]
        private DrawState m_DrawState;

        [NonSerialized]
        bool m_HasError;

        [NonSerialized]
        bool m_IsActive = true;
        
        [SerializeField]
        List<JsonData<MaterialSlot>> m_Slots = new List<JsonData<MaterialSlot>>();

        public GraphData owner { get; set; }

        OnNodeModified m_OnModified;

        public GroupData group
        {
            get => m_Group;
            set
            {
                if (m_Group == value)
                    return;

                m_Group = value;
                Dirty(ModificationScope.Topological);
            }
        }

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

        public string name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        protected virtual string documentationPage => name;
        public virtual string documentationURL => NodeUtils.GetDocumentationString(documentationPage);

        public virtual bool canDeleteNode => owner != null && owner.outputNode != this;

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

        [Inspectable("Precision", ConcretePrecision.Float)]
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

        // TODO: Can actually delete this
        public virtual bool allowedInSubGraph
        {
            get { return !(this is BlockNode); }
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

        public virtual bool isActive
        {
            get { return m_IsActive; }
            set 
            {
                if(m_IsActive == value)
                    return;

                // Update this node
                m_IsActive = value;
                Dirty(ModificationScope.Node);

                // Get all downsteam nodes and update their active state
                var nodes = ListPool<AbstractMaterialNode>.Get();
                NodeUtils.DepthFirstCollectNodesFromNode(nodes, this, NodeUtils.IncludeSelf.Include);
                foreach(var upstreamNode in nodes)
                {
                    NodeUtils.UpdateNodeActiveOnEdgeChange(upstreamNode);
                }
            }
        }

        string m_DefaultVariableName;
        string m_NameForDefaultVariableName;

        string defaultVariableName
        {
            get
            {
                if (m_NameForDefaultVariableName != name)
                {
                    m_DefaultVariableName = string.Format("{0}_{1}", NodeUtils.GetHLSLSafeName(name ?? "node"), objectId);
                    m_NameForDefaultVariableName = name;
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
            m_NodeVersion = GetCompiledNodeVersion();
            version = 0;
        }

        public void GetInputSlots<T>(List<T> foundSlots) where T : MaterialSlot
        {
            foreach (var slot in m_Slots.SelectValue())
            {
                if (slot.isInputSlot && slot is T)
                    foundSlots.Add((T)slot);
            }
        }

        public void GetOutputSlots<T>(List<T> foundSlots) where T : MaterialSlot
        {
            foreach (var slot in m_Slots.SelectValue())
            {
                if (slot.isOutputSlot && slot is T materialSlot)
                {
                    foundSlots.Add(materialSlot);
                }
            }
        }

        public void GetSlots<T>(List<T> foundSlots) where T : MaterialSlot
        {
            foreach (var slot in m_Slots.SelectValue())
            {
                if (slot is T materialSlot)
                {
                    foundSlots.Add(materialSlot);
                }
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
                var fromNode = fromSocketRef.node;
                return fromNode.GetOutputForSlot(fromSocketRef, inputSlot.concreteValueType, generationMode);
            }

            return inputSlot.GetDefaultValue(generationMode);
        }

        protected virtual string GetOutputForSlot(SlotReference fromSocketRef,  ConcreteSlotValueType valueType, GenerationMode generationMode)
        {
            var slot = FindOutputSlot<MaterialSlot>(fromSocketRef.slotId);
            if (slot == null)
                return string.Empty;

            return GenerationUtils.AdaptNodeOutput(this, slot.id, valueType);
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

        public virtual void EvaluateConcretePrecision()
        {
            // If Node has a precision override use that
            if (precision != Precision.Inherit)
            {
                m_ConcretePrecision = precision.ToConcrete();
                return;
            }

            // Get inputs
            using(var tempSlots = PooledList<MaterialSlot>.Get())
            {
                GetInputSlots(tempSlots);

                // If no inputs were found use the precision of the Graph
                // This can be removed when parameters are considered as true inputs
                if (tempSlots.Count == 0)
                {
                    m_ConcretePrecision = owner.concretePrecision;
                    return;
                }

                // Otherwise compare precisions from inputs
                var precisionsToCompare = new List<int>();

                foreach (var inputSlot in tempSlots)
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
                    var outputNode = outputSlotRef.node;

                    // Use precision from connected Node
                    precisionsToCompare.Add((int)outputNode.concretePrecision);
                }

                // Use highest precision from all input sources
                m_ConcretePrecision = (ConcretePrecision)precisionsToCompare.OrderBy(x => x).First();

                // Clean up
                return;
            }
        }

        public virtual void EvaluateDynamicMaterialSlots()
        {
            var dynamicInputSlotsToCompare = DictionaryPool<DynamicVectorMaterialSlot, ConcreteSlotValueType>.Get();
            var skippedDynamicSlots = ListPool<DynamicVectorMaterialSlot>.Get();

            var dynamicMatrixInputSlotsToCompare = DictionaryPool<DynamicMatrixMaterialSlot, ConcreteSlotValueType>.Get();
            var skippedDynamicMatrixSlots = ListPool<DynamicMatrixMaterialSlot>.Get();

            // iterate the input slots
            using (var tempSlots = PooledList<MaterialSlot>.Get())
            {
                GetInputSlots(tempSlots);
                foreach (var inputSlot in tempSlots)
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
                    var outputNode = outputSlotRef.node;
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

                tempSlots.Clear();
                GetInputSlots(tempSlots);
                bool inputError = tempSlots.Any(x => x.hasError);
                if(inputError)
                {
                    owner.AddConcretizationError(objectId, string.Format("Node {0} had input error", objectId));
                    hasError = true;
                }

                // configure the output slots now
                // their slotType will either be the default output slotType
                // or the above dynamic slotType for dynamic nodes
                // or error if there is an input error
                tempSlots.Clear();
                GetOutputSlots(tempSlots);
                foreach (var outputSlot in tempSlots)
                {
                    outputSlot.hasError = false;

                    if (inputError)
                    {
                        outputSlot.hasError = true;
                        continue;
                    }

                    if (outputSlot is DynamicVectorMaterialSlot dynamicVectorMaterialSlot)
                    {
                        dynamicVectorMaterialSlot.SetConcreteType(dynamicType);
                        continue;
                    }
                    else if (outputSlot is DynamicMatrixMaterialSlot dynamicMatrixMaterialSlot)
                    {
                        dynamicMatrixMaterialSlot.SetConcreteType(dynamicMatrixType);
                        continue;
                    }
                }


                tempSlots.Clear();
                GetOutputSlots(tempSlots);
                if(tempSlots.Any(x => x.hasError))
                {
                    owner.AddConcretizationError(objectId, string.Format("Node {0} had output error", objectId));
                    hasError = true;
                }
                CalculateNodeHasError();

                ListPool<DynamicVectorMaterialSlot>.Release(skippedDynamicSlots);
                DictionaryPool<DynamicVectorMaterialSlot, ConcreteSlotValueType>.Release(dynamicInputSlotsToCompare);

                ListPool<DynamicMatrixMaterialSlot>.Release(skippedDynamicMatrixSlots);
                DictionaryPool<DynamicMatrixMaterialSlot, ConcreteSlotValueType>.Release(dynamicMatrixInputSlotsToCompare);
            }
        }

        public virtual void Concretize()
        {
            hasError = false;
            owner.ClearErrorsForNode(this);
            EvaluateConcretePrecision();
            EvaluateDynamicMaterialSlots();
            if(!hasError)
            {
                ++version;
            }
        }

        public virtual void ValidateNode()
        {

        }

        public int version { get; set; }
        public virtual bool canCutNode => true;
        public virtual bool canCopyNode => true;

        protected virtual void CalculateNodeHasError()
        {
            foreach (var slot in this.GetInputSlots<MaterialSlot>())
            {
                if (slot.isConnected)
                {
                    var edge = owner.GetEdges(slot.slotReference).First();
                    var outputNode = edge.outputSlot.node;
                    var outputSlot = outputNode.GetOutputSlots<MaterialSlot>().First(s => s.id == edge.outputSlot.slotId);
                    if (!slot.IsCompatibleWith(outputSlot))
                    {
                        owner.AddConcretizationError(objectId, $"Slot {slot.RawDisplayName()} cannot accept input of type {outputSlot.concreteValueType}.");
                        hasError = true;
                        return;
                    }
                }
            }
        }

        public virtual void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            using (var tempSlots = PooledList<MaterialSlot>.Get())
            using (var tempPreviewProperties = PooledList<PreviewProperty>.Get())
            using (var tempEdges = PooledList<IEdge>.Get())
            {
                GetInputSlots(tempSlots);
                foreach (var s in tempSlots)
                {
                    tempPreviewProperties.Clear();
                    tempEdges.Clear();
                    owner.GetEdges(s.slotReference, tempEdges);
                    if (tempEdges.Any())
                        continue;

                    s.GetPreviewProperties(tempPreviewProperties, GetVariableNameForSlot(s.id));
                    for (int i = 0; i < tempPreviewProperties.Count; i++)
                    {
                        if (tempPreviewProperties[i].name == null)
                            continue;

                        properties.Add(tempPreviewProperties[i]);
                    }
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

        public void AddSlot(MaterialSlot slot)
        {
            if(slot == null)
            {
                throw new ArgumentException($"Trying to add null slot to node {this}");
            }
            var foundSlot = FindSlot<MaterialSlot>(slot.id);

            // Try to keep the existing instance to avoid unnecessary changes to file
            if (foundSlot != null && slot.GetType() == foundSlot.GetType())
            {
                foundSlot.displayName = slot.RawDisplayName();
                foundSlot.CopyDefaultValue(slot);
                return;
            }

            // this will remove the old slot and add a new one
            // if an old one was found. This allows updating values
            m_Slots.RemoveAll(x => x.value.id == slot.id);

            m_Slots.Add(slot);
            slot.owner = this;

            OnSlotsChanged();

            if (foundSlot == null)
                return;

            slot.CopyValuesFrom(foundSlot);
            foundSlot.owner = null;
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
            m_Slots.RemoveAll(x => x.value.id == slotId);

            OnSlotsChanged();
        }

        protected virtual void OnSlotsChanged()
        {
            Dirty(ModificationScope.Topological);
            owner?.ClearErrorsForNode(this);
        }

        public void RemoveSlotsNameNotMatching(IEnumerable<int> slotIds, bool supressWarnings = false)
        {
            var invalidSlots = m_Slots.Select(x => x.value.id).Except(slotIds);

            foreach (var invalidSlot in invalidSlots.ToArray())
            {
                if (!supressWarnings)
                    Debug.LogWarningFormat("Removing Invalid MaterialSlot: {0}", invalidSlot);
                RemoveSlot(invalidSlot);
            }
        }

        public SlotReference GetSlotReference(int slotId)
        {
            var slot = FindSlot<MaterialSlot>(slotId);
            if (slot == null)
                throw new ArgumentException("Slot could not be found", "slotId");
            return new SlotReference(this, slotId);
        }

        public T FindSlot<T>(int slotId) where T : MaterialSlot
        {
            foreach (var slot in m_Slots.SelectValue())
            {
                if (slot.id == slotId && slot is T)
                    return (T)slot;
            }
            return default(T);
        }

        public T FindInputSlot<T>(int slotId) where T : MaterialSlot
        {
            foreach (var slot in m_Slots.SelectValue())
            {
                if (slot.isInputSlot && slot.id == slotId && slot is T)
                    return (T)slot;
            }
            return default(T);
        }

        public T FindOutputSlot<T>(int slotId) where T : MaterialSlot
        {
            foreach (var slot in m_Slots.SelectValue())
            {
                if (slot.isOutputSlot && slot.id == slotId && slot is T)
                    return (T)slot;
            }
            return default(T);
        }

        public virtual IEnumerable<MaterialSlot> GetInputsWithNoConnection()
        {
            return this.GetInputSlots<MaterialSlot>().Where(x => !owner.GetEdges(GetSlotReference(x.id)).Any());
        }

        public override void OnAfterMultiDeserialize(string json)
        {
            if (m_NodeVersion != GetCompiledNodeVersion())
            {
                UpgradeNodeWithVersion(m_NodeVersion, GetCompiledNodeVersion());
                m_NodeVersion = GetCompiledNodeVersion();
            }

            foreach (var s in m_Slots.SelectValue())
                s.owner = this;

            // UpdateNodeAfterDeserialization();
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

        public virtual void Setup() {}
    }
}
