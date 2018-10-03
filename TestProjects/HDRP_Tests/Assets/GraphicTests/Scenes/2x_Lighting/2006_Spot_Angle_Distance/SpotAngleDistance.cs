using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

[RequireComponent(typeof(Light))]
public class SpotAngleDistance : MonoBehaviour 
{
	public float targetRadius = 1f;
	public float referenceIntensity = 600f;
	public float referenceDistance = 1.5f;
	public float additionalRange = 10f;

	public enum Mode {Distance, Angle}
	public Mode mode = Mode.Distance;
	public float distance = 3f;
	public float angle = 60f;

	[SerializeField, HideInInspector] new private Light light;
	[SerializeField, HideInInspector] private HDAdditionalLightData hdLightData;

	void OnValidate()
	{
		if (light == null) light = GetComponent<Light>();
		if (light == null) return;
		if (hdLightData == null) hdLightData = GetComponent<HDAdditionalLightData>();
		if (hdLightData == null) return;

		if ( mode == Mode.Distance)
		{
			float t = targetRadius / distance;
			angle = Mathf.Atan(t) * Mathf.Rad2Deg * 2f;
		}
		else
		{
			float t = Mathf.Tan(Mathf.Deg2Rad * angle * 0.5f);
			distance = targetRadius / t;
		}

		float sphereRadius = targetRadius / Mathf.Sin( Mathf.Deg2Rad * angle * 0.5f );

		light.spotAngle = angle;
		light.range = sphereRadius + additionalRange;
		transform.localPosition = -sphereRadius * Vector3.forward;

		hdLightData.intensity = referenceIntensity * Mathf.Pow( sphereRadius / referenceDistance , 2f );
	}
}
