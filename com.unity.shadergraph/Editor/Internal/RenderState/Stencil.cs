namespace UnityEditor.ShaderGraph.Internal
{
    public struct Stencil
    {
        public string WriteMask;
        public string Ref;
        public string Comp;
        public string Pass;

        public string ToString()
        {
            ShaderStringBuilder builder = new ShaderStringBuilder();
            builder.AppendLine("Stencil");
            using (builder.BlockScope())
            {
                builder.AppendLine($"WriteMask {WriteMask}");
                builder.AppendLine($"Ref {Ref}");
                builder.AppendLine($"Comp {Comp}");
                builder.AppendLine($"Pass {Pass}");
            }
            return builder.ToCodeBlack();
        }
    }
}
