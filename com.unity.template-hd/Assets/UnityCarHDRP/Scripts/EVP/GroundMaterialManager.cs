//------------------------------------------------------------------------------------------------
// Edy's Vehicle Physics
// (c) Angel Garcia "Edy" - Oviedo, Spain
// http://www.edy.es
//------------------------------------------------------------------------------------------------

using UnityEngine;
using System;

namespace EVP
{

[Serializable]
public class GroundMaterial
	{
	public PhysicMaterial physicMaterial;

	public float grip = 1.0f;
	public float drag = 0.1f;

	public TireMarksRenderer marksRenderer;
	public TireParticleEmitter particleEmitter;

	// Surface type affects the audio clips and other effects that are invoked
	// depending on the surface. See the VehicleAudio component.
	//
	// Hard: tire skid audio, hard impacts, hard body drag, body scratches
	// Soft: offroad rumble, soft impacts, soft body drag

	public enum SurfaceType { Hard, Soft };
	public SurfaceType surfaceType = SurfaceType.Hard;
	}


public class GroundMaterialManager : MonoBehaviour
	{
	public GroundMaterial[] groundMaterials = new GroundMaterial[0];


	#if UNITY_EDITOR
	// Editor-only code for initializing the elements in the groundMaterials array from
	// a zero-sized array. Otherwise, they would be initialized to all-zero.

	bool m_firstDeserialization = true;
	int m_materialsLength = 0;

	void OnValidate ()
		{
		if (m_firstDeserialization)
			{
			m_materialsLength = groundMaterials.Length;
			m_firstDeserialization = false;
			}
		else
			{
			if (groundMaterials.Length != m_materialsLength)
				{
				if (m_materialsLength == 0)
					{
					for (int i = 0; i < groundMaterials.Length; i++)
						groundMaterials[i] = new GroundMaterial();
					}

				m_materialsLength = groundMaterials.Length;
				}
			}
		}
	#endif


	public GroundMaterial GetGroundMaterial (PhysicMaterial physicMaterial)
		{
		for (int i=0, c=groundMaterials.Length; i<c; i++)
			{
			if (groundMaterials[i].physicMaterial == physicMaterial)
				return groundMaterials[i];
			}

		return null;
		}
	}
}