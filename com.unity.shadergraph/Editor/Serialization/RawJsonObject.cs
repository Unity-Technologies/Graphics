using System;

namespace UnityEditor.ShaderGraph.Serialization
{
    [Serializable]
    struct RawJsonObject : IEquatable<RawJsonObject>
    {
        public string type;
        public string id;
        public string json;
        public int changeVersion;

        public bool Equals(RawJsonObject other)
        {
            return type == other.type && id == other.id && json == other.json && changeVersion == other.changeVersion;
        }

        public override bool Equals(object obj)
        {
            return obj is RawJsonObject other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (type != null ? type.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (id != null ? id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (json != null ? json.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ changeVersion;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{nameof(type)}: '{type}', {nameof(id)}: '{id}', {nameof(json)}: '{json}'";
        }
    }
}
