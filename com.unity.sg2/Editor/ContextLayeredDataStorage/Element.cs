using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.ContextLayeredDataStorage
{
    public class Element
    {
        public ElementID ID { get; internal set; }
        public DataHeader Header { get; internal set; }
        public List<Element> Children { get; internal set; }
        public Element Parent { get; internal set; }
        internal ContextLayeredDataStorage owner;
        internal string serializedData;

        public Element(ContextLayeredDataStorage owner)
        {
            ID = new ElementID("");
            Header = new DataHeader();
            Parent = null;
            Children = new List<Element>();
            this.owner = owner;
        }

        public Element(ContextLayeredDataStorage owner, DataHeader header)
        {
            ID = new ElementID("");
            this.Header = header;
            Parent = null;
            Children = new List<Element>();
            this.owner = owner;
        }

        public Element(ElementID id, ContextLayeredDataStorage owner) : this(owner)
        {
            this.ID = id;
        }

        public Element(ElementID id, ContextLayeredDataStorage owner, DataHeader header) : this(owner, header)
        {
            this.ID = id;
        }

        public T GetData<T>()
        {
            return (this as Element<T>).Data;
        }

        public DataReader GetReader()
        {
            return Header.GetReader(this);
        }

        public DataWriter GetWriter()
        {
            return Header.GetWriter(this);
        }

        internal virtual SerializedElementData ToSerializedFormat()
        {
            return new SerializedElementData(ID, Header.GetType().AssemblyQualifiedName, Header.ToJson(), null, null);
        }

        public virtual Element MakeCopy()
        {
           return new Element(ID, owner, Header.MakeCopy());
        }

        public ElementID GetUniqueLocalID(string desiredLocalPath)
        {
            return ElementID.CreateUniqueLocalID(Parent != null ? Parent.ID : "",
                                                 owner.GetFlatImmediateChildList(Parent).Select(e => e.ID.LocalPath),
                                                 ID.LocalPath);

        }
    }

    public class Element<T> : Element
    {
        internal T m_Data;
        public ref readonly T Data => ref m_Data;

        [Serializable]
        internal struct DataBox
        {
            public T m_data;
        }


        public Element(ElementID id, T data, ContextLayeredDataStorage owner) : base(id, owner)
        {
            m_Data = data;
        }
        public Element(ElementID id, T data, ContextLayeredDataStorage owner, DataHeader header) : base(id, owner, header)
        {
            m_Data = data;
        }


        internal override SerializedElementData ToSerializedFormat()
        {
            try
            {
                return new SerializedElementData(ID, Header.GetType().AssemblyQualifiedName, Header.ToJson(), typeof(T).AssemblyQualifiedName, JsonUtility.ToJson(new DataBox() { m_data = Data }, true));
            }
            catch
            {
                Debug.LogError($"Could not serialize data associated with {ID.FullPath}: {Data}");
                return new SerializedElementData(ID, Header.GetType().AssemblyQualifiedName, Header.ToJson(), typeof(T).AssemblyQualifiedName, null);
            }
        }

        public override Element MakeCopy()
        {
            return new Element<T>(ID, m_Data, owner, Header.MakeCopy());
        }
    }

    //Used to organize elements when serialized to try and keep a consistent ordering
    internal class SerializedDataComparer : IComparer<SerializedElementData>
    {
        public int Compare(SerializedElementData x, SerializedElementData y)
        {
            return x.id.CompareTo(y.id);
        }
    }

    //Stores a single Element's data 
    [Serializable]
    internal struct SerializedElementData
    {
        public string id;
        public string headerType;
        public string headerData;
        public string valueType;
        public string valueData;

        public SerializedElementData(ElementID id, string headerType, string headerData, string valueType, string valueData)
        {
            this.id = id.FullPath;
            this.headerType = headerType;
            this.headerData = headerData;
            this.valueType = valueType;
            this.valueData = valueData;
        }
    }



}
