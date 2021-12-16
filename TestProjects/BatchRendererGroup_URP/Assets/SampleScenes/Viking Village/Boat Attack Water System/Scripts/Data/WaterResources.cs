using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaterSystem
{
	/// <summary>
	/// This scriptable object holds default resources for the water rendering
	/// </summary>
	[System.Serializable][CreateAssetMenu(fileName = "WaterResources", menuName = "WaterSystem/Resource", order = 0)]
	public class WaterResources : ScriptableObject 
	{
		public Texture2D defaultFoamRamp; // a default foam ramp for the basic foam setting
        public Texture2D defaultFoamMap; // a default foam texture map
        public Texture2D defaultSurfaceMap; // a default normal/caustic map
        public Material defaultSeaMaterial;
        public Mesh[] defaultWaterMeshes;
	}
}
