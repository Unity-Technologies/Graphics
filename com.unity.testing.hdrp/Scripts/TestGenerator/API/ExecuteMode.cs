using System;

namespace UnityEngine.Experimental.Rendering.HDPipelineTest.TestGenerator
{
    // Prototype
    [Flags]
    public enum ExecuteMode
    {
        None = 0,
        PlayMode = 1 << 0,
        EditMode = 1 << 1,

        All = -1
    }
}
