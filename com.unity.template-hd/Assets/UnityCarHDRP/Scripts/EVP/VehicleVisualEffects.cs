using UnityEngine;
using System.Collections;

namespace EVP
{

[RequireComponent(typeof(VehicleController))]
public class VehicleVisualEffects : MonoBehaviour
	{
	[Header("Steering wheel")]
	public Transform steeringWheel;
	public float degreesOfRotation = 420.0f;

	[Header("Brake lights")]
	public Renderer brakesRenderer;
	public int brakesMaterialIndex;
	public Material brakesOnMaterial;
	public Material brakesOffMaterial;


	VehicleController m_vehicle;
	bool m_prevBrakes = false;


	void OnEnable ()
		{
		m_vehicle = GetComponent<VehicleController>();
		}


	void Update ()
		{
		// Steering wheel rotation

		if (steeringWheel != null)
			{
			Vector3 angles = steeringWheel.localEulerAngles;

			if (m_vehicle.maxSteerAngle > 0.0f)
				angles.z = -0.5f * degreesOfRotation * m_vehicle.steerAngle / m_vehicle.maxSteerAngle;
			else
				angles.z = 0.0f;

			steeringWheel.localEulerAngles = angles;
			}

		// Brake lights

		bool brakes = m_vehicle.brakeInput > 0.1f;
		if (brakes != m_prevBrakes)
			{
			if (brakesRenderer != null && brakesMaterialIndex >= 0 && brakesMaterialIndex < brakesRenderer.sharedMaterials.Length)
				{
				Material[] materialsCopy = brakesRenderer.materials;
				Destroy(materialsCopy[brakesMaterialIndex]);
				materialsCopy[brakesMaterialIndex] = brakes? brakesOnMaterial : brakesOffMaterial;
				brakesRenderer.materials = materialsCopy;
				}

			m_prevBrakes = brakes;
			}
		}
	}
}