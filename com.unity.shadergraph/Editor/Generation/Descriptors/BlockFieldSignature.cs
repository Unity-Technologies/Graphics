namespace UnityEditor.ShaderGraph
{
    // This encapsulates the serialized link between a BlockNode (which is serialized) to a BlockFieldDescriptor.
    // (the later isn't and can be declared by different pipelines and/or materials.)
    struct BlockFieldSignature
    {
        public string providerNamespace { get; }
        public string tag { get; }
        public string referenceName { get; }

        public BlockFieldSignature(string providerNamespace, string tag, string referenceName)
        {
            this.providerNamespace = providerNamespace;
            this.tag = tag;
            this.referenceName = referenceName;
        }

        public bool Equals(BlockFieldSignature other)
        {
            return providerNamespace == other.providerNamespace && tag == other.tag && referenceName == other.referenceName;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (providerNamespace != null ? providerNamespace.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (tag != null ? tag.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (referenceName != null ? referenceName.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            // The first form is to support upgrade from old weakly serialized block field descriptor strings in block nodes
            return (string.IsNullOrEmpty(providerNamespace)) ? $"{tag}.{referenceName}" : $"{providerNamespace}.{tag}.{referenceName}";
        }
    }
}
