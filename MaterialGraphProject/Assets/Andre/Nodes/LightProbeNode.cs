using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
	[Title("Input/Scene Data/Light Probe")]
	public class LightProbeNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireNormal
	{
		protected const string kUVSlotName = "Normal";
		protected const string kOutputSlotRGBAName = "RGBA";

		public const int NormalSlot = 0;
		public const int OutputSlotRgbaId = 1;

		public override bool hasPreview { get { return false; } }

		public LightProbeNode()
		{
			name = "LightProbe";
			UpdateNodeAfterDeserialization();
		}

		public sealed override void UpdateNodeAfterDeserialization()
		{
			AddSlot(new MaterialSlot(OutputSlotRgbaId, kOutputSlotRGBAName, kOutputSlotRGBAName, SlotType.Output, SlotValueType.Vector3, Vector3.zero));
			AddSlot(new MaterialSlot(NormalSlot, kUVSlotName, kUVSlotName, SlotType.Input, SlotValueType.Vector3, Vector3.zero, false));
			RemoveSlotsNameNotMatching(validSlots);
		}

		public override PreviewMode previewMode {
			get {
				return PreviewMode.Preview3D;
			}
		}

		protected int[] validSlots
		{
			get { return new[] {OutputSlotRgbaId, NormalSlot}; }
		}

		// Node generations
		public virtual void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
		{
			var normalSlot = FindInputSlot<MaterialSlot>(NormalSlot);
			if (normalSlot == null)
				return;

			var normalName = "worldSpaceNormal";
			var edgesNorm = owner.GetEdges(normalSlot.slotReference).ToList();


			if (edgesNorm.Count > 0)
			{
				var edge = edgesNorm[0];
				var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(edge.outputSlot.nodeGuid);
				normalName = ShaderGenerator.AdaptNodeOutput(fromNode, edge.outputSlot.slotId, ConcreteSlotValueType.Vector3, true);
			}
			//float3 shl = ShadeSH9(float4(worldNormal,1));
			string body = "ShadeSH9(float4(" + normalName + ".xyz , 1))";
			visitor.AddShaderChunk(precision + "3 " + GetVariableNameForNode() + " = " + body + ";", true);
		}

		public override string GetVariableNameForSlot(int slotId)
		{
			string slotOutput;
			switch (slotId)
			{
			case NormalSlot:
				slotOutput = "_normalDir";
				break;
			default:
				slotOutput = "";
				break;
			}
			return GetVariableNameForNode() + slotOutput;
		}

		public bool RequiresViewDirection()
		{
			return true;
		}

		public bool RequiresNormal()
		{
			return true;
		}
	}
}
