
namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class WaterData : HDTargetData
    {
    }

    class WaterDecalData : HDTargetData
    {
        public bool affectsDeformation = true;
        public bool affectsFoam = true;
        public bool affectsSimulationMask = true;
        public bool affectsLargeCurrent = true;
        public bool affectsRipplesCurrent = true;
    }
}
