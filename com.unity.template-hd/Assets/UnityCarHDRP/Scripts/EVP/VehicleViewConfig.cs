//------------------------------------------------------------------------------------------------
// Edy's Vehicle Physics
// (c) Angel Garcia "Edy" - Oviedo, Spain
// http://www.edy.es
//------------------------------------------------------------------------------------------------

using UnityEngine;

namespace EVP
{

public class VehicleViewConfig : MonoBehaviour
	{
	public Transform lookAtPoint;
	public Transform driverView;

	public float viewDistance = 10.0f;
	public float viewHeight = 3.5f;
	public float viewDamping = 3.0f;
	public float viewMinDistance = 3.8f;
	public float viewMinHeight = 0.0f;

	public float targetDiameter { get; private set; }


	void Awake ()
		{
		Bounds b = new Bounds(transform.position, Vector3.zero);
		foreach (Renderer r in GetComponentsInChildren<Renderer>())
			b.Encapsulate(r.bounds);

		targetDiameter = (b.size.x + b.size.y + b.size.z) / 3.0f;
		}
	}
}