using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace WaterSystem.Data
{
	/// <summary>
	/// This scriptable object stores teh graphical/rendering settings for a water system
	/// </summary>
    [System.Serializable][CreateAssetMenu(fileName = "WaterSettingsData", menuName = "WaterSystem/Settings", order = 0)]
    public class WaterSettingsData : ScriptableObject
    {
		public GeometryType waterGeomType; // The type of geometry, either vertex offset or tessellation
		public LayerMask depthMapCullingMask = 1 << 10;
		public ReflectionType refType = ReflectionType.PlanarReflection; // How the reflecitons are generated
		// planar
		public PlanarReflections.PlanarReflectionSettings planarSettings; // Planar reflection settings
		// cubemap
		public Cubemap cubemapRefType; // custom cubemap reference
		
		public bool isInfinite; // Is the water infinite (shader incomplete)
		public Vector4 originOffset = new Vector4(0f, 0f, 500f, 500f);
	}

	/// <summary>
	/// The type of reflection source, custom cubemap, closest refelction probe, planar reflection
	/// </summary>
	[System.Serializable]
	public enum ReflectionType
	{
		Cubemap,
		ReflectionProbe,
		PlanarReflection
	}

	/// <summary>
	/// The type of geometry, either vertex offset or tessellation
	/// </summary>
	[System.Serializable]
	public enum GeometryType
	{
		VertexOffset,
		Tesselation
	}
}
