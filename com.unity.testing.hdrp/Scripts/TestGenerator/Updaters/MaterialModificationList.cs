using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipelineTest.TestGenerator
{
    // Prototype
    /// <summary>
    ///     A list of modification to apply to a material.
    /// </summary>
    [Serializable]
    public struct MaterialModificationList
    {
        // Note: this should probably be private with better api
        public List<MaterialModification> modifications;
    }
}
