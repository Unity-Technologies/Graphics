using System;
using System.Collections.Generic;

namespace UnityEditor.ContextLayeredDataStorage
{
    public interface IDataReader
    {
        public IEnumerable<IDataReader> GetChildren();
        public IDataReader GetChild(string localID);
        public IDataReader GetParent();
        public T GetData<T>();
    }

    internal class ElementReader : IDataReader
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

        public ElementReader(Element element)
        {
            m_elem = new WeakReference<Element>(element);
        }

        public T GetData<T>()
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

        public IEnumerable<IDataReader> GetChildren()
        {
            throw new Exception();
            //Element e = Element;
            //foreach (var (key, value) in e.owner.m_flatStructureLookup)
            //{
            //    if (e.ID.IsSubpathOf(key))
            //    {
            //        yield return new ElementReader(value);
            //    }
            //}
        }

        public IDataReader GetChild(string localID)
        {
            Element e = Element;
            return e.owner.Search(e.ID.FullPath + $".{localID}");
        }

        public IDataReader GetParent()
        {
            Element e = Element;
            return e.owner.Search(e.ID.FullPath.Replace($".{Element.ID.LocalPath}", ""));
        }
    }


}
