//------------------------------------------------------------------------------------------------
// Edy's Vehicle Physics
// (c) Angel Garcia "Edy" - Oviedo, Spain
// http://www.edy.es
//------------------------------------------------------------------------------------------------

using UnityEngine;

namespace EVP
{

// Data stored per wheel for tire effects

public class TireFxData
	{
	// Tire marks

	public TireMarksRenderer lastRenderer;
	public int lastMarksIndex = -1;
	public float marksDelta = 0.0f;

	// Tire particles

	public TireParticleEmitter lastEmitter;
	public float lastParticleTime = -1.0f;
	public float slipTime = 0.0f;
	}


[RequireComponent(typeof(VehicleController))]
public class VehicleTireEffects : MonoBehaviour
	{
	public float tireWidth = 0.2f;
	public float minSlip = 1.0f;
	public float maxSlip = 5.0f;

	[Header("Tire marks")]
	[Range(0,2)]
	public float intensity = 1.0f;
	public float updateInterval = 0.02f;

	[Header("Smoke")]
	public float minIntensityTime = 0.5f;
	public float maxIntensityTime = 6.0f;
	public float limitIntensityTime = 10.0f;


	VehicleController m_vehicle;
	TireFxData[] m_fxData = new TireFxData[0];


	void OnEnable ()
		{
		m_vehicle = GetComponent<VehicleController>();
		m_vehicle.computeExtendedTireData = true;
		}


	void Update ()
		{
		if (m_vehicle.paused) return;

		if (m_vehicle.wheelData.Length != m_fxData.Length)
			InitializeTireFxData();

		for (int i=0, c=m_fxData.Length; i<c; i++)
			{
			WheelData wd = m_vehicle.wheelData[i];
			TireFxData md = m_fxData[i];
			UpdateTireMarks(wd, md);
			UpdateTireParticles(wd, md);
			}
		}


	//--------------------------------------------------------------------------------------------


	void InitializeTireFxData ()
		{
		m_fxData = new TireFxData[m_vehicle.wheelData.Length];

		for (int i=0; i<m_fxData.Length; i++)
			m_fxData[i] = new TireFxData();
		}


	void UpdateTireMarks (WheelData wheelData, TireFxData fxData)
		{
		// If we are already drawing marks to this wheel, wait before updating.

		if (fxData.lastMarksIndex != -1 && wheelData.grounded && fxData.marksDelta < updateInterval)
			{
			fxData.marksDelta += Time.deltaTime;
			return;
			}

		// deltaT = time since last mark for this wheel

		float deltaT = fxData.marksDelta;
		if (deltaT == 0.0f)
			deltaT = Time.deltaTime;
		fxData.marksDelta = 0.0f;

		// Verify: Should we put marks?
		// - Grounded
		// - Contacted object has not a rigidbody (assumed to be static)

		if (!wheelData.grounded || wheelData.hit.collider.attachedRigidbody != null)
			{
			fxData.lastMarksIndex = -1;
			return;
			}

		// Have we changed renderer? If so, start a new tread

		TireMarksRenderer marksRenderer =
			wheelData.groundMaterial != null? wheelData.groundMaterial.marksRenderer : null;

		if (marksRenderer != fxData.lastRenderer)
			{
			fxData.lastRenderer = marksRenderer;
			fxData.lastMarksIndex = -1;
			}

		if (marksRenderer != null)
			{
			float pressureRatio = Mathf.Clamp01(intensity * wheelData.downforceRatio * 0.5f);
			float skidRatio = Mathf.InverseLerp(minSlip, maxSlip, wheelData.combinedTireSlip);

			fxData.lastMarksIndex = marksRenderer.AddMark(
				wheelData.rayHit.point - wheelData.transform.right * wheelData.collider.center.x + wheelData.velocity * deltaT,
				wheelData.rayHit.normal,
				pressureRatio,
				skidRatio,
				tireWidth,
				fxData.lastMarksIndex
				);
			}
		}


	void UpdateTireParticles (WheelData wheelData, TireFxData fxData)
		{
		if (!wheelData.grounded)
			{
			// Not grounded: clear particle state and decrement the particle slip time

			fxData.lastParticleTime = -1.0f;

			fxData.slipTime -= Time.deltaTime;
			if (fxData.slipTime < 0.0f) fxData.slipTime = 0.0f;
			return;
			}

		TireParticleEmitter particleEmitter =
			wheelData.groundMaterial != null? wheelData.groundMaterial.particleEmitter : null;

		if (particleEmitter != fxData.lastEmitter)
			{
			fxData.lastEmitter = particleEmitter;
			fxData.lastParticleTime = -1.0f;
			}

		if (particleEmitter != null)
			{
			Vector3 position = wheelData.rayHit.point + wheelData.transform.up * tireWidth * 0.5f;
			Vector3 positionRandom = Random.insideUnitSphere * tireWidth;

			float pressureRatio = Mathf.Clamp01(wheelData.downforceRatio);
			float skidRatio = Mathf.InverseLerp(minSlip, maxSlip, wheelData.combinedTireSlip);

			// Emulate tire "heating" as for the time it has been skidding over the minSlip value.
			// Tire will "heat" at full rate when completely skidding at full pressure.

			if (skidRatio > 0.0f && particleEmitter.mode == TireParticleEmitter.Mode.PressureAndSkid)
				fxData.slipTime += Time.deltaTime * skidRatio * pressureRatio;
			else
				fxData.slipTime -= Time.deltaTime;
			fxData.slipTime = Mathf.Clamp(fxData.slipTime, 0.0f, limitIntensityTime);

			float slipTimeRatio = Mathf.InverseLerp(minIntensityTime, maxIntensityTime, fxData.slipTime);

			fxData.lastParticleTime = particleEmitter.EmitParticle(
				position + positionRandom,
				wheelData.velocity,
				wheelData.tireSlip.y * wheelData.transform.forward,
				pressureRatio,
				skidRatio * slipTimeRatio,
				fxData.lastParticleTime
				);
			}
		else
			{
			// No particles set up for this material. Assume is not a "heating material"
			// and "cold down" the tire surface.

			fxData.slipTime -= Time.deltaTime;
			if (fxData.slipTime < 0.0f) fxData.slipTime = 0.0f;
			}
		}
	}
}
