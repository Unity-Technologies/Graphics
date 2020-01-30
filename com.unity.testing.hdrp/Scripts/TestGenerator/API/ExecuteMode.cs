using System;

namespace UnityEngine.Experimental.Rendering.HDPipelineTest.TestGenerator
{
    // Prototype
    /// <summary>
    ///     Describe when an execution should occur
    /// </summary>
    [Flags]
    public enum ExecuteMode
    {
        None = 0,
        PlayMode = 1 << 0,
        EditMode = 1 << 1,

        All = -1
    }
}
