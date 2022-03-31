using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal interface IStandardNode
    {
        static Dictionary<string, RegistryKey> FunctionDescriptorNameToRegistryKey { get; set; }

        /// <summary>
        /// The FunctionDescriptor that describes the HLSL function of the node.
        /// </summary>
        static FunctionDescriptor FunctionDescriptor { get; }

        /// <summary>
        /// A selection of FunctionDescriptors for this node.
        /// Presence of an array of FunctionDescriptors means that the topology of
        /// the node is switchable.
        /// NOTE: Providing an array of FunctionDescritors is intended to overide
        /// an assignment into the isngle FunctionDescriptor property.
        /// </summary>
        static FunctionDescriptor[] FunctionDescriptors { get; }

        static Dictionary<string, string> UIStrings { get; }


        static Dictionary<string, float> UIHints { get; }
    }
}
