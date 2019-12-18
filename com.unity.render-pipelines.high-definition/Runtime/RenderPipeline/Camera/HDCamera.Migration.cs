namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDCamera : IVersionable<HDCamera.Version>
    {
        enum Version
        {
            None,
            MergeHDAdditionalCameraDataIntoHDCamera, //see HDAdditionalCameraData for migration step prior merge
        }

        [SerializeField]
        Version m_Version;

        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }

        static readonly MigrationDescription<Version, HDCamera> k_Migration = MigrationDescription.New<Version, HDCamera>(
            //Add migration step here;
        );
        
        void Awake() => k_Migration.Migrate(this);
    }
}
