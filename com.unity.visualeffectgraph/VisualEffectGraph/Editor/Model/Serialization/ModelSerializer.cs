using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental.VFX
{
    public static class SerializationUtils
    {
        public static string FromVector2(Vector2 v)
        {
            return v.x + "," + v.y;
        }

        public static Vector2 ToVector2(string v)
        {
            var components = v.Split(',');
            return new Vector2(float.Parse(components[0]), float.Parse(components[1]));
        }

        public static string FromColor(Color c)
        {
            return c.r + "," + c.g + "," + c.b + "," + c.a;
        }

        public static Color ToColor(string c)
        {
            var components = c.Split(',');
            return new Color(
                float.Parse(components[0]), 
                float.Parse(components[1]),
                float.Parse(components[2]),
                float.Parse(components[3]));
        }

        public static void WriteCurve(XmlWriter writer,AnimationCurve curve)
        {
            writer.WriteStartElement(VFXValueType.kCurve.ToString());
            writer.WriteAttributeString("PreWrapMode", curve.preWrapMode.ToString());
            writer.WriteAttributeString("PostWrapMode", curve.postWrapMode.ToString());
            foreach (var key in curve.keys)
            {
                writer.WriteStartElement("KeyFrame");
                writer.WriteAttributeString("t", key.time.ToString());
                writer.WriteAttributeString("v", key.value.ToString());
                writer.WriteAttributeString("inT", key.inTangent.ToString());
                writer.WriteAttributeString("outT", key.outTangent.ToString());
                writer.WriteAttributeString("tMode", key.tangentMode.ToString());
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        public static AnimationCurve ReadCurve(XmlReader reader)
        {
            var curve = new AnimationCurve();

            curve.preWrapMode = (WrapMode)Enum.Parse(typeof(WrapMode), reader.GetAttribute("PreWrapMode"));
            curve.postWrapMode = (WrapMode)Enum.Parse(typeof(WrapMode), reader.GetAttribute("PostWrapMode"));

            reader.Read();
            while (reader.IsStartElement("KeyFrame"))
            {
                Keyframe key = new Keyframe();
                key.time = float.Parse(reader.GetAttribute("t"));
                key.value = float.Parse(reader.GetAttribute("v"));
                key.inTangent = float.Parse(reader.GetAttribute("inT"));
                key.outTangent = float.Parse(reader.GetAttribute("outT"));
                key.tangentMode = int.Parse(reader.GetAttribute("tMode"));
                curve.AddKey(key);
                reader.Read();
            }

            return curve;
        }

        public static void WriteGradient(XmlWriter writer, Gradient gradient)
        {
            writer.WriteStartElement(VFXValueType.kColorGradient.ToString());
            foreach (var key in gradient.colorKeys)
            {
                writer.WriteStartElement("ColorKey");
                writer.WriteAttributeString("t", key.time.ToString());
                writer.WriteAttributeString("v", SerializationUtils.FromColor(key.color));
                writer.WriteEndElement();
            }
            foreach (var key in gradient.alphaKeys)
            {
                writer.WriteStartElement("AlphaKey");
                writer.WriteAttributeString("t", key.time.ToString());
                writer.WriteAttributeString("v", key.alpha.ToString());
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        public static Gradient ReadGradient(XmlReader reader)
        {
            var colorKeys = new List<GradientColorKey>();
            var alphaKeys = new List<GradientAlphaKey>();

            reader.Read();

            while (reader.IsStartElement("ColorKey"))
            {
                var time = float.Parse(reader.GetAttribute("t"));
                var color = SerializationUtils.ToColor(reader.GetAttribute("v"));
                colorKeys.Add(new GradientColorKey(color,time));
                reader.Read();
            }

            while (reader.IsStartElement("AlphaKey"))
            {
                var time = float.Parse(reader.GetAttribute("t"));
                var alpha = float.Parse(reader.GetAttribute("v"));
                alphaKeys.Add(new GradientAlphaKey(alpha, time));
                reader.Read();
            }

            var gradient = new Gradient();
            gradient.SetKeys(colorKeys.ToArray(), alphaKeys.ToArray());
            return gradient;
        }
    }

    public static class ModelSerializer
    {
        // SERIALIZATION
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
            string res = null;
            try
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
                writer.WriteStartElement("Graph");
                writer.WriteAttributeString("Version", "1");

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

                writer.WriteEndElement();
                writer.WriteEndDocument();

                writer.Flush();
                res = buffer.ToString();
            }
            catch(Exception e)
            {
                Debug.LogError("Exception while serializing graph: " + e.ToString());
                res = null;
            }

            return res;
        }

        private static void Serialize(XmlWriter writer, VFXSystemModel system, MetaData data)
        {
            writer.WriteStartElement("System");
            writer.WriteAttributeString("MaxNb", system.MaxNb.ToString());
            writer.WriteAttributeString("SpawnRate", system.SpawnRate.ToString());
            writer.WriteAttributeString("BlendingMode", system.BlendingMode.ToString());
            writer.WriteAttributeString("OrderPriority", system.OrderPriority.ToString());
            for (int i = 0; i < system.GetNbChildren(); ++i)
                Serialize(writer,system.GetChild(i),data);
            writer.WriteEndElement();
        }

        private static void Serialize(XmlWriter writer, VFXContextModel context, MetaData data)
        {
            writer.WriteStartElement("Context");
            writer.WriteAttributeString("DescId", context.Desc.Name);
            writer.WriteAttributeString("Position", SerializationUtils.FromVector2(context.UIPosition));
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
            writer.WriteAttributeString("Position", SerializationUtils.FromVector2(dataNode.UIPosition));
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
            var collapsed = RegisterSlot(slot, data);

            writer.WriteStartElement("Slot");

            writer.WriteStartElement("Values");
            slot.GetStringValues(writer);
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


        // DESERIALIZATION
        public static VFXGraph Deserialize(string xml) 
        {
            VFXGraph graph = new VFXGraph(); // TMP Needs to remove RTData from graph
            try
            {
                var doc = XDocument.Parse(xml);
                var root = doc.Element("Graph");

                var systemsXML = root.Elements("System");
                var dataNodesXML = root.Elements("DataNode");
                var connectionsXML = root.Elements("Connections");

                foreach (var systemXML in systemsXML)
                    graph.systems.AddChild(DeserializeSystem(systemXML));

                foreach (var dataNodeXML in dataNodesXML)
                    graph.models.AddChild(DeserializeDataNode(dataNodeXML));
            }
            catch(Exception e)
            {
                Debug.LogError("Exception while deserializing graph: " + e.ToString());
                graph = null;
            }

            return graph;
        }

        private static VFXSystemModel DeserializeSystem(XElement xml)
        {
            var system = new VFXSystemModel();
            system.MaxNb = uint.Parse(xml.Attribute("MaxNb").Value);
            system.SpawnRate = float.Parse(xml.Attribute("SpawnRate").Value);
            system.BlendingMode = (BlendMode)Enum.Parse(typeof(BlendMode), xml.Attribute("BlendingMode").Value);
            system.OrderPriority = int.Parse(xml.Attribute("OrderPriority").Value);
            //system.Id = uint.Parse(systemXML.Attribute("ID").Value);

            foreach (var contextXML in xml.Elements("Context"))
            {
                var context = DeserializeContext(contextXML);
                system.AddChild(context);
            }

            return system;
        }

        private static VFXContextModel DeserializeContext(XElement xml)
        {
            var descId = xml.Attribute("DescId").Value;
            var desc = VFXEditor.ContextLibrary.GetContext(descId);

            var context = new VFXContextModel(desc);
            context.UpdatePosition(SerializationUtils.ToVector2(xml.Attribute("Position").Value));
            context.UpdateCollapsed(bool.Parse(xml.Attribute("Collapsed").Value));

            foreach (var blockXML in xml.Elements("Block"))
            {
                var block = DeserializeBlock(blockXML);
                context.AddChild(block);
            }

            int index = 0;
            foreach (var slotXML in xml.Elements("Slot"))
                DeserializeSlot(slotXML, context.GetSlot(index++));

            return context;
        }

        private static VFXBlockModel DeserializeBlock(XElement xml)
        {
            var descId = xml.Attribute("DescId").Value;
            var desc = VFXEditor.BlockLibrary.GetBlock(descId);

            var block = new VFXBlockModel(desc);
            block.UpdateCollapsed(bool.Parse(xml.Attribute("Collapsed").Value));

            int index = 0;
            foreach (var slotXML in xml.Elements("Slot"))
                DeserializeSlot(slotXML, block.GetSlot(index++));

            return block;
        }

        private static VFXDataNodeModel DeserializeDataNode(XElement xml)
        {
            var dataNode = new VFXDataNodeModel();
            dataNode.UpdatePosition(SerializationUtils.ToVector2(xml.Attribute("Position").Value));
            dataNode.Exposed = bool.Parse(xml.Attribute("Exposed").Value);

            foreach (var blockXML in xml.Elements("DataBlock"))
            {
                var block = DeserializeDataBlock(blockXML);
                dataNode.AddChild(block);
            }

            return dataNode;
        }

        private static VFXDataBlockModel DeserializeDataBlock(XElement xml)
        {
            var descId = xml.Attribute("DescId").Value;
            var desc = VFXEditor.BlockLibrary.GetDataBlock(descId);

            var block = new VFXDataBlockModel(desc);
            block.UpdateCollapsed(bool.Parse(xml.Attribute("Collapsed").Value));

            DeserializeSlot(xml.Element("Slot"), block.Slot);

            return block;          
        }

        private static void DeserializeSlot(XElement xml,VFXPropertySlot dst)
        {
            var values = xml.Element("Values");
            var reader = values.CreateReader();
            reader.ReadToFollowing("Values");
            while (reader.Read() && reader.NodeType != XmlNodeType.Element) { } // Advance to element
            dst.SetValuesFromString(reader);

            var collapsed = xml.Element("Collapsed");
            string[] collapsedStr = collapsed.Value.Split(' ');
            bool[] collapsedValues = new bool[collapsedStr.Length];
            for (int i = 0; i < collapsedStr.Length; ++i)
                collapsedValues[i] = bool.Parse(collapsedStr[i]);

            /*int index = 0;
            VFXPropertySlot slot = dst;
            for (int i = 0; i < collapsedStr.Length; ++i)
            {
                slot.UpdateCollapsed(collapsedValues[i])
            }*/
        }
    }
}