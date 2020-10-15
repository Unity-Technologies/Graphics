namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDCameraExtension : IVersionable<HDCameraExtension.Version>
    {
        enum Version
        {
            Initial
        }

        [SerializeField] Version m_Version = MigrationDescription.LastVersion<Version>();

        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }
    }
}
