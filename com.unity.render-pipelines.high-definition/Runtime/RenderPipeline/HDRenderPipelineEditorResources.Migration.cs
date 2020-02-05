#if UNITY_EDITOR //file must be in realtime assembly folder to be found in HDRPAsset

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipelineEditorResources
    {
        enum Version
        {
            None
        }

        [HideInInspector, SerializeField]
        Version m_Version = MigrationDescription.LastVersion<Version>();

        //Note: nothing to migrate at the moment.
        // If any, it must be done at deserialisation time on this component due to lazy init and disk access conflict when rebuilding library folder
    }
}
#endif
