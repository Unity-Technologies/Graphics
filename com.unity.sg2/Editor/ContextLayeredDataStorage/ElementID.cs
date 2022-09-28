using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Profiling;

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
    public class ElementID : ISerializationCallbackReceiver
    {
        [SerializeField]
        private string[] m_path;
        [SerializeField]
        private char[][] m_charPath;
        [SerializeField]
        private char[] m_fullPath;
        private int m_hash;
        private int[] m_pathHash;
        private List<string> m_pathList;


        public string FullPath
        {
            get
            {
                if (m_fullPath == null)
                {
                    string temp = "";
                    for(int i = 0; i < m_charPath.Length; ++i)
                    {
                        for(int j = 0; j < m_charPath[i].Length; ++j)
                        {
                            temp += m_charPath[i][j];
                        }
                        if (i + 1 < m_charPath.Length)
                        {
                            temp += '.';
                        }
                    }
                    m_fullPath = temp.ToCharArray();
                }
                return new string(m_fullPath);
            }
        }

        private List<string> Path
        {
            get
            {
                if (m_charPath == null)
                {
                    int pathCount = 1;
                    foreach (char c in m_fullPath)
                    {
                        if (c == '.')
                        {
                            pathCount++;
                        }
                    }
                    m_charPath = new char[pathCount][];
                    int index = 0;
                    string temp = "";
                    foreach (char c in m_fullPath)
                    {
                        if (c == '.')
                        {
                            m_charPath[index] = temp.ToCharArray();
                            index++;
                            temp = "";
                        }
                        else
                        {
                            temp += c;
                        }
                    }

                    m_pathList = new List<string>(m_charPath.Length);
                    foreach (char[] subPath in m_charPath)
                    {
                        m_pathList.Add(new string(subPath));
                    }
                }
                return m_pathList;
            }
        }

        private int PathHash(int index)
        {
            if(m_pathHash == null)
            {
                if (m_charPath != null)
                {
                    m_pathHash = new int[m_charPath.Length];
                }
                else
                {
                    int pathCount = 1;
                    for (int i = 0; i < m_fullPath.Length; i++)
                    {
                        if (m_fullPath[i] == '.')
                        {
                            pathCount++;
                        }
                    }
                    m_pathHash = new int[pathCount];
                }
            }
            if(m_pathHash[index] == 0)
            {
                if(m_charPath != null)
                {
                    m_pathHash[index] = GetDeterministicStringHash(m_charPath[index], 0, m_charPath[index].Length);
                }
                else
                {
                    Profiler.BeginSample("fullPath case");
                    int i = 0;
                    int startIndex = 0;
                    int length = 0;
                    for (int j = 0; j < m_fullPath.Length; j++)
                    {
                        char c = m_fullPath[j];
                        if (c == '.')
                        {
                            i++;
                            if(i == index)
                            {
                                startIndex = j;
                            }
                            if(i == index+1)
                            {
                                break;
                            }
                        }
                        else
                        {
                            if(i == index)
                            {
                                length++;
                            }
                        }
                        
                    }
                    m_pathHash[index] = GetDeterministicStringHash(m_fullPath, startIndex, length);
                }
            }
            Profiler.EndSample();
            return m_pathHash[index];
        }

        private int PathLength
        {
            get
            {
                if (m_charPath != null)
                {
                    return m_charPath.Length;
                }
                else
                {
                    int pathCount = 1;
                    foreach (char c in m_fullPath)
                    {
                        if (c == '.')
                        {
                            pathCount++;
                        }
                    }
                    return pathCount;
                }
            }
        }

        private bool IsRoot
        {
            get
            {
                if(m_charPath != null)
                {
                    return m_charPath.Length == 1 && m_charPath[0].Length == 0;
                }
                else
                {
                    return m_fullPath == null || m_fullPath.Length == 0;
                }
            }
        }

        public string LocalPath => FullPath.Substring(FullPath.LastIndexOf('.') + 1);
        public string ParentPath => FullPath.Substring(0, Mathf.Max(FullPath.LastIndexOf('.'),0));

        public override int GetHashCode()
        {
            if(m_hash == 0)
            {
                m_hash = GetDeterministicStringHash(FullPath);
            }
            return m_hash;
        }

        private static int GetDeterministicStringHash(string str)
        {
            unchecked
            {
                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1)
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);

            }
        }

        private static int GetDeterministicStringHash(char[] str, int startIndex, int length)
        {
            unchecked
            {
                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;

                for (int i = startIndex; i < length; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == length - 1)
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);

            }
        }


        public ElementID(string id)
        {
            m_fullPath = id.ToCharArray();
            m_charPath = null;
            m_hash = 0;
            m_pathHash = null;
        }

        public ElementID(IEnumerable<string> path)
        {
            m_charPath = new char[path.Count()][];
            int i = 0;
            foreach (string p in path)
            {
                m_charPath[i] = p.ToCharArray();
                ++i;
            }
            m_fullPath = null;
            m_hash = 0;
            m_pathHash = null;
        }

        public ElementID(ElementID parent, IEnumerable<string> localPath)
        {
            m_charPath = new char[parent.Path.Count + localPath.Count()][];
            int i;
            for(i = 0; i < parent.m_charPath.Length; ++i)
            {
                m_charPath[i] = new char[parent.m_charPath[i].Length];
                parent.m_charPath[i].CopyTo(m_charPath[i], 0);
            }
            var temp = parent.FullPath;
            foreach (string p in localPath)
            {
                m_charPath[i] = p.ToCharArray();
                i++;
                temp += "." + p;
            }
            m_fullPath = temp.ToCharArray();
            m_hash = 0;
            m_pathHash = null;
        }

        public bool Equals(ElementID other)
        {
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
            if(IsRoot)
            {
                return true;
            }

            if (PathLength >= other.PathLength)
            {
                return false;
            }

            for (int i = 0; i < PathLength; ++i)
            {
                if (PathHash(i) != other.PathHash(i))
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsImmediateSubpathOf(ElementID other)
        {
            if(IsRoot)
            {
                if (other.PathLength > 1)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            return (PathLength + 1 == other.PathLength) && IsSubpathOf(other);
        }

        public static ElementID FromString(string path)
        {
            return new ElementID(path);
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
            string[] newPath = new string[PathLength];
            for (int i = 0; i < m_charPath.Length; i++)
            {
                if (Path[i].Equals(toRename))
                {
                    newPath[i] = newName;
                }
                else
                {
                    newPath[i] = Path[i];
                }
            }
            return newPath;
        }

        public void OnBeforeSerialize()
        {
            if (m_fullPath == null)
            {
                string temp = "";
                for (int i = 0; i < m_charPath.Length; ++i)
                {
                    for (int j = 0; j < m_charPath[i].Length; ++j)
                    {
                        temp += m_charPath[i][j];
                    }
                    if (i + 1 < m_charPath.Length)
                    {
                        temp += '.';
                    }
                }
                m_fullPath = temp.ToCharArray();
            }
        }

        public void OnAfterDeserialize()
        {
            if(m_path != null)
            {
                m_charPath = new char[m_path.Length][];
                for (int i = 0; i < m_path.Length; i++)
                {
                    m_charPath[i] = m_path[i].ToCharArray();
                }
            }
        }

        public static implicit operator ElementID(List<string> path) => new ElementID(path);
        public static implicit operator ElementID(string[] path) => new ElementID(path);
        public static implicit operator ElementID(string path) => FromString(path);
    }
}
