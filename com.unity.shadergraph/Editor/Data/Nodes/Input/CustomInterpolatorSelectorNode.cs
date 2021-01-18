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
    [Title("Input", "Custom Interpolator Selector")]
    class CustomInterpolatorSelectorNode : AbstractMaterialNode
    {
        public delegate void OnRevalidation();
        public OnRevalidation revalidationCallback;

        internal override bool ExposeToSearcher { get => false; } // slated for removal.

        public CustomInterpolatorSelectorNode()
        {
            name = "Custom Interpolator";
            UpdateNodeAfterDeserialization();
        }

        public override void ValidateNode()
        {
            base.ValidateNode();
            revalidationCallback?.Invoke();
        }

        public override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();
            BuildSlot();
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return string.Format("IN.{0}", selectedName);
        }

        void BuildSlot()
        {
            if (selectedName != "")
            {
                switch (selectedType)
                {
                    case BlockNode.CustomBlockType.Float:
                        AddSlot(new Vector1MaterialSlot(0, "Out", "Out", SlotType.Output, default(float), ShaderStageCapability.Fragment), false);
                        break;
                    case BlockNode.CustomBlockType.Vector2:
                        AddSlot(new Vector2MaterialSlot(0, "Out", "Out", SlotType.Output, default(Vector2), ShaderStageCapability.Fragment), false);
                        break;
                    case BlockNode.CustomBlockType.Vector3:
                        AddSlot(new Vector3MaterialSlot(0, "Out", "Out", SlotType.Output, default(Vector3), ShaderStageCapability.Fragment), false);
                        break;
                    case BlockNode.CustomBlockType.Vector4:
                        AddSlot(new Vector4MaterialSlot(0, "Out", "Out", SlotType.Output, default(Vector4), ShaderStageCapability.Fragment), false);
                        break;
                }
                RemoveSlotsNameNotMatching(new[] { 0 });
                SetActive(true);
            }
            else
            {
                SetActive(false);
            }
        }


        [SerializeField]
        private string selectedName = "";

        [SerializeField]
        private BlockNode.CustomBlockType selectedType = BlockNode.CustomBlockType.Vector4;

        [CustomInterpolatorControlAttribute("Name")]
        CustomInterpolatorList Selector
        {
            get
            {
                return new CustomInterpolatorList(selectedName, selectedType);
            }
            set
            {
                selectedName = value.selectedEntry;
                selectedType = value.selectedType;
                BuildSlot();
            }
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
