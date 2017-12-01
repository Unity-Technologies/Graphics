using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    /*   [Title("Input/Texture/TextureLOD")]
       public class TextureLODNode : PropertyNode, IGeneratesBodyCode, IMayRequireMeshUV
       {
           protected const string kUVSlotName = "UV";
           protected const string kLODSlotName = "MipMap Level";
           protected const string kOutputSlotRGBAName = "RGBA";
           protected const string kOutputSlotRName = "R";
           protected const string kOutputSlotGName = "G";
           protected const string kOutputSlotBName = "B";
           protected const string kOutputSlotAName = "A";

           public const int UvSlotId = 0;
           public const int LODSlotId = 1;
           public const int OutputSlotRgbaId = 2;
           public const int OutputSlotRId = 3;
           public const int OutputSlotGId = 4;
           public const int OutputSlotBId = 5;
           public const int OutputSlotAId = 6;

           [SerializeField]
           private string m_SerializedTexture;

           [SerializeField]
           private TextureType m_TextureType;

           [Serializable]
           private class TextureHelper
           {
               public Texture texture;
           }

           public override bool hasPreview { get { return true; } }

   #if UNITY_EDITOR
           public Texture defaultTexture
           {
               get
               {
                   if (string.IsNullOrEmpty(m_SerializedTexture))
                       return null;

                   var tex = new TextureHelper();
                   EditorJsonUtility.FromJsonOverwrite(m_SerializedTexture, tex);
                   return tex.texture;
               }
               set
               {
                   if (defaultTexture == value)
                       return;

                   var tex = new TextureHelper();
                   tex.texture = value;
                   m_SerializedTexture = EditorJsonUtility.ToJson(tex, true);

                   if (onModified != null)
                   {
                       onModified(this, ModificationScope.Node);
                   }
               }
           }
   #else
           public Texture defaultTexture { get; set; }
   #endif

           public TextureType textureType
           {
               get { return m_TextureType; }
               set
               {
                   if (m_TextureType == value)
                       return;


                   m_TextureType = value;
                   if (onModified != null)
                   {
                       onModified(this, ModificationScope.Graph);
                   }
               }
           }

           public TextureLODNode()
           {
               name = "TextureLOD";
               UpdateNodeAfterDeserialization();
           }

           public sealed override void UpdateNodeAfterDeserialization()
           {
               AddSlot(new MaterialSlot(OutputSlotRgbaId, kOutputSlotRGBAName, kOutputSlotRGBAName, SlotType.Output, SlotValueType.Vector4, Vector4.zero));
               AddSlot(new MaterialSlot(OutputSlotRId, kOutputSlotRName, kOutputSlotRName, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
               AddSlot(new MaterialSlot(OutputSlotGId, kOutputSlotGName, kOutputSlotGName, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
               AddSlot(new MaterialSlot(OutputSlotBId, kOutputSlotBName, kOutputSlotBName, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
               AddSlot(new MaterialSlot(OutputSlotAId, kOutputSlotAName, kOutputSlotAName, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
               AddSlot(new MaterialSlot(UvSlotId, kUVSlotName, kUVSlotName, SlotType.Input, SlotValueType.Vector2, Vector4.zero));
               AddSlot(new MaterialSlot(LODSlotId, kLODSlotName, kLODSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
               RemoveSlotsNameNotMatching(validSlots);
           }

           protected int[] validSlots
           {
               get { return new[] { OutputSlotRgbaId, OutputSlotRId, OutputSlotGId, OutputSlotBId, OutputSlotAId, UvSlotId, LODSlotId }; }
           }

           // Node generations
           public virtual void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
           {
               var uvSlot = FindInputSlot<MaterialSlot>(UvSlotId);
               var lodSlot = FindInputSlot<MaterialSlot>(LODSlotId);

               if (uvSlot == null)
                   return;

               if (lodSlot == null)
                   return;

               var uvName = string.Format("{0}", UVChannel.uv0.GetUVName());
               var lodName = "";

               var edges = owner.GetEdges(uvSlot.slotReference).ToList();
               var edgesLOD = owner.GetEdges(lodSlot.slotReference).ToList();

               if (edges.Count > 0)
               {
                   var edge = edges[0];
                   var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(edge.outputSlot.nodeGuid);
                   uvName = ShaderGenerator.AdaptNodeOutput(fromNode, edge.outputSlot.slotId, ConcreteSlotValueType.Vector2, true);
               }

               if (edgesLOD.Count > 0)
               {
                   var edge = edgesLOD[0];
                   var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(edge.outputSlot.nodeGuid);
                   lodName = ShaderGenerator.AdaptNodeOutput(fromNode, edge.outputSlot.slotId, ConcreteSlotValueType.Vector1, true);
               }
               //if no input is specified, default to mipmap 0
               else
                   lodName = "0";

               string body = "tex2Dlod (" + propertyName + ", " + precision + "4(" + uvName + ".x," + uvName + ".y, 0," + lodName + "));";
               if (m_TextureType == TextureType.Bump)
                   body = precision + "4(UnpackNormal(" + body + "), 0)";
               visitor.AddShaderChunk(precision + "4 " + GetVariableNameForNode() + " = " + body + ";", true);
           }

           public override string GetVariableNameForSlot(int slotId)
           {
               string slotOutput;
               switch (slotId)
               {
                   case OutputSlotRId:
                       slotOutput = ".r";
                       break;
                   case OutputSlotGId:
                       slotOutput = ".g";
                       break;
                   case OutputSlotBId:
                       slotOutput = ".b";
                       break;
                   case OutputSlotAId:
                       slotOutput = ".a";
                       break;
                   default:
                       slotOutput = "";
                       break;
               }
               return GetVariableNameForNode() + slotOutput;
           }

           public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
           {
               properties.Add(GetPreviewProperty());
           }

           // Properties
           public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
           {
               visitor.AddShaderProperty(
                   new TexturePropertyChunk(
                       propertyName,
                       description,
                       defaultTexture, m_TextureType,
                       PropertyChunk.HideState.Visible,
                       exposedState == ExposedState.Exposed ?
                       TexturePropertyChunk.ModifiableState.Modifiable
                       : TexturePropertyChunk.ModifiableState.NonModifiable));
           }

           public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
           {
               visitor.AddShaderChunk("sampler2D " + propertyName + ";", true);
           }

           /*
               public override bool DrawSlotDefaultInput(Rect rect, Slot inputSlot)
               {
                   var uvSlot = FindInputSlot(kUVSlotName);
                   if (uvSlot != inputSlot)
                       return base.DrawSlotDefaultInput(rect, inputSlot);


                   var rectXmax = rect.xMax;
                   rect.x = rectXmax - 70;
                   rect.width = 70;

                   EditorGUI.DrawRect(rect, new Color(0.0f, 0.0f, 0.0f, 0.7f));
                   GUI.Label(rect, "From Mesh");

                   return false;
               }
               *

           public override PreviewProperty GetPreviewProperty()
           {
               return new PreviewProperty
               {
                   m_Name = propertyName,
                   m_PropType = PropertyType.Texture,
                   m_Texture = defaultTexture
               };
           }

           public override PropertyType propertyType { get { return PropertyType.Texture; } }

           public bool RequiresMeshUV(UVChannel channel)
           {
               if (channel != UVChannel.uv0)
               {
                   return false;
               }

               var uvSlot = FindInputSlot<MaterialSlot>(UvSlotId);
               if (uvSlot == null)
                   return true;

               var edges = owner.GetEdges(uvSlot.slotReference).ToList();
               return edges.Count == 0;
           }
       }*/
}
