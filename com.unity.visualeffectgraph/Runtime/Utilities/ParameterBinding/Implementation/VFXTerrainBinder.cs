#if VFX_HAS_TERRAINMODULE
using UnityEngine.Experimental.VFX;

namespace UnityEngine.Experimental.VFX.Utility
{
    [AddComponentMenu("VFX/Utilities/Parameters/VFX Terrain Binder")]
    [VFXBinder("Utility/Terrain")]
    public class VFXTerrainBinder : VFXBinderBase
    {
        [VFXParameterBinding("UnityEditor.VFX.TerrainType")]
        public ExposedParameter TerrainParameter;
        public Terrain Terrain;

        private ExposedParameter Terrain_Bounds_center;
        private ExposedParameter Terrain_Bounds_size;
        private ExposedParameter Terrain_HeightMap;
        private ExposedParameter Terrain_Height;

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateSubParameters();
        }

        private void OnValidate()
        {
            UpdateSubParameters();
        }

        void UpdateSubParameters()
        {
            Terrain_Bounds_center = TerrainParameter + "_Bounds_center";
            Terrain_Bounds_size = TerrainParameter + "_Bounds_size";
            Terrain_HeightMap = TerrainParameter + "_HeightMap";
            Terrain_Height = TerrainParameter + "_Height";
        }

        public override bool IsValid(VisualEffect component)
        {
            return Terrain != null &&
                component.HasVector3(Terrain_Bounds_center) &&
                component.HasVector3(Terrain_Bounds_size) &&
                component.HasTexture(Terrain_HeightMap) &&
                component.HasFloat(Terrain_Height);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            Bounds b = Terrain.terrainData.bounds;

            component.SetVector3(Terrain_Bounds_center, b.center);
            component.SetVector3(Terrain_Bounds_size, b.size);
            component.SetTexture(Terrain_HeightMap, Terrain.terrainData.heightmapTexture);
            component.SetFloat(Terrain_Height, Terrain.terrainData.heightmapScale.y);
        }

        public override string ToString()
        {
            return string.Format("Sphere : '{0}' -> {1}", TerrainParameter, Terrain == null ? "(null)" : Terrain.name);
        }
    }
}
#endif
