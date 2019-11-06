using System;

namespace UnityEditor.ShaderGraph.Serialization
{
    [Serializable]
    struct RawJsonObject : IEquatable<RawJsonObject>
    {
        public string typeFullName;
        public string id;
        public string json;
        public int changeGeneration;
        public int changeVersion;

        public bool Equals(RawJsonObject other)
        {
            return typeFullName == other.typeFullName && id == other.id && json == other.json && changeGeneration == other.changeGeneration && changeVersion == other.changeVersion;
        }

        public override bool Equals(object obj)
        {
            return obj is RawJsonObject other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (typeFullName != null ? typeFullName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (id != null ? id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (json != null ? json.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ changeGeneration;
                hashCode = (hashCode * 397) ^ changeVersion;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{nameof(typeFullName)}: '{typeFullName}', {nameof(id)}: '{id}', {nameof(json)}: '{json}'";
        }
    }
}
