using System;

namespace UnityEditor.ContextLayeredDataStorage
{
    public interface IDataWriter
    {
        public IDataWriter AddChild(string localID);
        public IDataWriter AddChild<T>(string localID, T data);
        public void RemoveChild(string localID);
        public void SetData<T>(T value);
    }

    internal class ElementWriter : IDataWriter
    {
        private WeakReference<Element> m_elem;
        public Element Element
        {
            get
            {
                if (m_elem.TryGetTarget(out Element element))
                {
                    return element;
                }
                else
                {
                    throw new Exception();
                }
            }
        }

        public ElementWriter(Element element)
        {
            m_elem = new WeakReference<Element>(element);
        }

        public IDataWriter AddChild(string localID)
        {
            Element e = Element;
            e.owner.AddData(ElementID.FromString(e.ID.FullPath + $".{localID}"), out Element element);
            return new ElementWriter(element);
        }

        public IDataWriter AddChild<T>(string localID, T data)
        {
            Element e = Element;
            e.owner.AddData(ElementID.FromString(e.ID.FullPath + $".{localID}"), data, out Element<T> element);
            return new ElementWriter(element);
        }

        public void RemoveChild(ElementID childID)
        {
            Element.owner.RemoveData(childID);
        }

        public void RemoveChild(string localID)
        {
            Element e = Element;
            e.owner.RemoveData(ElementID.FromString(e.ID.FullPath + $".{localID}"));
        }

        public void SetData<T>(T value)
        {
            //(Element as Element<T>).Data = value;
        }
    }


}
