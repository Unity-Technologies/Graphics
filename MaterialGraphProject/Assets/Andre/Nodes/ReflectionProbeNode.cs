using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
	[Title("Input/Scene Data/Reflection Probe")]
	public class ReflectionProbeNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireViewDirection, IMayRequireNormal
	{
		protected const string kUVSlotName = "RefVector";
		protected const string kOutputSlotRGBAName = "RGBA";
		protected const string kInputSlotLodName = "MipLevel";

		public const int NormalSlot = 0;
		public const int InputSlotLod = 2;
		public const int OutputSlotRgbaId = 1;

		public override bool hasPreview { get { return true; } }

		public ReflectionProbeNode()
		{
			name = "ReflectionProbe";
			UpdateNodeAfterDeserialization();
		}

		public sealed override void UpdateNodeAfterDeserialization()
		{
			AddSlot(new MaterialSlot(OutputSlotRgbaId, kOutputSlotRGBAName, kOutputSlotRGBAName, SlotType.Output, SlotValueType.Vector4, Vector4.zero));
			AddSlot(new MaterialSlot(NormalSlot, kUVSlotName, kUVSlotName, SlotType.Input, SlotValueType.Vector3, Vector3.zero, false));
			AddSlot (new MaterialSlot(InputSlotLod, kInputSlotLodName, kInputSlotLodName, SlotType.Input, SlotValueType.Vector1, Vector2.zero));
			RemoveSlotsNameNotMatching(validSlots);
		}

		public override PreviewMode previewMode {
			get {
				return PreviewMode.Preview3D;
			}
		}

		protected int[] validSlots
		{
			get { return new[] {OutputSlotRgbaId, NormalSlot, InputSlotLod}; }
		}

		// Node generations
		public virtual void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
		{
			var uvSlot = FindInputSlot<MaterialSlot>(NormalSlot);
			if (uvSlot == null)
				return;

			var lodID = FindInputSlot<MaterialSlot>(InputSlotLod);
			if (lodID == null)
				return;

			var uvName = "reflect(-worldSpaceViewDirection, worldSpaceNormal)";
			var lodValue = lodID.currentValue.x.ToString ();
			var edgesUV = owner.GetEdges(uvSlot.slotReference).ToList();
			var edgesLOD = owner.GetEdges (lodID.slotReference).ToList ();


			if (edgesUV.Count > 0)
			{
				var edge = edgesUV[0];
				var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(edge.outputSlot.nodeGuid);
				uvName = ShaderGenerator.AdaptNodeOutput(fromNode, edge.outputSlot.slotId, ConcreteSlotValueType.Vector3, true);
			}

			if (edgesLOD.Count > 0)
			{
				var edge = edgesLOD[0];
				var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(edge.outputSlot.nodeGuid);
				lodValue = GetSlotValue (edge.inputSlot.slotId, GenerationMode.ForReals);
			}

			string body = "DecodeHDR(UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, " + uvName + ".xyz ," + lodValue + "), unity_SpecCube0_HDR)";
			visitor.AddShaderChunk(precision + "3 " + GetVariableNameForNode() + " = " + body + ";", true);
		}

		public override string GetVariableNameForSlot(int slotId)
		{
			string slotOutput;
			switch (slotId)
			{
			case InputSlotLod:
				slotOutput = "_lod";
				break;
			case NormalSlot:
				slotOutput = "_refDir";
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
