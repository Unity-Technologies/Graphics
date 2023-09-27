using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.VFX.VFXSortingUtility;

namespace UnityEditor.VFX
{
    enum VFXTaskShaderType
    {
        ComputeShader,
        Shader,
    }

    class VFXTask
    {
        public struct BufferMapping
        {
            public string bufferName;
            public string mappingName;
            public bool useBufferCountIndexInName;

            public BufferMapping(string bufferName, string mappingName)
            {
                this.bufferName = bufferName;
                this.mappingName = mappingName;
                useBufferCountIndexInName = false;
            }

            public static implicit operator BufferMapping(string bufferName)
                => new BufferMapping { bufferName = bufferName, mappingName = bufferName};
        }

        public bool doesGenerateShader;
        public string templatePath;
        public VFXTaskShaderType shaderType;
        public VFXTaskType type;
        public string[] additionalDefines;
        public List<BufferMapping> bufferMappings = new();
        public string name; // optional name (null valid)

        public bool needsIndirectBuffer => bufferMappings.Any(m => m.bufferName.StartsWith(VFXDataParticle.k_IndirectBufferName));
    }
}
