#if VFX_HAS_TERRAINMODULE
using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    [AddComponentMenu("VFX/Property Binders/Terrain Binder")]
    [VFXBinder("Utility/Terrain")]
    class VFXTerrainBinder : VFXBinderBase
    {
        public string Property { get { return (string)m_Property; } set { m_Property = value; UpdateSubProperties(); } }

        [VFXPropertyBinding("UnityEditor.VFX.TerrainType"), UnityEngine.Serialization.FormerlySerializedAs("TerrainParameter")]
        public ExposedProperty m_Property;
        public Terrain Terrain = null;

        private ExposedProperty Terrain_Bounds_center;
        private ExposedProperty Terrain_Bounds_size;
        private ExposedProperty Terrain_HeightMap;
        private ExposedProperty Terrain_Height;

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateSubProperties();
        }

        private void OnValidate()
        {
            UpdateSubProperties();
        }

        void UpdateSubProperties()
        {
            Terrain_Bounds_center = m_Property + "_Bounds_center";
            Terrain_Bounds_size = m_Property + "_Bounds_size";
            Terrain_HeightMap = m_Property + "_HeightMap";
            Terrain_Height = m_Property + "_Height";
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
            return string.Format("Sphere : '{0}' -> {1}", m_Property, Terrain == null ? "(null)" : Terrain.name);
        }
    }
}
#endif
