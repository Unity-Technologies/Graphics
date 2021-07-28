using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public interface IDataElement
    {
        public bool TryGetData<T>(out T data);
        public string ID { get; }

        public IEnumerator<IDataElement> Children { get; }

        public IDataElement Parent { get; }
    }

    internal class ContextLayeredDataStorage
    {
        protected class Element : IDataElement
        {

            internal string m_id;
            public string ID => m_id;

            internal List<Element> m_children;
            public IEnumerator<IDataElement> Children => m_children.GetEnumerator();

            internal Element m_parent;
            public IDataElement Parent => m_parent;

            internal ContextLayeredDataStorage owner;

            internal Element(ContextLayeredDataStorage owner)
            {
                m_id = "";
                m_parent = null;
                m_children = new List<Element>();
                this.owner = owner;
            }

            internal Element(string id, ContextLayeredDataStorage owner) : this(owner)
            {
                m_id = id;
            }

            //It is a path id if there are any '.' characters
            public static bool IsPath(string id)
            {
                return id.IndexOf('.') >= 0;
            }

            public void AddChild(Element child)
            {
                child.m_parent = this;
                m_children.Add(child);
            }

            public void LinkElement(Element other)
            {
                other.AddChild(this);
                this.AddChild(other);
            }

            public Element<T> AddData<T>(string id, T data)
            {
                //not a path? Add it directly as a child
                if (!IsPath(id))
                {
                    var output = new Element<T>(id, data, owner);
                    AddChild(output);
                    return output;
                }
                else
                {
                    //is a path? see if it matches any children
                    foreach (Element child in m_children)
                    {
                        if (id.IndexOf(child.ID) == 0)
                        {
                            return child.AddData(id.Substring(child.m_id.Length).TrimStart('.'), data);
                        }
                    }
                    //otherwise, add as child with full name
                    var output = new Element<T>(id, data, owner);
                    AddChild(output);
                    return output;
                }
            }

            public Element AddData(string id)
            {
                //not a path? Add it directly as a child
                if (!IsPath(id))
                {
                    var output = new Element(id, owner);
                    AddChild(output);
                    return output;
                }
                else
                {
                    //is a path? see if it matches any children
                    foreach (Element child in m_children)
                    {
                        if (id.IndexOf(child.m_id) == 0)
                        {
                            return child.AddData(id.Substring(child.m_id.Length).TrimStart('.'));
                        }
                    }
                    //otherwise, add as child with full name
                    var output = new Element(id, owner);
                    AddChild(output);
                    return output;
                }
            }

            public void RemoveChild(Element child)
            {
                child.m_parent = null;
                m_children.Remove(child);
            }
            public void Remove()
            {
                foreach (var child in m_children)
                {
                    child.m_id = $"{m_id}.{child.m_id}";
                    if (m_parent != null)
                    {
                        RemoveChild(child);
                        m_parent.AddChild(child);
                        break;

                    }
                }
                if (m_parent != null)
                {
                    m_parent.RemoveChild(this);
                }
            }

            public void RemoveWithoutFix()
            {
                if(m_parent != null)
                {
                    m_parent.RemoveChild(this);
                }
            }

            public bool TryGetData<T>(out T data)
            {
                var isDataHolder = this as Element<T>;
                if(isDataHolder != null)
                {
                    data = isDataHolder.data;
                    return true;
                }
                else
                {
                    data = default(T);
                    return false;
                }
            }
        }

        protected class Element<T> : Element, IDataElement
        {
            public T data;

            internal Element(string id, T data, ContextLayeredDataStorage owner) : base(id,owner)
            {
                this.data = data;
            }
        }

        public struct LayerID
        {
            public string name;
        }

        private class ReverseIntComparer : IComparer<int>
        {
            //reverse order since all our searching will check highest layer first
            public int Compare(int x, int y) => x.CompareTo(y) * -1;
        }

        private readonly SortedList<int, (LayerID layerID, Element element)> m_layerList;
        public ContextLayeredDataStorage()
        {
            m_layerList = new SortedList<int, (LayerID layerID, Element element)>(new ReverseIntComparer());
            m_layerList.Add(-1, (new LayerID() { name = "Root" }, new Element(this)));
        }

        public void AddNewTopLayer(string layerName)
        {
            if (string.IsNullOrEmpty(layerName))
            {
                throw new System.ArgumentException("Cannor use a null or empty layer name", "layerName");
            }
            m_layerList.Add(m_layerList.Keys[0] + 1, (new LayerID() { name = layerName }, new Element(this)));
        }

        //AddData with no specified layer gets added to the topmost layer
        protected void AddData<T>(string id, T data, out Element<T> elem)
        {
            AddData(m_layerList[m_layerList.Keys[0]].element, id, data, out elem);
        }

        public IDataElement AddData<T>(string id, T data)
        {
            AddData(id, data, out Element<T> element);
            return element;
        }

        protected void AddData(string id, out Element elem)
        {
            AddData(m_layerList[m_layerList.Keys[0]].element, id, out elem);
        }

        public IDataElement AddData(string id)
        {
            AddData(id, out Element elem);
            return elem;
        }

        protected void AddData<T>(LayerID layer, string id, T data, out Element<T> elem)
        {
            elem = null;
            foreach(var l in m_layerList)
            {
                if(string.CompareOrdinal(l.Value.layerID.name, layer.name) == 0)
                {
                    AddData(l.Value.element, id, data, out elem);
                }
            }
        }

        public IDataElement AddData<T>(string layer, string id, T data)
        {
            AddData(new LayerID() { name = layer }, id, data, out Element<T> elem);
            return elem;
        }

        protected void AddData(LayerID layer, string id, out Element elem)
        {
            elem = null;
            foreach (var l in m_layerList)
            {
                if (string.CompareOrdinal(l.Value.layerID.name, layer.name) == 0)
                {
                    AddData(l.Value.element, id, out elem);
                }
            }
        }

        public IDataElement AddData(LayerID layer, string id)
        {
            AddData(layer, id, out Element elem);
            return elem;
        }

        protected void AddData<T>(Element elem, string id, T data, out Element<T> output)
        {
            output = elem.AddData(id, data);
        }

        protected void AddData(Element elem, string id, out Element output)
        {
            output = elem.AddData(id);
        }

        public void RemoveData(string id)
        {
            if(string.IsNullOrEmpty(id))
            {
                throw new System.ArgumentException("Cannot remove a null or empty string id", "id");
            }
            SearchInternal(id)?.Remove();
        }

        protected void RemoveData(Element elem)
        {
            if(elem.m_id.Length == 0 && elem.m_parent == null)
            {
                throw new System.ArgumentException("Cannot remove the root element of a layer", "elem");
            }
            elem.Remove();
        }

        public IDataElement Search(string lookup)
        {
            return SearchInternal(lookup);
        }

        protected Element SearchInternal(string lookup)
        {
            foreach(var layer in m_layerList)
            {
                Element elem = SearchRecurse(layer.Value.element, lookup);
                if(elem != null)
                {
                    return elem;
                }
            }
            return null;
        }

        protected Element SearchRelative(Element elem, string lookup)
        {
            if(elem.m_children.Count > 0)
            {
                Element output = null;
                foreach(Element child in elem.m_children)
                {
                    output = SearchRecurse(child, lookup);
                    if(output != null)
                    {
                        return output;
                    }
                }
            }
            return null;
        }

        //may rewrite as non recursive
        protected Element SearchRecurse(Element elem, string lookup)
        {
            if (string.CompareOrdinal(elem.m_id, lookup) == 0)
            {
                return elem;
            }

            if(lookup.IndexOf(elem.m_id) == 0 && elem.m_children.Count > 0)
            {
                Element output = null;
                string nextLookup = lookup.Substring(elem.m_id.Length).TrimStart('.');
                foreach(Element child in elem.m_children)
                {
                    output = SearchRecurse(child, nextLookup);
                    if(output != null)
                    {
                        return output;
                    }
                }
            }
            return null;
        }

        protected bool Contains(Element elem)
        {
            return elem.owner == this;
        }
    }
}
