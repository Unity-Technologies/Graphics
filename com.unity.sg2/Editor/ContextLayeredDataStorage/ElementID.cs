using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEditor.ContextLayeredDataStorage
{

    public class ElementIDComparer : IEqualityComparer<ElementID>
    {
        public bool Equals(ElementID x, ElementID y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(ElementID obj)
        {
            return obj.GetHashCode();
        }
    }

    [Serializable]
    public struct ElementID
    {
        [SerializeField]
        private string[] m_path;
        [field: SerializeField]
        public string FullPath { get; private set; }
        public string LocalPath => m_path.Length >= 1 ? m_path[m_path.Length - 1] : "";
        public string ParentPath => FullPath.Substring(0, Mathf.Max(FullPath.LastIndexOf('.'),0));

        public override int GetHashCode()
        {
            return FullPath.GetHashCode(StringComparison.Ordinal);
        }

        public ElementID(string id)
        {
            m_path = new string[] { id };
            FullPath = id;
        }

        public ElementID(IEnumerable<string> path)
        {
            m_path = new string[path.Count()];
            FullPath = "";
            int i = 0;
            foreach (string p in path)
            {
                m_path[i] = p;

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
            m_path = new string[parent.m_path.Length + localPath.Count()];
            int i;
            for(i = 0; i < parent.m_path.Length; ++i)
            {
                m_path[i] = parent.m_path[i];
            }
            FullPath = parent.FullPath;
            foreach (string p in localPath)
            {
                m_path[i] = p;
                i++;
                FullPath += "." + p;
            }
        }

        public bool Equals(ElementID other)
        {
            if (m_path.Length != other.m_path.Length)
            {
                return false;
            }

            return other.GetHashCode() == GetHashCode();
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
            //special case for root, empty string is always a subpath
            if(m_path.Length == 1 && m_path[0].Length == 0)
            {
                return true;
            }

            if (m_path.Length >= other.m_path.Length)
            {
                return false;
            }

            for (int i = 0; i < m_path.Length; ++i)
            {
                if (string.CompareOrdinal(m_path[i],other.m_path[i]) != 0)
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsImmediateSubpathOf(ElementID other)
        {
            if(m_path.Length == 1 && m_path[0].Length == 0)
            {
                if (other.m_path.Length > 1)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            return (m_path.Length + 1 == other.m_path.Length) && IsSubpathOf(other);
        }

        public static ElementID FromString(string path)
        {
            return new ElementID(path.Split('.'));
        }

        public static ElementID CreateUniqueLocalID(ElementID parentID, IEnumerable<string> existingLocalChildIDs, string desiredLocalID)
        {
            string uniqueName = SanitizeName(existingLocalChildIDs, "{0}_{1}", desiredLocalID, "\"");
            return $"{parentID.FullPath}{(parentID.FullPath.Length > 0 ? "." : "")}{uniqueName}";
        }

        private static string SanitizeName(IEnumerable<string> existingNames, string duplicateFormat, string name, string disallowedPatternRegex = "\"")
        {
            name = Regex.Replace(name, disallowedPatternRegex, "_");
            return DeduplicateName(existingNames, duplicateFormat, name);
        }

        private static string DeduplicateName(IEnumerable<string> existingNames, string duplicateFormat, string name)
        {
            if (!existingNames.Contains(name))
                return name;

            string escapedDuplicateFormat = Regex.Escape(duplicateFormat);

            // Escaped format will escape string interpolation, so the escape characters must be removed for these.
            escapedDuplicateFormat = escapedDuplicateFormat.Replace(@"\{0}", @"{0}");
            escapedDuplicateFormat = escapedDuplicateFormat.Replace(@"\{1}", @"{1}");

            var baseRegex = new Regex(string.Format(escapedDuplicateFormat, @"^(.*)", @"(\d+)"));

            var baseMatch = baseRegex.Match(name);
            if (baseMatch.Success)
                name = baseMatch.Groups[1].Value;

            string baseNameExpression = string.Format(@"^{0}", Regex.Escape(name));
            var regex = new Regex(string.Format(escapedDuplicateFormat, baseNameExpression, @"(\d+)") + "$");

            var existingDuplicateNumbers = existingNames.Select(existingName => regex.Match(existingName)).Where(m => m.Success).Select(m => int.Parse(m.Groups[1].Value)).Where(n => n > 0).Distinct().ToList();

            var duplicateNumber = 1;
            existingDuplicateNumbers.Sort();
            if (existingDuplicateNumbers.Any() && existingDuplicateNumbers.First() == 1)
            {
                duplicateNumber = existingDuplicateNumbers.Last() + 1;
                for (var i = 1; i < existingDuplicateNumbers.Count; i++)
                {
                    if (existingDuplicateNumbers[i - 1] != existingDuplicateNumbers[i] - 1)
                    {
                        duplicateNumber = existingDuplicateNumbers[i - 1] + 1;
                        break;
                    }
                }
            }

            return string.Format(duplicateFormat, name, duplicateNumber);
        }

        public ElementID Rename(string toRename, string newName)
        {
            string[] newPath = new string[m_path.Length];
            for (int i = 0; i < m_path.Length; i++)
            {
                if (m_path[i].Equals(toRename))
                {
                    newPath[i] = newName;
                }
                else
                {
                    newPath[i] = m_path[i];
                }
            }
            return newPath;
        }

        public static implicit operator ElementID(List<string> path) => new ElementID(path);
        public static implicit operator ElementID(string[] path) => new ElementID(path);
        public static implicit operator ElementID(string path) => FromString(path);
    }
}
