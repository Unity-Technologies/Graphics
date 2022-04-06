using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [FormerName("UnityEngine.MaterialGraph.BoneIndexNode")]
    [Title("Input", "Geometry", "BoneIndex")]
    class BoneIndexNode : AbstractMaterialNode, IMayRequireVertexSkinning
    {
        public override int latestVersion => 1;
        private const int kOutputSlotId = 0;
        public const string kOutputSlotName = "Out";
        //public override List<CoordinateSpace> validSpaces => new List<CoordinateSpace> { CoordinateSpace.Object };

        public BoneIndexNode()
        {
            name = "BoneIndex";
            precision = Precision.Single;
            UpdateNodeAfterDeserialization();
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return "IN.BoneIndices";
        }

        public bool RequiresVertexSkinning(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return true;
        }

        public override void OnAfterMultiDeserialize(string json)
        {
            base.OnAfterMultiDeserialize(json);
            //required update
            if(sgVersion < 1)
            {
                ChangeVersion(1);
            }
        }
	}
}
