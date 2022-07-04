using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal struct KernelDescriptor
    {
        public string name;

        // Templates
        public string templatePath;
        public string[] sharedTemplateDirectories;

        // Kernels are defined by a reference pass descriptor, to re-use a lot of systems for generating the graph code.
        public PassDescriptor passDescriptorReference;
    }
}
