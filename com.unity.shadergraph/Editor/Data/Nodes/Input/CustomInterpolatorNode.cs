using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.Rendering;
using UnityEditor.ShaderGraph.Serialization;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [Title("Custom Interpolators", "Instance")]
    class CustomInterpolatorNode : AbstractMaterialNode
    {
        [SerializeField]
        public string customBlockNodeName = "K_INVALID";

        [SerializeField]
        BlockNode.CustomBlockType serializedType = BlockNode.CustomBlockType.Vector4;

        // preview should be the CI value.
        public override bool hasPreview { get { return true; } }

        internal override bool ExposeToSearcher { get => false; }

        internal BlockNode e_targetBlockNode // weak indirection via customBlockNodeName
        {
            get => (owner?.vertexContext.blocks.Find(cib => cib.value.descriptor.name == customBlockNodeName))?.value ?? null;
        }

        public CustomInterpolatorNode()
        {
            UpdateNodeAfterDeserialization();
        }

        public void ConnectToCustomBlock(string customBlockName)
        {
            // target shouldn't really change, but if it did- we need to unregister.
            if (e_targetBlockNode != null)
            {
                e_targetBlockNode.UnregisterCallback(OnCustomBlockModified);
            }

            name = customBlockNodeName = customBlockName;
            if (e_targetBlockNode != null)
            {
                serializedType = e_targetBlockNode.customWidth;
                BuildSlot();                
                e_targetBlockNode.RegisterCallback(OnCustomBlockModified);
            }
            else // Target blockNode didn't actually exist :(.
            {
                // We should get badged in OnValidate.
            }
        }

        void OnCustomBlockModified(AbstractMaterialNode node, Graphing.ModificationScope scope)
        {
            if (node is BlockNode bnode)
            {
                if (bnode?.isCustomBlock ?? false)
                {
                    name = customBlockNodeName = bnode.customName;
                    if (e_targetBlockNode != null && e_targetBlockNode.owner != null)
                    {
                        serializedType = e_targetBlockNode.customWidth;
                        BuildSlot();
                    }
                }
            }
            // bnode information we got is somehow invalid, this is probably case for an exception.
        }

        public override void ValidateNode()
        {
            // Our node was deleted or we had bad deserialization, we need to badge.
            if (e_targetBlockNode == null || e_targetBlockNode.owner == null)
            {
                e_targetBlockNode?.UnregisterCallback(OnCustomBlockModified);
                owner.AddValidationError(objectId, String.Format("Custom Block Interpolator '{0}' not found.", customBlockNodeName), ShaderCompilerMessageSeverity.Error);

            }
            else
            {
                // our blockNode reference is somehow valid again after it wasn't,
                // we can reconnect and everything should be restored.
                ConnectToCustomBlock(customBlockNodeName);
            }
        }

        public override void UpdateNodeAfterDeserialization()
        {
            // our e_targetBlockNode is unsafe here, so we build w/our serialization info and hope for the best!
            BuildSlot();
            base.UpdateNodeAfterDeserialization();
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return string.Format("IN.{0}", customBlockNodeName);
        }

        void BuildSlot()
        {
            switch (serializedType)
            {
                case BlockNode.CustomBlockType.Float:
                    AddSlot(new Vector1MaterialSlot(0, "Out", "Out", SlotType.Output, default(float), ShaderStageCapability.Fragment));
                    break;
                case BlockNode.CustomBlockType.Vector2:
                    AddSlot(new Vector2MaterialSlot(0, "Out", "Out", SlotType.Output, default(Vector2), ShaderStageCapability.Fragment));
                    break;
                case BlockNode.CustomBlockType.Vector3:
                    AddSlot(new Vector3MaterialSlot(0, "Out", "Out", SlotType.Output, default(Vector3), ShaderStageCapability.Fragment));
                    break;
                case BlockNode.CustomBlockType.Vector4:
                    AddSlot(new Vector4MaterialSlot(0, "Out", "Out", SlotType.Output, default(Vector4), ShaderStageCapability.Fragment));
                    break;
            }
            RemoveSlotsNameNotMatching(new[] { 0 });
        }
    }
}



//if (currentBlockNode == null)
//{
//    owner.AddValidationError(objectId, "Custom Interpolator Identifier not found.", ShaderCompilerMessageSeverity.Error);
//}
//else
//{
//    owner.ClearErrorsForNode(this);
//}
