using UnityEngine;
using System.Collections;

public class ForceToAcessibleVolume : MonoBehaviour {

	public float forceMultiplier;
	public float maxForce;
	public float distanceExponent;

	public int accesibleVolumeLayer;

	private Vector3 lastContactNormal;
	private Vector3 lastContanctPosition;
	private Vector3 previousPosition;

	private bool outsideVolume = false;
	private new Rigidbody rigidbody;
	
	void Start()
	{
		rigidbody = GetComponent<Rigidbody> ();
		lastContanctPosition = transform.position;
		previousPosition = transform.position;
	}

	void OnTriggerStay(Collider other) {
		if (outsideVolume && other.gameObject.layer == accesibleVolumeLayer)
		{
			lastContanctPosition = transform.position;
			outsideVolume = false;
		}
	}

	void OnTriggerExit(Collider other)
	{
		if (other.gameObject.layer == accesibleVolumeLayer)
		{
			outsideVolume = true;
			lastContactNormal = (transform.position - previousPosition).normalized;
		}
	}

	void FixedUpdate()
	{
		if (outsideVolume)
		{
			Vector3 displacement = transform.position - lastContanctPosition;
			float distanceFromVolume = displacement.magnitude;

			RaycastHit hit;
			if (Physics.Raycast(transform.position, -lastContactNormal, out hit,distanceFromVolume,1 << accesibleVolumeLayer))
			{
				lastContanctPosition = hit.point;
				lastContactNormal = hit.normal;
				displacement = transform.position - lastContanctPosition;
				distanceFromVolume = displacement.magnitude;
			}

			Vector3 forceDirection = -displacement.normalized;
			float forceAmount = Mathf.Clamp(Mathf.Pow(distanceFromVolume,distanceExponent),0,maxForce);
			rigidbody.AddForce(forceAmount*forceDirection);
		}
		previousPosition = transform.position;
	}
}
