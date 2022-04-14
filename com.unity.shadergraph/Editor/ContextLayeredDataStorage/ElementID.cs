using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.ContextLayeredDataStorage
{
    [Serializable]
    public struct ElementID
    {
        [SerializeField]
        private List<string> m_path;
        [SerializeField]
        public string FullPath { get; private set; }
        public string LocalPath => m_path.Count >= 1 ? m_path[m_path.Count - 1] : "";
        public string ParentPath => FullPath.Substring(0, Mathf.Max(FullPath.LastIndexOf('.'),0));

        public ElementID(string id)
        {
            m_path = new List<string>() { id };
            FullPath = id;
        }

        public ElementID(IEnumerable<string> path)
        {
            m_path = new List<string>();
            FullPath = "";
            int i = 0;
            foreach (string p in path)
            {
                m_path.Add(p);

                if (i == 0)
                {
                    FullPath = p;
                }
                else
                {
                    FullPath += "." + p;
                }
                ++i;
            }
        }

        public ElementID(ElementID parent, IEnumerable<string> localPath)
        {
            m_path = new List<string>();
            if (parent.m_path != null)
            {
                m_path.AddRange(parent.m_path);
            }
            FullPath = parent.FullPath;
            foreach (string p in localPath)
            {
                m_path.Add(p);
                FullPath += "." + p;
            }
        }

        public bool Equals(ElementID other)
        {
            if (m_path.Count != other.m_path.Count)
            {
                return false;
            }

            for (int i = 0; i < m_path.Count; ++i)
            {
                if (m_path[i].CompareTo(other.m_path[i]) != 0)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns true if ElementID other contains ElementID this as a root path
        /// ie. "Foo.Bar" is a subpath of "Foo.Bar.Baz"
        /// "Foo.Bar" is not "Baz.Foo.Bar"
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsSubpathOf(ElementID other)
        {
            if (m_path.Count >= other.m_path.Count)
            {
                return false;
            }

            for (int i = 0; i < m_path.Count; ++i)
            {
                if (m_path[i].CompareTo(other.m_path[i]) != 0)
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsImmediateSubpathOf(ElementID other)
        {
            return m_path.Count + 1 == other.m_path.Count && IsSubpathOf(other);
        }

        public static ElementID FromString(string path)
        {
            return new ElementID(path.Split('.'));
        }

        public static implicit operator ElementID(List<string> path) => new ElementID(path);
        public static implicit operator ElementID(string[] path) => new ElementID(path);
        public static implicit operator ElementID(string path) => FromString(path);
    }
}
