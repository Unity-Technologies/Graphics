using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEngine.Serialization;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Subgraph Delegate")]
    class SubgraphDelegateNode : AbstractMaterialNode, IOnAssetEnabled, IGeneratesBodyCode, IGeneratesFunction
    {
        public SubgraphDelegateNode()
        {
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        private string m_SubgraphDelegateGuidSerialized;

        private Guid m_SubgraphDelegateGuid;

        public Guid subgraphDelegateGuid
        {
            get { return m_SubgraphDelegateGuid; }
            set
            {
                if (m_SubgraphDelegateGuid == value)
                    return;

                m_SubgraphDelegateGuid = value;
                UpdateNode();
                Dirty(ModificationScope.Topological);
            }
        }

        public override bool canSetPrecision => false;
        public override bool hasPreview => true;
        //public const int OutputSlotId = 0;
        List<MaterialSlot> inputSlots = new List<MaterialSlot>();
        List<MaterialSlot> outputSlots = new List<MaterialSlot>();

        public string GetFunctionName()
        {
            return GetFunctionNameFromGuid(m_SubgraphDelegateGuid);
        }

        public static string GetFunctionNameFromGuid(Guid guid)
        {
            return $"SubgraphDelegate_{guid.GetHashCode().ToString("X")}";
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            List<String> slotValues = new List<String>();

            for (int i = 0; i < inputSlots.Count(); ++i)
            {
                String slotValueStr = GetSlotValue(inputSlots[i].id, generationMode, concretePrecision);
                switch (inputSlots[i].valueType)
                {
                    case SlotValueType.Texture2D:
                        slotValues.Add(String.Format("TEXTURE2D_ARGS({0}, sampler{0}), {0}_TexelSize", slotValueStr));
                        break;
                    case SlotValueType.Texture2DArray:
                        slotValues.Add(String.Format("TEXTURE2DARRAY_ARGS({0}, sampler{0})", slotValueStr));
                        break;
                    case SlotValueType.Texture3D:
                        slotValues.Add(String.Format("TEXTURE3D_ARGS({0}, sampler{0})", slotValueStr));
                        break;
                    case SlotValueType.Cubemap:
                        slotValues.Add(String.Format("TEXTURECUBE_ARGS({0}, sampler{0})", slotValueStr));
                        break;
                    default:
                        slotValues.Add(slotValueStr);
                        break;
                }
            }
            slotValues.Add("IN");
            for (int i = 0; i < outputSlots.Count(); ++i)
            {
                slotValues.Add(GetSlotValue(outputSlots[i].id, generationMode, concretePrecision));
                if (FindSlot<MaterialSlot>(outputSlots[i].id) != null)
                {
                    sb.AppendLine("{0} {1} = {2};", outputSlots[i].concreteValueType.ToShaderString(), GetVariableNameForSlot(outputSlots[i].id), outputSlots[i].GetDefaultValue(GenerationMode.ForReals));
                }
            }
            var subDelegate = owner.subgraphDelegates.FirstOrDefault(x => x.guid == subgraphDelegateGuid);
            if (subDelegate != null && subDelegate.connectedNode != null)
            {
                var inputVariableName = $"_{subDelegate.connectedNode.GetVariableNameForNode()}";

                SubShaderGenerator.GenerateSurfaceInputTransferCode(sb, subDelegate.connectedNode.asset.requirements, subDelegate.connectedNode.asset.inputStructName, inputVariableName);
            }
            sb.Append("{0}(", GetFunctionName());
            for (int i = 0; i < slotValues.Count(); ++i)
            {
                sb.Append("{0}", slotValues[i]);
                if (i < (slotValues.Count() - 1))
                    sb.Append(", ");
            }
            sb.Append(");\n");
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction(GetFunctionName(), s =>
            {
                s.Append("void {0}(", GetFunctionName());
                for (int i = 0; i < inputSlots.Count(); ++i)
                {
                    switch(inputSlots[i].valueType)
                    {
                        case SlotValueType.Texture2D:
                            s.Append("TEXTURE2D_PARAM({0}, sampler_{0}), $precision4 {0}_TexelSize, ", inputSlots[i].shaderOutputName);
                            break;
                        case SlotValueType.Texture2DArray:
                            s.Append("TEXTURE2DARRAY_PARAM({0}, sampler_{0}), ", inputSlots[i].shaderOutputName);
                            break;
                        case SlotValueType.Texture3D:
                            s.Append("TEXTURE3D_PARAM({0}, sampler_{0}), ", inputSlots[i].shaderOutputName);
                            break;
                        case SlotValueType.Cubemap:
                            s.Append("TEXTURECUBE_PARAM({0}, sampler_{0}), ", inputSlots[i].shaderOutputName);
                            break;
                        default:
                            s.Append("{0} {1}, ", inputSlots[i].concreteValueType.ToShaderString(), inputSlots[i].shaderOutputName);
                            break;
                    }
                }

                s.Append("SurfaceDescriptionInputs IN, ");

                for (int i = 0; i < outputSlots.Count(); ++i)
                {
                    s.Append("inout {0} {1}", outputSlots[i].concreteValueType.ToShaderString(), outputSlots[i].shaderOutputName);
                    if (i < (outputSlots.Count() - 1))
                        s.Append(", ");
                }
                s.Append(")\n");
                using (s.BlockScope())
                {
                    // TODO : If there's a source SubGraphAsset, we put in a call
                }
            });
        }

        public void OnEnable()
        {
            UpdateNode();
        }

        public void UpdateNode()
        {
            var subDelegate = owner.subgraphDelegates.FirstOrDefault(x => x.guid == subgraphDelegateGuid);
            if (subDelegate == null)
                return;
            name = subDelegate.displayName;
            UpdatePorts(subDelegate);
        }

        void UpdatePorts(ShaderSubgraphDelegate subDelegate)
        {
            // Get slots
            //List<MaterialSlot> inputSlots = new List<MaterialSlot>();
            inputSlots.Clear();
            GetInputSlots(inputSlots);
            int numInputSlots = inputSlots.Count();

            // Store the edges
            Dictionary<MaterialSlot, List<IEdge>> inputEdgeDict = new Dictionary<MaterialSlot, List<IEdge>>();
            foreach (MaterialSlot slot in inputSlots)
            {
                var edges = (List<IEdge>)slot.owner.owner.GetEdges(slot.slotReference);
                inputEdgeDict.Add(slot, edges);
            }

            // Remove old slots
            for (int i = 0; i < inputSlots.Count; i++)
            {
                RemoveSlot(inputSlots[i].id);
            }

            // Get slots
            //List<MaterialSlot> outputSlots = new List<MaterialSlot>();
            outputSlots.Clear();
            GetOutputSlots(outputSlots);

            // Store the edges
            Dictionary<MaterialSlot, List<IEdge>> outputEdgeDict = new Dictionary<MaterialSlot, List<IEdge>>();
            foreach (MaterialSlot slot in outputSlots)
            {
                var edges = (List<IEdge>)slot.owner.owner.GetEdges(slot.slotReference);
                outputEdgeDict.Add(slot, edges);
            }

            // Remove old slots
            for (int i = 0; i < outputSlots.Count; i++)
            {
                RemoveSlot(outputSlots[i].id);
            }

            // Clear the lists again
            inputSlots.Clear();
            outputSlots.Clear();

            // Add input slots
            int[] slotIds = new int[subDelegate.input_Entries.Count + subDelegate.output_Entries.Count];
            for (int i = 0; i < subDelegate.input_Entries.Count; i++)
            {
                // Get slot based on entry id
                MaterialSlot slot = inputSlots.Where(x =>
                x.id == subDelegate.input_Entries[i].id &&
                x.slotType == SlotType.Input &&
                x.concreteValueType == subDelegate.input_Entries[i].propertyType.ToConcreteShaderValueType() &&
                x.RawDisplayName() == subDelegate.input_Entries[i].displayName &&
                x.shaderOutputName == subDelegate.input_Entries[i].referenceName).FirstOrDefault();

                // If slot doesnt exist its new so create it
                if (slot == null)
                {
                    SlotValueType valueType = subDelegate.input_Entries[i].propertyType.ToConcreteShaderValueType().ToSlotValueType();
                    slot = MaterialSlot.CreateMaterialSlot(valueType, subDelegate.input_Entries[i].id, subDelegate.input_Entries[i].displayName, subDelegate.input_Entries[i].referenceName, SlotType.Input, Vector4.zero);
                }

                AddSlot(slot);
                slotIds[i] = subDelegate.input_Entries[i].id;
                inputSlots.Add(slot);
            }
            for (int i = 0; i < subDelegate.output_Entries.Count; i++)
            {
                // Get slot based on entry id
                int newID = subDelegate.output_Entries[i].id + subDelegate.input_Entries.Count;
                MaterialSlot slot = outputSlots.Where(x => x.id == newID &&
                x.slotType == SlotType.Output &&
                x.concreteValueType == subDelegate.output_Entries[i].propertyType.ToConcreteShaderValueType() &&
                x.RawDisplayName() == subDelegate.output_Entries[i].displayName &&
                x.shaderOutputName == subDelegate.output_Entries[i].referenceName).FirstOrDefault();

                // If slot doesnt exist it's new so create it
                if (slot == null)
                {
                    SlotValueType valueType = subDelegate.output_Entries[i].propertyType.ToConcreteShaderValueType().ToSlotValueType();
                    slot = MaterialSlot.CreateMaterialSlot(valueType, newID, subDelegate.output_Entries[i].displayName, subDelegate.output_Entries[i].referenceName, SlotType.Output, Vector4.zero);
                }

                AddSlot(slot);
                slotIds[i + subDelegate.input_Entries.Count] = newID;
                outputSlots.Add(slot);
            }
            RemoveSlotsNameNotMatching(slotIds);

            int inputSlotsOffset = inputSlots.Count() - numInputSlots;

            // Reconnect the edges
            foreach (KeyValuePair<MaterialSlot, List<IEdge>> entry in inputEdgeDict)
            {
                foreach (IEdge edge in entry.Value)
                {
                    owner.Connect(edge.outputSlot, edge.inputSlot);
                }
            }

            // Reconnect the edges
            foreach (KeyValuePair<MaterialSlot, List<IEdge>> entry in outputEdgeDict)
            {
                foreach (IEdge edge in entry.Value)
                {
                    SlotReference newOutputSlot = new SlotReference(edge.outputSlot.nodeGuid, edge.outputSlot.slotId + inputSlotsOffset);
                    owner.Connect(newOutputSlot, edge.inputSlot);
                }
            }
            
            ValidateNode();
        }

        protected override bool CalculateNodeHasError(ref string errorMessage)
        {
            return false;
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();

            // Handle keyword guid serialization
            m_SubgraphDelegateGuidSerialized = m_SubgraphDelegateGuid.ToString();
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            // Handle keyword guid serialization
            if (!string.IsNullOrEmpty(m_SubgraphDelegateGuidSerialized))
            {
                m_SubgraphDelegateGuid = new Guid(m_SubgraphDelegateGuidSerialized);
            }
        }
    }
}
