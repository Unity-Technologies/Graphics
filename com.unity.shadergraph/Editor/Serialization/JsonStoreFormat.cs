using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Serialization
{
    struct RawJsonStoreItem : IEquatable<RawJsonStoreItem>
    {
        public string typeFullName;
        public string id;
        public string json;

        public bool Equals(RawJsonStoreItem other)
        {
            return typeFullName == other.typeFullName && id == other.id && json == other.json;
        }

        public override bool Equals(object obj)
        {
            return obj is RawJsonStoreItem other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (typeFullName != null ? typeFullName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (id != null ? id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (json != null ? json.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    static class JsonStoreFormat
    {
        public static List<RawJsonStoreItem> Parse(string str)
        {
            var result = new List<RawJsonStoreItem>();
            var beginStr = $"{Environment.NewLine}--- ";
            var startIndex = 0;
            if (!str.StartsWith("--- "))
            {
                throw new InvalidOperationException("Expected '--- '");
            }
            while (startIndex < str.Length)
            {
                var jsonIndex = str.IndexOf(Environment.NewLine, startIndex, StringComparison.Ordinal) + 1;
                var typeIndex = str.IndexOf(' ', startIndex, jsonIndex - startIndex) + 1;
                var idIndex = str.IndexOf(' ', typeIndex, jsonIndex - typeIndex) + 1;
                var nextIndex = str.IndexOf(beginStr, StringComparison.Ordinal) + 1;
                if (nextIndex == -1)
                {
                    nextIndex = str.Length;
                }

                result.Add(new RawJsonStoreItem
                {
                    typeFullName = str.Substring(typeIndex, idIndex - typeIndex),
                    id = str.Substring(idIndex, jsonIndex - idIndex),
                    json = str.Substring(jsonIndex, nextIndex - jsonIndex)
                });

                startIndex = nextIndex;
            }

            return result;
        }
    }
}
