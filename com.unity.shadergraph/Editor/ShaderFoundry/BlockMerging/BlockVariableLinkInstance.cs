using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderFoundry
{
    /// Represents a mutable variable instance within blocks. A variable might have an owner (a variable in a sub-class instance).
    [DebuggerDisplay("{Type.Name} {ReferenceName}")]
    internal class BlockVariableLinkInstance
    {
        // The type that owns this instance.
        internal BlockTypeLinkInstance TypeLinkInstance;
        // If this is a field in another type, then this is the owning instance.
        internal BlockVariableLinkInstance Owner;
        internal ShaderType Type;
        internal string ReferenceName;
        internal string DisplayName;
        internal string DefaultExpression;
        internal List<ShaderAttribute> Attributes = new List<ShaderAttribute>();

        internal static BlockVariableLinkInstance Construct(BlockVariable variable, BlockVariableLinkInstance owner, BlockTypeLinkInstance typeLinkInstance, IEnumerable<ShaderAttribute> attributes = null)
        {
            return Construct(variable.Type, variable.ReferenceName, variable.DisplayName, owner, typeLinkInstance, attributes);
        }

        internal static BlockVariableLinkInstance Construct(ShaderType type, string referenceName, string displayName, BlockVariableLinkInstance owner, BlockTypeLinkInstance typeLinkInstance, IEnumerable<ShaderAttribute> attributes = null)
        {
            var result = new BlockVariableLinkInstance
            {
                Type = type,
                ReferenceName = referenceName,
                DisplayName = displayName,
                Owner = owner,
                TypeLinkInstance = typeLinkInstance
            };
            if (attributes != null)
            {
                foreach (var attribute in attributes)
                    result.Attributes.Add(attribute);
            }
            return result;
        }

        internal BlockVariable Build(ShaderContainer container)
        {
            var blockVariableBuilder = new BlockVariable.Builder();
            blockVariableBuilder.Type = Type;
            blockVariableBuilder.ReferenceName = ReferenceName;
            blockVariableBuilder.DisplayName = DisplayName;
            blockVariableBuilder.DefaultExpression = DefaultExpression;
            foreach (var attribute in Attributes)
                blockVariableBuilder.AddAttribute(attribute);
            return blockVariableBuilder.Build(container);
        }
    }
}
