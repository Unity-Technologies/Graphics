namespace UnityEditor.VFX
{
    /// <summary>
    /// The granularity the VFX runtime dispatches with, which anything a dispatch indexes has to be sized by.
    /// </summary>
    static class VfxDispatch
    {
        /// <summary>
        /// Must match VFXManager::kDefaultThreadCountPerGroup.
        /// </summary>
        public const uint ThreadsPerGroup = 64u;

        /// <summary>
        /// Rounds a thread count up to a whole number of groups. A dispatch pads each instance's slice this
        /// way (InstanceSplitData::AddInstance), so the threads in the padding still index the data the
        /// instance was given, past the count it actually holds.
        /// </summary>
        public static uint AlignToThreadGroups(uint threadCount)
        {
            uint groupCount = (threadCount + ThreadsPerGroup - 1u) / ThreadsPerGroup;
            return (groupCount != 0u ? groupCount : 1u) * ThreadsPerGroup;
        }
    }
}
