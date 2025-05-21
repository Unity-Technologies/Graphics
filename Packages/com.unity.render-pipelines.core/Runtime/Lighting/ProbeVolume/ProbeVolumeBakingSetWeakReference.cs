namespace UnityEngine.Rendering
{
    // Q: What is this?
    // A: A utility class for storing a reference to a ProbeVolumeBakingSet, without forcing the object to be loaded in-memory.

    // Q: Why do we need this?
    // A: APV uses a lot of global state, and we aren't always good with cleaning it up. Unity's serialization layer will automatically
    // load referenced assets into memory, recursively. Some of the assets referenced by ProbeVolumeBakingSet can get very large, on
    // the order of several gigabytes. A lingering global reference to a ProbeVolumeBakingSet can keep these assets in memory indefinitely,
    // preventing them from being garbage collected. By using a weak reference, we prevent this - ProbeVolumeBakingSet can be freely
    // garbage collected. Whenever we actually need to access it, we load it on-demand, if it isn't already in memory.

    internal class ProbeVolumeBakingSetWeakReference
    {
        public int m_InstanceID;

        public ProbeVolumeBakingSetWeakReference(ProbeVolumeBakingSet bakingSet)
        {
            Set(bakingSet);
        }

        public ProbeVolumeBakingSetWeakReference()
        {
            m_InstanceID = 0;
        }

        // Change which baking set the references points to.
        public void Set(ProbeVolumeBakingSet bakingSet)
        {
            if (bakingSet == null)
                m_InstanceID = 0;
            else
                m_InstanceID = bakingSet.GetInstanceID();
        }

        // Get the referenced baking set, loading it into memory if necessary.
        public ProbeVolumeBakingSet Get()
        {
            return Resources.InstanceIDToObject(m_InstanceID) as ProbeVolumeBakingSet;
        }

        // Is the referenced baking set in memory?
        public bool IsLoaded()
        {
            return Resources.InstanceIDIsValid(m_InstanceID);
        }

        // Force the referenced baking set to be unloaded from memory.
        // Calling Get() after Unload() will re-load the baking set into memory.
        public void Unload()
        {
            if (!IsLoaded())
                return;

            var bakingSet = Get();

            // These assets would get garbage collected, but we clean them up immediately to free memory earlier.
            // Only do this in editor, where the assets are never needed, so we don't unload assets that may still be in use.
            #if UNITY_EDITOR
            Resources.UnloadAsset(bakingSet.cellBricksDataAsset?.asset);
            Resources.UnloadAsset(bakingSet.cellSharedDataAsset?.asset);
            Resources.UnloadAsset(bakingSet.cellSupportDataAsset?.asset);
            foreach (var scenario in bakingSet.scenarios)
            {
                Resources.UnloadAsset(scenario.Value.cellDataAsset?.asset);
                Resources.UnloadAsset(scenario.Value.cellOptionalDataAsset?.asset);
                Resources.UnloadAsset(scenario.Value.cellProbeOcclusionDataAsset?.asset);
            }
            #endif

            // Unload the baking set itself.
            Resources.UnloadAsset(bakingSet);
        }
    }
}
