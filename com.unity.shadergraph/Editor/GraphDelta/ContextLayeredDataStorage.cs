using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal class ContextLayeredDataStorage
    {
        public class Element
        {
            public string id;
            public List<Element> children;
            public Element parent;

            internal Element()
            {
                id = "";
                parent = null;
                children = new List<Element>();
            }

            internal Element(string id)
            {
                this.id = id;
                parent = null;
                children = new List<Element>();
            }

            //It is a path id if there are any '.' characters
            public static bool IsPath(string id)
            {
                return id.IndexOf('.') >= 0;
            }

            public void AddChild(Element child)
            {
                child.parent = this;
                children.Add(child);
            }

            public Element<T> AddData<T>(string id, T data)
            {
                //not a path? Add it directly as a child
                if (!IsPath(id))
                {
                    var output = new Element<T>(id, data);
                    AddChild(output);
                    return output;
                }
                else
                {
                    //is a path? see if it matches any children
                    foreach (Element child in children)
                    {
                        if (id.IndexOf(child.id) == 0)
                        {
                            return child.AddData(id.Substring(child.id.Length).TrimStart('.'), data);
                        }
                    }
                    //otherwise, add as child with full name
                    var output = new Element<T>(id, data);
                    AddChild(output);
                    return output;
                }
            }

            public Element AddData(string id)
            {
                //not a path? Add it directly as a child
                if (!IsPath(id))
                {
                    var output = new Element(id);
                    AddChild(output);
                    return output;
                }
                else
                {
                    //is a path? see if it matches any children
                    foreach (Element child in children)
                    {
                        if (id.IndexOf(child.id) == 0)
                        {
                            return child.AddData(id.Substring(child.id.Length).TrimStart('.'));
                        }
                    }
                    //otherwise, add as child with full name
                    var output = new Element(id);
                    AddChild(output);
                    return output;
                }
            }

            public void RemoveChild(Element child)
            {
                child.parent = null;
                children.Remove(child);
            }
            public void Remove()
            {
                foreach (var child in children)
                {
                    child.id = $"{id}.{child.id}";
                    if (parent != null)
                    {
                        parent.AddChild(child);
                    }
                }
                parent.RemoveChild(this);
            }

            public T GetData<T>()
            {
                var isDataHolder = this as Element<T>;
                if(isDataHolder != null)
                {
                    return isDataHolder.data;
                }
                else
                {
                    return default(T);
                }
            }
        }

        public class Element<T> : Element
        {
            public T data;

            internal Element(string id, T data) : base(id)
            {
                this.data = data;
            }
        }


        private class ReverseIntComparer : IComparer<int>
        {
            //reverse order since all our searching will check highest layer first
            public int Compare(int x, int y) => x.CompareTo(y) * -1;
        }

        private readonly SortedList<int, (string name, Element element)> m_layerList;
        public ContextLayeredDataStorage()
        {
            m_layerList = new SortedList<int, (string name, Element element)>(new ReverseIntComparer());
            m_layerList.Add(-1, ("Root", new Element()));
        }

        public void AddNewTopLayer(string layerName)
        {
            if (string.IsNullOrEmpty(layerName))
            {
                throw new System.ArgumentException("Cannor use a null or empty layer name", "layerName");
            }
            m_layerList.Add(m_layerList.Keys[0] + 1, (layerName, new Element()));
        }

        //AddData with no specified layer gets added to the topmost layer
        public Element<T> AddData<T>(string id, T data)
        {
            return AddData(m_layerList[m_layerList.Keys[0]].element, id, data);
        }

        public Element AddData(string id)
        {
            return AddData(m_layerList[m_layerList.Keys[0]].element, id);
        }

        public Element<T> AddData<T>(string layer, string id, T data)
        {
            foreach(var l in m_layerList)
            {
                if(string.CompareOrdinal(l.Value.name, layer) == 0)
                {
                    return AddData(l.Value.element, id, data);
                }
            }
            return null;
        }

        public Element AddData(string layer, string id)
        {
            foreach (var l in m_layerList)
            {
                if (string.CompareOrdinal(l.Value.name, layer) == 0)
                {
                    return AddData(l.Value.element, id);
                }
            }
            return null;
        }


        public Element<T> AddData<T>(Element elem, string id, T data)
        {
            return elem.AddData(id, data);
        }

        public Element AddData(Element elem, string id)
        {
            return elem.AddData(id);
        }

        public void RemoveData(string id)
        {
            if(string.IsNullOrEmpty(id))
            {
                throw new System.ArgumentException("Cannot remove a null or empty string id", "id");
            }
            Search(id)?.Remove();
        }

        public void RemoveData(Element elem)
        {
            if(elem.id.Length == 0 && elem.parent == null)
            {
                throw new System.ArgumentException("Cannot remove the root element of a layer", "elem");
            }
            elem.Remove();
        }

        public Element Search(string lookup)
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

        //may rewrite as non recursive
        private Element SearchRecurse(Element elem, string lookup)
        {
            if (string.CompareOrdinal(elem.id, lookup) == 0)
            {
                return elem;
            }

            if(lookup.IndexOf(elem.id) == 0 && elem.children.Count > 0)
            {
                Element output = null;
                string nextLookup = lookup.Substring(elem.id.Length).TrimStart('.');
                foreach(Element child in elem.children)
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
    }
}
