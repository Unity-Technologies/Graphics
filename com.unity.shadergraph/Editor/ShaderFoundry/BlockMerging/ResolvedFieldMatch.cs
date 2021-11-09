using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    internal class ResolvedFieldMatch
    {
        internal BlockVariableLinkInstance Source;
        internal BlockVariableLinkInstance Destination;
        internal int SourceSwizzle = 0;
        internal int DestinationSwizzle = 0;
    }
}
