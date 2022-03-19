using System;

namespace UnityEditor.ContextLayeredDataStorage
{
    public class DataWriter
    {
        private WeakReference<Element> m_element;
        protected Element Element
        {
            get
            {
                if (m_element.TryGetTarget(out Element element))
                {
                    return element;
                }
                else
                {
                    throw new Exception();
                }
            }
        }

        public DataWriter(Element element)
        {
            m_element = new WeakReference<Element>(element);
        }

        public virtual DataWriter AddChild(string localID)
        {
            Element e = Element;
            e.owner.AddData(e, ElementID.FromString(e.ID.FullPath + $".{localID}"), out Element element);
            return new DataWriter(element);
        }

        public virtual DataWriter AddChild<T>(string localID, T data)
        {
            Element e = Element;
            e.owner.AddData(e, ElementID.FromString(e.ID.FullPath + $".{localID}"), data, out Element<T> element);
            return new DataWriter(element);
        }

        public virtual void RemoveChild(string localID)
        {
            Element e = Element;
            e.owner.RemoveData(e, ElementID.FromString(e.ID.FullPath + $".{localID}"));
        }
        public virtual void SetData<T>(T value)
        {
            (Element as Element<T>).m_Data = value;
        }

        public virtual void SetHeader(DataHeader newHeader)
        {
            Element.Header = newHeader;
        }
    }
}
