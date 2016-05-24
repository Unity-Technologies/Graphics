using System.Collections.Generic;
using System.Text;
using System.Xml;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental.VFX
{
    public static class ModelSerializer
    {
        private class MetaData
        {
            private List<VFXPropertySlot> slots = new List<VFXPropertySlot>();
            private Dictionary<VFXPropertySlot,int> slotsToId = new Dictionary<VFXPropertySlot,int>();

            public void AddSlot(VFXPropertySlot slot)
            {
                slotsToId.Add(slot,slots.Count);
                slots.Add(slot);      
            }

            public int GetNbSlots()                 { return slots.Count; }
            public VFXPropertySlot GetSlot(int id)  { return slots[id]; }
            public int GetId(VFXPropertySlot slot)  { return slotsToId[slot]; }
        }

        public static string Serialize(VFXGraph graph)
        {
            var buffer = new StringBuilder();

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "    ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };

            var writer = XmlWriter.Create(buffer, settings);

            var data = new MetaData();

            writer.WriteStartDocument();
            writer.WriteElementString("version","1");

            for (int i = 0; i < graph.systems.GetNbChildren(); ++i)
                Serialize(writer, graph.systems.GetChild(i), data);

            for (int i = 0; i < graph.models.GetNbChildren(); ++i)
            {
                var model = graph.models.GetChild(i);
                var dataNode = model as VFXDataNodeModel;
                if (dataNode != null)
                    Serialize(writer, dataNode, data);   
            }

            SerializeConnections(writer, data);
            writer.WriteEndDocument();

            writer.Flush();
            return buffer.ToString();
        }

        private static void Serialize(XmlWriter writer, VFXSystemModel system, MetaData data)
        {
            writer.WriteStartElement("System");
            writer.WriteAttributeString("MaxNb", system.MaxNb.ToString());
            writer.WriteAttributeString("SpawnRate", system.SpawnRate.ToString());
            writer.WriteAttributeString("BlendingMode", system.BlendingMode.ToString());
            writer.WriteAttributeString("OrderPriority", system.OrderPriority.ToString());
            writer.WriteAttributeString("ID", system.Id.ToString());
            for (int i = 0; i < system.GetNbChildren(); ++i)
                Serialize(writer,system.GetChild(i),data);
            writer.WriteEndElement();
        }

        private static void Serialize(XmlWriter writer, VFXContextModel context, MetaData data)
        {
            writer.WriteStartElement("Context");
            writer.WriteAttributeString("DescId", context.Desc.Name);
            writer.WriteAttributeString("Position", context.UIPosition.ToString());
            writer.WriteAttributeString("Collapsed", context.UICollapsed.ToString());
            for (int i = 0; i < context.GetNbSlots(); ++i)
                Serialize(writer, context.GetSlot(i),data);
            for (int i = 0; i < context.GetNbChildren(); ++i)
                Serialize(writer, context.GetChild(i),data);
            writer.WriteEndElement();
        }

        private static void Serialize(XmlWriter writer, VFXBlockModel block, MetaData data)
        {
            writer.WriteStartElement("Block");
            writer.WriteAttributeString("DescId", block.Desc.ID);
            writer.WriteAttributeString("Hash", block.Desc.SlotHash.ToString());
            writer.WriteAttributeString("Collapsed", block.UICollapsed.ToString());
            for (int i = 0; i < block.GetNbSlots(); ++i)
                Serialize(writer, block.GetSlot(i), data);
            writer.WriteEndElement();
        }

        private static void Serialize(XmlWriter writer, VFXDataNodeModel dataNode, MetaData data)
        {
            writer.WriteStartElement("DataNode");
            writer.WriteAttributeString("Position", dataNode.UIPosition.ToString());
            writer.WriteAttributeString("Exposed", dataNode.Exposed.ToString());
            for (int i = 0; i < dataNode.GetNbChildren(); ++i)
                Serialize(writer, dataNode.GetChild(i), data);
            writer.WriteEndElement();
        }

        private static void Serialize(XmlWriter writer, VFXDataBlockModel dataBlock, MetaData data)
        {
            writer.WriteStartElement("DataBlock");
            writer.WriteAttributeString("DescId", dataBlock.Desc.Semantics.ID);
            writer.WriteAttributeString("Collapsed", dataBlock.UICollapsed.ToString());
            Serialize(writer, dataBlock.Slot,data);
            writer.WriteEndElement();
        }

        private static void Serialize(XmlWriter writer, VFXPropertySlot slot, MetaData data)
        {
            List<string> values = new List<string>();
            
            var collapsed = RegisterSlot(slot, data);

            writer.WriteStartElement("Slot");

            slot.GetStringValues(values);
            writer.WriteStartElement("Values");
            writer.WriteValue(values);
            writer.WriteEndElement();

            writer.WriteStartElement("Collapsed");
            writer.WriteValue(collapsed);
            writer.WriteEndElement();

            writer.WriteEndElement();
        }

        private static void SerializeConnections(XmlWriter writer,MetaData data)
        {
            writer.WriteStartElement("Connections");
            for (int i = 0; i < data.GetNbSlots(); ++i)
            {
                VFXPropertySlot slot = data.GetSlot(i);
                if (slot.IsLinked() && slot is VFXOutputSlot)
                {
                    var connectedSlots = slot.GetConnectedSlots();
                    
                    List<int> connectedIds = new List<int>();
                    foreach (var connected in connectedSlots)
                            connectedIds.Add(data.GetId(connected));

                    if (connectedIds.Count > 0)
                    {
                        writer.WriteStartElement("Connection");
                        writer.WriteAttributeString("Id", i.ToString());
                        writer.WriteValue(connectedIds);
                        writer.WriteEndElement();
                    }
                }
            }
            writer.WriteEndElement();
        }

        private static List<bool> RegisterSlot(VFXPropertySlot slot,MetaData data,List<bool> collapsed = null)
        {
            if (collapsed == null)
                collapsed = new List<bool>();

            data.AddSlot(slot);

            collapsed.Add(slot.UICollapsed);
            for (int i = 0; i < slot.GetNbChildren(); ++i)
                RegisterSlot(slot.GetChild(i), data, collapsed);

            return collapsed;
        }
    }
}