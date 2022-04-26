using System;
using System.Collections.Generic;

namespace UnityEditor.ContextLayeredDataStorage
{
    public class DataReader
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

        public ContextLayeredDataStorage Owner => Element.owner;

        public DataReader(Element element)
        {
            m_elem = new WeakReference<Element>(element);
        }

        public virtual T GetData<T>()
        {
            if (Element is Element<T> typedElement)
            {
                return typedElement.Data;
            }
            else
            {
                throw new InvalidCastException();
            }
        }

        public virtual IEnumerable<DataReader> GetChildren()
        {
            Element e = Element;
            foreach (var (key, value) in e.owner.FlatStructureLookup)
            {
                if (e.ID.IsImmediateSubpathOf(key))
                {
                    yield return new DataReader(value);
                }
            }
        }

        public virtual DataReader GetChild(string localID)
        {
            Element e = Element;
            return e.owner.Search(e.ID.FullPath + $".{localID}");
        }

        public virtual DataReader GetParent()
        {
            Element e = Element;
            return e.owner.Search(e.ID.FullPath.Replace($".{Element.ID.LocalPath}", ""));
        }
    }


}
