using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        // 1: Initial
        // 2: block enable toggle
        // 3: data block exposed name
        // 4: soft particle fade distance in system
        // 5: model id for system and spawner node
        // 6: change the way slot connections are serialized
        private const int VERSION = 6;

        // SERIALIZATION
        private class MetaData
        {
            private Dictionary<int, VFXPropertySlot> idsToSlots = new Dictionary<int, VFXPropertySlot>();
            private Dictionary<VFXPropertySlot, int> slotsToIds = new Dictionary<VFXPropertySlot, int>();

            private Dictionary<int, VFXElementModel> idsToModels = new Dictionary<int, VFXElementModel>();
            private Dictionary<VFXElementModel, int> modelsToIds = new Dictionary<VFXElementModel, int>();
   
            private int m_Version = VERSION;

            public int Version
            {
                get { return m_Version; }
                set { m_Version = value; }
            }

            private int m_CurrentModelId = 0;
            public int RegisterModel(VFXElementModel model)
            {
                return RegisterModel(model, m_CurrentModelId++);
            }
            public int RegisterModel(VFXElementModel model, int id)    
            {
                modelsToIds.Add(model, id);
                idsToModels.Add(id, model);
                return id;
            }
            public VFXElementModel GetModel(int id)         { return idsToModels[id]; }
            public int GetModelId(VFXElementModel model)    { return modelsToIds[model]; }

            private int m_CurrentSlotId = 0;
            public int RegisterSlot(VFXPropertySlot slot)
            {
                return RegisterSlot(slot,m_CurrentSlotId++);
            }
            public int RegisterSlot(VFXPropertySlot slot,int id)
            {
                slotsToIds.Add(slot, id);
                idsToSlots.Add(id, slot);
                return id;
            }

            public IEnumerable<VFXPropertySlot> GetSlots()   { return slotsToIds.Keys; }
            public VFXPropertySlot GetSlot(int id)           { return idsToSlots[id]; }
            public int GetSlotId(VFXPropertySlot slot)       { return slotsToIds[slot]; }
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
                data.Version = VERSION;

                writer.WriteStartDocument();
                writer.WriteStartElement("Graph");
                writer.WriteAttributeString("Version", VERSION.ToString());

                for (int i = 0; i < graph.systems.GetNbChildren(); ++i)
                    Serialize(writer, graph.systems.GetChild(i), data);

                List<VFXSpawnerNodeModel> spawners = new List<VFXSpawnerNodeModel>(); // keep tracks of spawners to write connections at the end
                for (int i = 0; i < graph.models.GetNbChildren(); ++i)
                {
                    var model = graph.models.GetChild(i);
                    Type modelType = model.GetType();
                    if (modelType == typeof(VFXDataNodeModel))
                        Serialize(writer, (VFXDataNodeModel)model, data);
                    else if (modelType == typeof(VFXCommentModel))
                        Serialize(writer, (VFXCommentModel)model, data);
                    else if (modelType == typeof(VFXSpawnerNodeModel))
                    {
                        var spawnerModel = (VFXSpawnerNodeModel)model;
                        Serialize(writer, spawnerModel, data);
                        spawners.Add(spawnerModel);
                    }
                    else
                        Debug.LogWarning("Cannot serialize model of type: " + modelType);
                }

                SerializeConnections(writer, data);

                foreach (var spawner in spawners)
                    SerializeSpawnerConnections(writer, spawner, data);

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

        private static void SerializeModelId(XmlWriter writer, VFXElementModel model, MetaData data)
        {
            int id = data.RegisterModel(model);
            writer.WriteAttributeString("ModelId", id.ToString());
        }

        private static void Serialize(XmlWriter writer, VFXSystemModel system, MetaData data)
        {
            writer.WriteStartElement("System");
            SerializeModelId(writer, system, data);
            writer.WriteAttributeString("MaxNb", system.MaxNb.ToString());
            writer.WriteAttributeString("SpawnRate", system.SpawnRate.ToString());
            writer.WriteAttributeString("BlendingMode", system.BlendingMode.ToString());
            writer.WriteAttributeString("SoftParticlesFadeDistance", system.SoftParticlesFadeDistance.ToString());
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
            writer.WriteAttributeString("Enabled", block.Enabled.ToString()); // version 2
            for (int i = 0; i < block.GetNbSlots(); ++i)
                Serialize(writer, block.GetSlot(i), data);
            writer.WriteEndElement();
        }

        private static void Serialize(XmlWriter writer, VFXDataNodeModel dataNode, MetaData data)
        {
            writer.WriteStartElement("DataNode");
            SerializeModelId(writer, dataNode, data);
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
            writer.WriteAttributeString("ExposedName", dataBlock.ExposedName.ToString());
            Serialize(writer, dataBlock.Slot,data);
            writer.WriteEndElement();
        }

        private static void Serialize(XmlWriter writer, VFXCommentModel comment, MetaData data)
        {
            writer.WriteStartElement("Comment");
            writer.WriteAttributeString("Position", SerializationUtils.FromVector2(comment.UIPosition));
            writer.WriteAttributeString("Size", SerializationUtils.FromVector2(comment.UISize));
            writer.WriteAttributeString("Title", comment.Title);
            writer.WriteAttributeString("Body", comment.Body);
            writer.WriteAttributeString("Color", SerializationUtils.FromColor(comment.Color));
            writer.WriteEndElement();
        }

        private static void Serialize(XmlWriter writer, VFXSpawnerNodeModel spawnerNode, MetaData data)
        {
            writer.WriteStartElement("SpawnerNode");
            SerializeModelId(writer, spawnerNode, data);
            writer.WriteAttributeString("Position", SerializationUtils.FromVector2(spawnerNode.UIPosition));
            for (int i = 0; i < spawnerNode.GetNbChildren(); ++i)
                Serialize(writer, spawnerNode.GetChild(i), data);
            writer.WriteEndElement();
        }

        private static void Serialize(XmlWriter writer, VFXSpawnerBlockModel spawnerBlock, MetaData data)
        {
            writer.WriteStartElement("SpawnerBlock");
            writer.WriteAttributeString("Type", spawnerBlock.SpawnerType.ToString());
            writer.WriteAttributeString("Collapsed", spawnerBlock.UICollapsed.ToString());
            for (int i = 0; i < spawnerBlock.GetNbInputSlots(); ++i)
                Serialize(writer, spawnerBlock.GetInputSlot(i), data);
            writer.WriteEndElement();
        }

        private static void Serialize(XmlWriter writer, VFXPropertySlot slot, MetaData data)
        {
            writer.WriteStartElement("Slot");

            var collapsed = RegisterSlot(slot, data);

            writer.WriteAttributeString("SlotId", data.GetSlotId(slot).ToString());

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

            foreach (var slot in data.GetSlots())
            {
                if (slot.IsLinked() && slot is VFXOutputSlot)
                {
                    var connectedSlots = slot.GetConnectedSlots();
                    
                    List<int> connectedIds = new List<int>();
                    foreach (var connected in connectedSlots)
                            connectedIds.Add(data.GetSlotId(connected));

                    if (connectedIds.Count > 0) 
                    {
                        writer.WriteStartElement("Connection");
                        writer.WriteAttributeString("Id", data.GetSlotId(slot).ToString());
                        writer.WriteValue(connectedIds);
                        writer.WriteEndElement();
                    }
                }
            }
            writer.WriteEndElement();
        }

        private static void SerializeSpawnerConnections(XmlWriter writer,VFXSpawnerNodeModel spawnerNode,MetaData data)
        {
            if (spawnerNode.GetNbLinked() == 0)
                return;

            writer.WriteStartElement("SpawnerConnections");
            int spawnerId = data.GetModelId(spawnerNode);
            writer.WriteAttributeString("Id", spawnerId.ToString());

            List<int> linkedIds = new List<int>();
            for (int i = 0; i < spawnerNode.GetNbLinked(); ++i)
            {
                VFXSystemModel system = spawnerNode.GetLinked(i).GetOwner();
                linkedIds.Add(data.GetModelId(system));
            }

            writer.WriteValue(linkedIds);
            writer.WriteEndElement();
        }

        private static List<bool> RegisterSlot(VFXPropertySlot slot,MetaData data,List<bool> collapsed = null,int id = -1)
        {
            if (collapsed == null)
                collapsed = new List<bool>();

            if (id == -1)
                data.RegisterSlot(slot);
            else
                data.RegisterSlot(slot, id);
            collapsed.Add(slot.UICollapsed);

            for (int i = 0; i < slot.GetNbChildren(); ++i)
                RegisterSlot(slot.GetChild(i), data, collapsed, id == -1 ? -1 : id + collapsed.Count);

            return collapsed;
        }


        // DESERIALIZATION
        public static VFXGraph Deserialize(string xml) 
        {
            VFXGraph graph = new VFXGraph(); // TMP Needs to remove RTData from graph

            if (xml.Length == 0) // To avoid exception with newly created assets
                return graph;

            try
            {
                var data = new MetaData();

                var doc = XDocument.Parse(xml);
                var root = doc.Element("Graph");

                data.Version = int.Parse(root.Attribute("Version").Value);

                var systemsXML = root.Elements("System");
                var dataNodesXML = root.Elements("DataNode");
                var commentsXML = root.Elements("Comment");
                var spawnersXML = root.Elements("SpawnerNode");
                var connectionsXML = root.Element("Connections");

                var spawnerConnectionsXML = root.Elements("SpawnerConnections");

                foreach (var systemXML in systemsXML)
                    graph.systems.AddChild(DeserializeSystem(systemXML, data));

                foreach (var dataNodeXML in dataNodesXML)
                    graph.models.AddChild(DeserializeDataNode(dataNodeXML, data));

                foreach (var commentXML in commentsXML)
                    graph.models.AddChild(DeserializeComment(commentXML, data));

                foreach (var spawnerXML in spawnersXML)
                    graph.models.AddChild(DeserializeSpawnerNode(spawnerXML, data));

                DeserializeConnections(connectionsXML, data);

                foreach (var spawnerXML in spawnerConnectionsXML)
                    DeserializeSpawnerConnections(spawnerXML, data);
            }
            catch(Exception e)
            {
                Debug.LogError("Exception while deserializing graph: " + e.ToString());
                graph = new VFXGraph();
            }

            return graph;
        }

        private static void DeserializeModelId(XElement xml, VFXElementModel model, MetaData data)
        {
            if (data.Version >= 5)
            {
                int id = int.Parse(xml.Attribute("ModelId").Value);
                data.RegisterModel(model, id);
            }
        }

        private static VFXSystemModel DeserializeSystem(XElement xml, MetaData data)
        {
            var system = new VFXSystemModel();
            DeserializeModelId(xml, system, data);
            system.MaxNb = uint.Parse(xml.Attribute("MaxNb").Value);
            system.SpawnRate = float.Parse(xml.Attribute("SpawnRate").Value);
            system.BlendingMode = (BlendMode)Enum.Parse(typeof(BlendMode), xml.Attribute("BlendingMode").Value);
            if (data.Version >= 4)
                system.SoftParticlesFadeDistance = float.Parse(xml.Attribute("SoftParticlesFadeDistance").Value);
            system.OrderPriority = int.Parse(xml.Attribute("OrderPriority").Value);

            foreach (var contextXML in xml.Elements("Context"))
            {
                var context = DeserializeContext(contextXML,data);
                system.AddChild(context);
            }

            return system;
        }

        private static VFXContextModel DeserializeContext(XElement xml, MetaData data)
        {
            var descId = xml.Attribute("DescId").Value;
            var desc = VFXEditor.ContextLibrary.GetContext(descId);

            var context = new VFXContextModel(desc);
            context.UpdatePosition(SerializationUtils.ToVector2(xml.Attribute("Position").Value));
            context.UpdateCollapsed(bool.Parse(xml.Attribute("Collapsed").Value));

            foreach (var blockXML in xml.Elements("Block"))
            {
                var block = DeserializeBlock(blockXML, data);
                context.AddChild(block);
            }

            int index = 0;
            foreach (var slotXML in xml.Elements("Slot"))
                DeserializeSlot(slotXML, context.GetSlot(index++), data); // TODO Should have a hash for context blocks too

            return context;
        }

        private static VFXBlockModel DeserializeBlock(XElement xml, MetaData data)
        {
            var descId = xml.Attribute("DescId").Value;
            var desc = VFXEditor.BlockLibrary.GetBlock(descId);

            var block = new VFXBlockModel(desc);
            block.UpdateCollapsed(bool.Parse(xml.Attribute("Collapsed").Value));

            bool hashTest = int.Parse(xml.Attribute("Hash").Value) == desc.SlotHash; // Check whether serialized slot data is compatible with current slots
            if (!hashTest)
                Debug.LogWarning("Slots configuration has changed between serialized data and current data. Slots cannot be deserialized for block " + desc);

            if (data.Version >= 2)
                block.Enabled = bool.Parse(xml.Attribute("Enabled").Value);

            int index = 0;
            foreach (var slotXML in xml.Elements("Slot"))
                DeserializeSlot(slotXML, hashTest ? block.GetSlot(index++) : null, data);

            return block;
        }

        private static VFXDataNodeModel DeserializeDataNode(XElement xml, MetaData data)
        {
            var dataNode = new VFXDataNodeModel();
            dataNode.UpdatePosition(SerializationUtils.ToVector2(xml.Attribute("Position").Value));
            dataNode.Exposed = bool.Parse(xml.Attribute("Exposed").Value);

            foreach (var blockXML in xml.Elements("DataBlock"))
            {
                var block = DeserializeDataBlock(blockXML, data);
                dataNode.AddChild(block);
            }

            return dataNode;
        }

        private static VFXDataBlockModel DeserializeDataBlock(XElement xml, MetaData data)
        {
            var descId = xml.Attribute("DescId").Value;
            var desc = VFXEditor.BlockLibrary.GetDataBlock(descId);

            var block = new VFXDataBlockModel(desc);
            block.UpdateCollapsed(bool.Parse(xml.Attribute("Collapsed").Value));
            if (data.Version >= 3)
                block.ExposedName = xml.Attribute("ExposedName").Value;

            DeserializeSlot(xml.Element("Slot"), block.Slot, data);

            return block;          
        }

        private static VFXSpawnerNodeModel DeserializeSpawnerNode(XElement xml, MetaData data)
        {
            var spawnerNode = new VFXSpawnerNodeModel();
            DeserializeModelId(xml, spawnerNode, data);
            spawnerNode.UpdatePosition(SerializationUtils.ToVector2(xml.Attribute("Position").Value));

            foreach (var blockXML in xml.Elements("SpawnerBlock"))
            {
                var block = DeserializeSpawnerBlock(blockXML, data);
                spawnerNode.AddChild(block);
            }

            return spawnerNode;
        }

        private static VFXSpawnerBlockModel DeserializeSpawnerBlock(XElement xml, MetaData data)
        {
            var spawnerType = (VFXSpawnerBlockModel.Type)Enum.Parse(typeof(VFXSpawnerBlockModel.Type), xml.Attribute("Type").Value);
            var block = new VFXSpawnerBlockModel(spawnerType);
            block.UpdateCollapsed(bool.Parse(xml.Attribute("Collapsed").Value));

            int index = 0;
            foreach (var slotXML in xml.Elements("Slot"))
                DeserializeSlot(slotXML, block.GetInputSlot(index++), data);

            return block;
        }

        private static VFXCommentModel DeserializeComment(XElement xml, MetaData data)
        {
            var comment = new VFXCommentModel();
            comment.UIPosition = SerializationUtils.ToVector2(xml.Attribute("Position").Value);
            comment.UISize = SerializationUtils.ToVector2(xml.Attribute("Size").Value);
            comment.Title = xml.Attribute("Title").Value;
            comment.Body = xml.Attribute("Body").Value;
            comment.Color = SerializationUtils.ToColor(xml.Attribute("Color").Value);
            return comment;    
        }

        private static void DeserializeSlot(XElement xml, VFXPropertySlot dst, MetaData data)
        {
            if (dst != null)
            {
                var id = -1;
                if (data.Version >= 6)
                    id = int.Parse(xml.Attribute("SlotId").Value);
                RegisterSlot(dst, data, null, id); 

                var values = xml.Element("Values");
                var reader = values.CreateReader();
                reader.ReadToFollowing("Values");
                while (reader.Read() && reader.NodeType != XmlNodeType.Element) { } // Advance to element
                dst.SetValuesFromString(reader);
            }

            var collapsed = xml.Element("Collapsed");
            string[] collapsedStr = collapsed.Value.Split(' ');
            bool[] collapsedValues = new bool[collapsedStr.Length];
            for (int i = 0; i < collapsedStr.Length; ++i)
                collapsedValues[i] = bool.Parse(collapsedStr[i]);
        }

        private static void DeserializeConnections(XElement xml,MetaData data)
        {
            foreach (var connectionXML in xml.Elements("Connection"))
            {
                var slotId = int.Parse(connectionXML.Attribute("Id").Value);
                var slot = data.GetSlot(slotId);

                string[] connectedStr = connectionXML.Value.Split(' ');
                for (int i = 0; i < connectedStr.Length; ++i)
                {
                    var connectedSlotId = int.Parse(connectedStr[i]);
                    var connectedSlot = data.GetSlot(connectedSlotId);
                    if (connectedSlot != null)
                        slot.Link(connectedSlot);
                    else
                        Debug.LogWarning("Cannot connect slots " + slotId + " and " + connectedSlotId + " as the lastest was invalidated");
                }
            }
        }

        private static void DeserializeSpawnerConnections(XElement xml,MetaData data)
        {
            var spawnerId = int.Parse(xml.Attribute("Id").Value);
            var model = (VFXSpawnerNodeModel)data.GetModel(spawnerId);

            string[] linkedStr = xml.Value.Split(' ');
            var linkedIds = linkedStr.Select(str => int.Parse(str));
            foreach (var linkedId in linkedIds)
            {
                VFXContextModel context = ((VFXSystemModel)data.GetModel(linkedId)).GetChild(0);
                model.Link(context);
            }
        }
    }
}
