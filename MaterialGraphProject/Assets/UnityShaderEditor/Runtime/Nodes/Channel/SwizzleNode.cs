using UnityEngine.Graphing;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.MaterialGraph
{
    /* [Title("Channel/Swizzle")]
     public class SwizzleNode : Function1Input
     {
         public enum SwizzleChannel
         {
             R = 0,
             G = 1,
             B = 2,
             A = 3,
         }

         [SerializeField]
         private SwizzleChannel[] m_SwizzleChannels = new SwizzleChannel[4] { SwizzleChannel.R, SwizzleChannel.G, SwizzleChannel.B, SwizzleChannel.A };

         public SwizzleChannel[] swizzleChannels
         {
             get { return m_SwizzleChannels; }
             set
             {
                 if (m_SwizzleChannels == value)
                     return;

                 m_SwizzleChannels = value;
                 if (onModified != null)
                 {
                     onModified(this, ModificationScope.Graph);
                 }
             }
         }

         public SwizzleNode()
         {
             name = "Swizzle";
         }

         protected override string GetFunctionName()
         {
             return "";
         }

         protected override string GetFunctionCallBody(string inputValue)
         {
             string[] channelNames = { "r", "g", "b", "a" };
             var inputSlot = FindInputSlot<MaterialSlot>(InputSlotId);
             var outputSlot = FindOutputSlot<MaterialSlot>(OutputSlotId);

             if (inputSlot == null)
                 return "1.0";
             if (outputSlot == null)
                 return "1.0";

             int numInputChannels = (int)SlotValueHelper.GetChannelCount(inputSlot.concreteValueType);
             int numOutputChannels = (int)SlotValueHelper.GetChannelCount(outputSlot.concreteValueType);

             //int numInputChannels = (int)inputSlot.concreteValueType;
             int numOutputChannels = (int)outputSlot.concreteValueType;
             if (owner.GetEdges(inputSlot.slotReference).ToList().Count() == 0)
                 numInputChannels = 0;
             if (owner.GetEdges(outputSlot.slotReference).ToList().Count() == 0)
                 numOutputChannels = 0;

             if (numOutputChannels == 0)
             {
                 return "1.0";
             }

             string outputString = precision + "4(";
             //float4(1.0,1.0,1.0,1.0)
             if (numInputChannels == 0)
             {
                 outputString += "1.0, 1.0, 1.0, 1.0).";
             }
             else
             {
                 //float4(arg1.
                 outputString += inputValue + ".";

                 //float4(arg1.xy
                 int i = 0;
                 for (; i < numInputChannels; ++i)
                 {
                     int channel = (int)m_SwizzleChannels[i];
                     outputString += channelNames[channel];
                 }

                 //float4(arg1.xy, 1.0, 1.0)
                 for (; i < 4; i++)
                 {
                     outputString += ", 1.0";
                 }
                 outputString += ").";
             }

             //float4(arg1.xy, 1.0, 1.0).rg
             for (int j = 0; j < numOutputChannels; ++j)
             {
                 outputString += channelNames[j];
             }
             return outputString;
         }
     }*/
}
