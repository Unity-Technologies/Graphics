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
    [Title("Utility", "Billboard")]
    class BillboardNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireTransform
    {
        private const int OutputSlotId = 0;
        private const int OutputSlot1Id = 1;
        public const string kOutputSlotName = "Cylindrical";
        public const string kOutputSlot1Name = "Spherical";

        public BillboardNode()
        {
            name = "Billboard";
            precision = Precision.Single;
            synonyms = new string[] { "" };
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot( OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot( OutputSlot1Id, kOutputSlot1Name, kOutputSlot1Name, SlotType.Output, Vector3.zero));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, OutputSlot1Id });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine(@"$precision3 _Object_Scale = $precision3(length($precision3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length($precision3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length($precision3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z))); ");
            sb.AppendLine("$precision4 temp_spherical = mul(UNITY_MATRIX_I_V, $precision4 (IN.ObjectSpacePosition * _Object_Scale, 0));");
            sb.AppendLine("$precision4x4 rotationCamMatrix = float4x4( UNITY_MATRIX_I_V[0], $precision4( 0, 1, 0, 0), UNITY_MATRIX_I_V[2], UNITY_MATRIX_I_V[3]);");
            sb.AppendLine("$precision4 temp_cylindrical = mul(rotationCamMatrix, $precision4 (IN.ObjectSpacePosition * _Object_Scale, 0));");
            sb.AppendLine(string.Format("$precision3 {0} = TransformWorldToObject(SHADERGRAPH_OBJECT_POSITION +temp_cylindrical.xyz);", GetVariableNameForSlot(OutputSlotId)));
            sb.AppendLine(string.Format("$precision3 {0} = TransformWorldToObject(SHADERGRAPH_OBJECT_POSITION + temp_spherical.xyz);", GetVariableNameForSlot(OutputSlot1Id)));
        }

        public NeededTransform[] RequiresTransform(ShaderStageCapability stageCapability = ShaderStageCapability.All) => new[] { NeededTransform.ObjectToWorld };


    }
}
