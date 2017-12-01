using System;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    /*  [Serializable]
      [Title("Master/Metallic")]
      public class MetallicMasterNode : AbstractSurfaceMasterNode
      {
          public const string MetallicSlotName = "Metallic";
          public const int MetallicSlotId = 2;

          public const string LightFunctionName = "Standard";
          public const string SurfaceOutputStructureName = "SurfaceOutputStandard";

          public MetallicMasterNode()
          {
              name = "MetallicMasterNode";
              UpdateNodeAfterDeserialization();
          }

          public sealed override void UpdateNodeAfterDeserialization()
          {
              AddSlot(new MaterialSlot(VertexOffsetId, VertexOffsetName, VertexOffsetName, SlotType.Input, SlotValueType.Vector3, Vector4.zero, ShaderStage.Vertex));
              AddSlot(new MaterialSlot(AlbedoSlotId, AlbedoSlotName, AlbedoSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero, ShaderStage.Fragment));
              AddSlot(new MaterialSlot(NormalSlotId, NormalSlotName, NormalSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero, ShaderStage.Fragment));
              AddSlot(new MaterialSlot(EmissionSlotId, EmissionSlotName, EmissionSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero, ShaderStage.Fragment));
              AddSlot(new MaterialSlot(MetallicSlotId, MetallicSlotName, MetallicSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero, ShaderStage.Fragment));
              AddSlot(new MaterialSlot(SmoothnessSlotId, SmoothnessSlotName, SmoothnessSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero, ShaderStage.Fragment));
              AddSlot(new MaterialSlot(OcclusionSlotId, OcclusionSlotName, OcclusionSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero, ShaderStage.Fragment));
              AddSlot(new MaterialSlot(AlphaSlotId, AlphaSlotName, AlphaSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero, ShaderStage.Fragment));

              // clear out slot names that do not match the slots
              // we support
              RemoveSlotsNameNotMatching(
                  new[]
              {
                  AlbedoSlotId,
                  NormalSlotId,
                  EmissionSlotId,
                  MetallicSlotId,
                  SmoothnessSlotId,
                  OcclusionSlotId,
                  AlphaSlotId,
                  VertexOffsetId
              });
          }

          protected override int[] surfaceInputs
          {
              get
              {
                  return new[]
                  {
                      AlbedoSlotId,
                      NormalSlotId,
                      EmissionSlotId,
                      MetallicSlotId,
                      SmoothnessSlotId,
                      OcclusionSlotId,
                      AlphaSlotId,
                  };
              }
          }


          protected override int[] vertexInputs
          {
              get
              {
                  return new[]
                  {
                      VertexOffsetId
                  };
              }
          }

          public override string GetSurfaceOutputName()
          {
              return SurfaceOutputStructureName;
          }

          public override string GetLightFunction()
          {
              return LightFunctionName;
          }
      }*/
}
