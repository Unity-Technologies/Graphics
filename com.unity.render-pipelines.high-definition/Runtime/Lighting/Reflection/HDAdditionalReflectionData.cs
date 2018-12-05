namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [RequireComponent(typeof(ReflectionProbe))]
    public sealed partial class HDAdditionalReflectionData : HDProbe
    {
        void Awake()
        {
            type = ProbeSettings.ProbeType.ReflectionProbe;
            k_ReflectionProbeMigration.Migrate(this);
        }
    }
}
