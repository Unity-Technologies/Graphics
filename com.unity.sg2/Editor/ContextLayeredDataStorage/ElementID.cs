using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEditor.ContextLayeredDataStorage
{
    [Serializable]
    public struct ElementID
    {
        [SerializeField]
        private List<string> m_path;
        [field: SerializeField]
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

        public static ElementID CreateUniqueLocalID(ElementID parentID, IEnumerable<string> existingLocalChildIDs, string desiredLocalID)
        {
            string uniqueName = SanitizeName(existingLocalChildIDs, "{0}_{1}", desiredLocalID, "\".");
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



        public static implicit operator ElementID(List<string> path) => new ElementID(path);
        public static implicit operator ElementID(string[] path) => new ElementID(path);
        public static implicit operator ElementID(string path) => FromString(path);
    }
}
