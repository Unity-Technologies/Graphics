using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    internal interface IBlockFieldProviderInfo
    {
        string uniqueNamespace { get; }
    }

    internal interface IBlockFieldProvider : IBlockFieldProviderInfo
    {
        IEnumerable<(BlockFieldSignature blockFieldSignature, BlockFieldDescriptor blockFieldDescriptor)> recognizedBlockFieldSignatures { get; }
        // Maybe something like this would be useful:
        //int version { get; }
        //BlockFieldDescriptor UpgradePreviousVersionBlockFieldUser(int previousVersion, BlockFieldSignature previousSignature);
        //
        // Version would also need to be a field in the descriptor.
        // For now, not really needed as blockfield descriptors, while specifying a type of control, should not contain serialized
        // state beside an identifying signature to express the final subtarget input port.
        // Any default value of the control is part of the port definition, so the matching table above (recognizedBlockFieldSignatures)
        // should automatically upgrade any user of this port.
        // If the type of port changes though, or it disappears it would be nice if the subtarget could play with the edge
        // connection to splice something to adapt a previous connection maybe.
    }
}
