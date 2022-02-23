namespace UnityEngine.Rendering
{
    [System.Serializable]
    internal struct ProbeDilationSettings
    {
        public bool enableDilation;
        public float dilationDistance;
        public float dilationValidityThreshold;
        public int dilationIterations;
        public bool squaredDistWeighting;

        internal void SetDefaults()
        {
            enableDilation = true;
            dilationDistance = 1;
            dilationValidityThreshold = 0.25f;
            dilationIterations = 1;
            squaredDistWeighting = true;
        }

        internal void UpgradeFromTo(ProbeVolumeBakingProcessSettings.SettingsVersion from, ProbeVolumeBakingProcessSettings.SettingsVersion to) { }
    }

    [System.Serializable]
    internal struct VirtualOffsetSettings
    {
        public bool useVirtualOffset;
        [Range(0f, 1f)] public float outOfGeoOffset;
        [Range(0f, 2f)] public float searchMultiplier;
        [Range(-0.05f, 0f)] public float rayOriginBias;
        [Range(4, 24)] public int maxHitsPerRay;
        public LayerMask collisionMask;

        internal void SetDefaults()
        {
            useVirtualOffset = true;
            outOfGeoOffset = 0.01f;
            searchMultiplier = 0.2f;
            UpgradeFromTo(ProbeVolumeBakingProcessSettings.SettingsVersion.Initial, ProbeVolumeBakingProcessSettings.SettingsVersion.ThreadedVirtualOffset);
        }

        internal void UpgradeFromTo(ProbeVolumeBakingProcessSettings.SettingsVersion from, ProbeVolumeBakingProcessSettings.SettingsVersion to)
        {
            if (from < ProbeVolumeBakingProcessSettings.SettingsVersion.ThreadedVirtualOffset && to >= ProbeVolumeBakingProcessSettings.SettingsVersion.ThreadedVirtualOffset)
            {
                rayOriginBias = -0.001f;
                maxHitsPerRay = 10;
                collisionMask = Physics.DefaultRaycastLayers;
            }
        }
    }

    // TODO: Use this structure in the actual authoring component rather than just a mean to group output parameters.
    [System.Serializable]
    internal struct ProbeVolumeBakingProcessSettings
    {
        internal static ProbeVolumeBakingProcessSettings Default { get { var s = new ProbeVolumeBakingProcessSettings(); s.SetDefaults(); return s; } }

        internal enum SettingsVersion
        {
            Initial,
            ThreadedVirtualOffset,

            Max,
            Current = Max - 1
        }

        internal ProbeVolumeBakingProcessSettings(ProbeDilationSettings dilationSettings, VirtualOffsetSettings virtualOffsetSettings)
        {
            m_Version = SettingsVersion.Current;
            this.dilationSettings = dilationSettings;
            this.virtualOffsetSettings = virtualOffsetSettings;
        }

        internal void SetDefaults()
        {
            m_Version = SettingsVersion.Current;
            dilationSettings.SetDefaults();
            virtualOffsetSettings.SetDefaults();
        }

        internal void Upgrade()
        {
            if (m_Version != SettingsVersion.Current)
            {
                // Debug.Log($"Upgrading probe volume baking process settings from '{m_Version}' to '{SettingsVersion.Current}'.");

                dilationSettings.UpgradeFromTo(m_Version, SettingsVersion.Current);
                virtualOffsetSettings.UpgradeFromTo(m_Version, SettingsVersion.Current);
                m_Version = SettingsVersion.Current;
            }
        }

        [SerializeField] SettingsVersion m_Version;

        public ProbeDilationSettings dilationSettings;
        public VirtualOffsetSettings virtualOffsetSettings;
    }
}
