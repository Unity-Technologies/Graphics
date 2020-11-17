//------------------------------------------------------------------------------------------------
// Edy's Vehicle Physics
// (c) Angel Garcia "Edy" - Oviedo, Spain
// http://www.edy.es
//------------------------------------------------------------------------------------------------

using UnityEngine;
using System;

namespace EVP
{

// Per-wheel audio-related data

public class WheelAudioData
	{
	public float lastDownforce = 0.0f;
	public float lastWheelBumpTime = 0.0f;
	}


[RequireComponent(typeof(VehicleController))]
public class VehicleAudio : MonoBehaviour
	{
	// Settings are arranged in classes for better organization

	[Serializable]
	public class Engine
		{
		public AudioSource audioSource;

        [Space(5)]
		public float idleRpm = 600.0f;
		public float idlePitch = 0.25f;
		public float idleVolume = 0.4f;
        [Space(5)]
		public float maxRpm = 6000.0f;
		public float maxPitch = 1.5f;
		public float maxVolume = 0.6f;
        [Space(5)]
		public float throttleRpmBoost = 500.0f;
		public float throttleVolumeBoost = 0.4f;
		[Space(5)]
		public float wheelsToEngineRatio = 16.0f;
		public int gears = 5;
		public float gearDownRpm = 2500.0f;
		public float gearUpRpm = 5000.0f;
		[Space(5)]
		public float gearUpRpmRate = 5.0f;
		public float gearDownRpmRate = 6.0f;
		public float volumeChangeRateUp = 48.0f;
		public float volumeChangeRateDown = 16.0f;
		}

	[Serializable]
	public class EngineExtras
		{
		public AudioSource turboAudioSource;
		public float turboMinRpm = 3500.0f;
		public float turboMaxRpm = 5500.0f;
		[Range(0,3)]
		public float turboMinPitch = 0.8f;
		[Range(0,3)]
		public float turboMaxPitch = 1.5f;
		[Range(0,1)]
		public float turboMaxVolume = 1.0f;
		public bool turboRequiresThrottle = true;

		[Space(5)]
		public AudioClip turboDumpClip;
		public float turboDumpMinRpm = 5000.0f;
		public float turboDumpMinInterval = 2.0f;
		public float turboDumpMinThrottleTime = 0.3f;
		public float turboDumpVolume = 0.5f;

		[Space(5)]
		public AudioSource transmissionAudioSource;
		[Range(0.1f,1)]
		public float transmissionMaxRatio = 0.9f;		// Ratio of maximum transmission velocity (maxRpm * gearCount) at which transmission is considered maximum
		[Range(0,3)]
		public float transmissionMinPitch = 0.2f;
		[Range(0,3)]
		public float transmissionMaxPitch = 1.1f;
		[Range(0,1)]
		public float transmissionMinVolume = 0.1f;
		[Range(0,1)]
		public float transmissionMaxVolume = 0.2f;
		}

	[Serializable]
	public class Wheels
		{
		public AudioSource skidAudioSource;
		public float skidMinSlip = 2.0f;
		public float skidMaxSlip = 7.0f;
		[Range(0,3)]
		public float skidMinPitch = 0.9f;
		[Range(0,3)]
		public float skidMaxPitch = 0.8f;
		[Range(0,1)]
		public float skidMaxVolume = 0.75f;

		[Space(5)]
		public AudioSource offroadAudioSource;
		public float offroadMinSpeed = 1.0f;
		public float offroadMaxSpeed = 20.0f;
		[Range(0,3)]
		public float offroadMinPitch = 0.3f;
		[Range(0,3)]
		public float offroadMaxPitch = 2.5f;
		[Range(0,1)]
		public float offroadMinVolume = 0.3f;
		[Range(0,1)]
		public float offroadMaxVolume = 0.6f;

		[Space(5)]
		public AudioClip bumpAudioClip;
		public float bumpMinForceDelta = 2000.0f;		// Minimum force change in a fixed time step to be considered a bump
		public float bumpMaxForceDelta = 18000.0f;		// Force change at which the bump is considered to be maximum intensity
		[Range(0,1)]
		public float bumpMinVolume = 0.2f;				// Volume to be applied at the minimum bump intensity
		[Range(0,1)]
		public float bumpMaxVolume = 0.6f;				// Volume to be applied at the maximum bump intensity
		}

	[Serializable]
	public class Impacts
		{
		[Space(5)]
		public AudioClip hardImpactAudioClip;
		public AudioClip softImpactAudioClip;
		public float minSpeed = 0.1f;				// Min contact speed to be considered an impact.
		public float maxSpeed = 10.0f;				// Contact speed at which the impact is considered maximum intensity.
		[Range(0,3)]
		public float minPitch = 0.3f;				// Pitch to be applied at minimum impact speed
		[Range(0,3)]
		public float maxPitch = 0.6f;				// Pitch to ba applied at maximum impact speed
		[Range(0,3)]
		public float randomPitch = 0.2f;			// Random pitch range (+- value) to be added to the pitch
		[Range(0,1)]
		public float minVolume = 0.7f;				// Volume to be applied at minimum impact speed
		[Range(0,1)]
		public float maxVolume = 1.0f;				// Volume to be applied at maximum impact speed
		[Range(0,1)]
		public float randomVolume = 0.2f;			// Random volume range (+- value) to be added to the volume
		}

	[Serializable]
	public class Drags
		{
		public AudioSource hardDragAudioSource;
		public AudioSource softDragAudioSource;
		public float minSpeed = 2.0f;
		public float maxSpeed = 20.0f;
		[Range(0,3)]
		public float minPitch = 0.6f;
		[Range(0,3)]
		public float maxPitch = 0.8f;
		[Range(0,1)]
		public float minVolume = 0.8f;
		[Range(0,1)]
		public float maxVolume = 1.0f;

		[Space(5)]
		public AudioClip scratchAudioClip;
		public float scratchRandomThreshold = 0.02f;	// Percentage of drag events that cause a drag
		public float scratchMinSpeed = 2.0f;
		public float scratchMinInterval = 0.2f;
		[Range(0,3)]
		public float scratchMinPitch = 0.7f;
		[Range(0,3)]
		public float scratchMaxPitch = 1.1f;
		[Range(0,1)]
		public float scratchMinVolume = 0.9f;
		[Range(0,1)]
		public float scratchMaxVolume = 1.0f;
		}

	[Serializable]
	public class Wind
		{
		public AudioSource windAudioSource;
		public float minSpeed = 3.0f;
		public float maxSpeed = 30.0f;
		[Range(0,3)]
		public float minPitch = 0.5f;
		[Range(0,3)]
		public float maxPitch = 1.0f;
		[Range(0,1)]
		public float maxVolume = 0.5f;
		}


	// Actual public properties

	[Tooltip("AudioSource to be used with the one-time audio effects (impacts, etc)")]
	public AudioSource audioClipTemplate;
	[Space(2)]
	public Engine engine = new Engine();
	[Space(2)]
	public EngineExtras engineExtras = new EngineExtras();
	[Space(2)]
	public Wheels wheels = new Wheels();
	[Space(2)]
	public Impacts impacts = new Impacts();
	[Space(2)]
	public Drags drags = new Drags();
	[Space(2)]
	public Wind wind = new Wind();

	// Additional less-relevant properties. To be configured from scripting if necessary.

	[NonSerialized] public float skidRatioChangeRate = 40.0f;
	[NonSerialized] public float offroadSpeedChangeRate = 20.0f;
	[NonSerialized] public float offroadCutoutSpeed = 0.02f;
	[NonSerialized] public float dragCutoutSpeed = 0.01f;
	[NonSerialized] public float turboRatioChangeRate = 8.0f;
	[NonSerialized] public float wheelsRpmChangeRateLimit = 400.0f;


	// Private fields

	VehicleController m_vehicle;
	float m_engineRpm = 0.0f;
	float m_engineThrottleRpm = 0.0f;
	float m_engineRpmDamp;

	float m_wheelsRpm = 0.0f;

	int m_gear = 0;
	int m_lastGear = 0;

	float m_skidRatio = 0.0f;
	float m_offroadSpeed = 0.0f;
	float m_lastScratchTime = 0.0f;
	float m_turboRatio = 0.0f;
	float m_lastTurboDumpTime = 0.0f;
	float m_lastThrottleInput = 0.0f;
	float m_lastThrottlePressedTime = 0.0f;

	WheelAudioData[] m_audioData = new WheelAudioData[0];


	// Public access to some private fields

	public int simulatedGear { get { return m_gear; } }
	public float simulatedEngineRpm { get { return m_engineRpm; } }


	void OnEnable ()
		{
		// Configure the vehicle: report impacts, compute extended tire data (for skid)

		m_vehicle = GetComponent<VehicleController>();
		m_vehicle.processContacts = true;
		m_vehicle.onImpact += DoImpactAudio;
		m_vehicle.computeExtendedTireData = true;

		// Verify / configure parameters

		if (engine.gears < 2)
			engine.gears = 2;

		m_engineRpmDamp = engine.gearUpRpmRate;
		m_wheelsRpm = 0.0f;

		// Verify the audio sources to belong to the actual vehicle (editor only)

		VerifyAudioSources();
		}


	void OnDisable ()
		{
		StopAllAudioSources();
		}


	void Update ()
		{
		if (m_vehicle.paused)
			{
			StopAllAudioSources();
			return;
			}

		DoEngineAudio();

		// At this point these values are available:
		//
		//	- m_gear and m_lastGear (m_gear comes from DoEngineAudio)
		//	- m_vehicle.throttleInput and m_lastThrottleInput
		//
		// This allows other parts to react to gear and throttle changes.

		DoEngineExtraAudio();
		DoBodyDragAudio();
		DoWindAudio();

		DoTireAudio();

		m_lastGear = m_gear;
		m_lastThrottleInput = m_vehicle.throttleInput;
		}


	void FixedUpdate ()
		{
		if (m_vehicle.wheelData.Length != m_audioData.Length)
			InitializeAudioData();

		// TO-DO: Base it on the speed of movement instead of the force.
		// We should trace the speed of movement in WheelData, in the vehicle's fixed update.
		// Then move to Update.
		// Right now it's somewhat designed to use FixedUpdate

		if (!m_vehicle.paused)
			DoWheelBumpAudio();
		}


	void InitializeAudioData ()
		{
		m_audioData = new WheelAudioData[m_vehicle.wheelData.Length];

		for (int i=0; i<m_audioData.Length; i++)
			m_audioData[i] = new WheelAudioData();
		}


	void DoEngineAudio ()
		{
		// Get the average RPMs of the drive wheels

		float averageWheelRate = 0.0f;
		int driveWheels = 0;

		foreach (WheelData wd in m_vehicle.wheelData)
			{
			if (wd.wheel.drive)
				{
				driveWheels++;
				averageWheelRate += wd.angularVelocity;
				}
			}

		if (driveWheels == 0)
			{
			if (engine.audioSource != null)
				engine.audioSource.Stop();
			return;
			}

		averageWheelRate /= driveWheels;
		m_wheelsRpm = Mathf.MoveTowards(m_wheelsRpm, averageWheelRate * Mathf.Rad2Deg / 6.0f, wheelsRpmChangeRateLimit * Time.deltaTime);

		// Get the RPM at the output of the gearbox

		float transmissionRpm = m_wheelsRpm * engine.wheelsToEngineRatio;

		// Calculate the engine RPM according to three possible states:
		// - Stopped
		// - Moving forward. The top gear can increase the sound pitch until its limit
		// - Reverse. Single gear, sound pitch is increased until its limit

		float updatedEngineRpm;

		if (Mathf.Abs(m_wheelsRpm) < 1.0f)
			{
			m_gear = 0;
			updatedEngineRpm = engine.idleRpm + Mathf.Abs(transmissionRpm);
			}
		else if (transmissionRpm >= 0)
			{
			// First gear goes from idle to gearUp

			float firstGear = engine.gearUpRpm - engine.idleRpm;

			if (transmissionRpm < firstGear)
				{
				m_gear = 1;
				updatedEngineRpm = transmissionRpm + engine.idleRpm;
				}
			else
				{
				// Second gear and above go from gearDown to gearUp

				float gearWidth = engine.gearUpRpm - engine.gearDownRpm;

				m_gear = 2 + (int)((transmissionRpm - firstGear) / gearWidth);

				if (m_gear > engine.gears)
					{
					m_gear = engine.gears;
					updatedEngineRpm = transmissionRpm - firstGear - (engine.gears - 2) * gearWidth + engine.gearDownRpm;
					}
				else
					{
					updatedEngineRpm = Mathf.Repeat(transmissionRpm - firstGear, gearWidth) + engine.gearDownRpm;
					}
				}
			}
		else
			{
			// Reverse gear

			m_gear = -1;
			updatedEngineRpm = Mathf.Abs(transmissionRpm) + engine.idleRpm;
			}

		updatedEngineRpm = Mathf.Clamp(updatedEngineRpm, 10.0f, engine.maxRpm);

		if (m_gear != m_lastGear)
			{
			m_engineRpmDamp = m_gear > m_lastGear ? engine.gearUpRpmRate : engine.gearDownRpmRate;

			// m_lastGear will be configured outside, so other methods can detect gear changes as well.
			}

		m_engineRpm = Mathf.Lerp(m_engineRpm, updatedEngineRpm, m_engineRpmDamp * Time.deltaTime);
		m_engineThrottleRpm = Mathf.Lerp(m_engineThrottleRpm, m_vehicle.throttleInput * engine.throttleRpmBoost, m_engineRpmDamp * Time.deltaTime);

		if (engine.audioSource != null)
			{
			float engineRatio = Mathf.InverseLerp(engine.idleRpm, engine.maxRpm, m_engineRpm + m_engineThrottleRpm);
			ProcessContinuousAudioPitch(engine.audioSource, engineRatio, engine.idlePitch, engine.maxPitch);

			float engineVolume = Mathf.Lerp(engine.idleVolume, engine.maxVolume, engineRatio)
				+ Mathf.Abs(m_vehicle.throttleInput) * engine.throttleVolumeBoost;
			ProcessVolume(engine.audioSource, engineVolume, engine.volumeChangeRateUp, engine.volumeChangeRateDown);
			}
		}


	void DoEngineExtraAudio ()
		{
		// Turbo audio

		float updatedTurboRatio = Mathf.InverseLerp(engineExtras.turboMinRpm, engineExtras.turboMaxRpm, m_engineRpm);
		if (engineExtras.turboRequiresThrottle)
			updatedTurboRatio *= Mathf.Clamp01(m_vehicle.throttleInput);
		m_turboRatio = Mathf.Lerp(m_turboRatio, updatedTurboRatio, turboRatioChangeRate * Time.deltaTime);

		ProcessContinuousAudio(engineExtras.turboAudioSource, m_turboRatio,
			engineExtras.turboMinPitch, engineExtras.turboMaxPitch, 0.0f, engineExtras.turboMaxVolume);

		// Turbo dump audio

		if (engineExtras.turboDumpClip != null)
			{
			if (Time.time-m_lastTurboDumpTime > engineExtras.turboDumpMinInterval && m_engineRpm > engineExtras.turboDumpMinRpm)
				{
				bool gearChangedUp = m_gear != m_lastGear && m_lastGear > 0 && m_gear > 0 && m_gear > m_lastGear;
				bool throttleReleased = m_vehicle.throttleInput < 0.5f && (m_vehicle.throttleInput - m_lastThrottleInput) / Time.deltaTime < -20.0f;

				float throttlePressedTime = Time.time - m_lastThrottlePressedTime;
				if (m_vehicle.throttleInput < 0.2f) m_lastThrottlePressedTime = Time.time;

				if ((gearChangedUp || throttleReleased) && throttlePressedTime > engineExtras.turboDumpMinThrottleTime)
					{
					Vector3 pos = engineExtras.turboAudioSource != null? engineExtras.turboAudioSource.transform.position : m_vehicle.cachedTransform.position;
					PlayOneTime(engineExtras.turboDumpClip, pos, engineExtras.turboDumpVolume);
					m_lastTurboDumpTime = Time.time;
					}
				}
			}

		// Transmission audio

		float transmissionRatio = Mathf.Abs(m_wheelsRpm * engine.wheelsToEngineRatio) / (engine.maxRpm * engine.gears * engineExtras.transmissionMaxRatio);

		ProcessContinuousAudio(engineExtras.transmissionAudioSource, transmissionRatio,
			engineExtras.transmissionMinPitch, engineExtras.transmissionMaxPitch, engineExtras.transmissionMinVolume, engineExtras.transmissionMaxVolume);
		}


	void DoTireAudio ()
		{
		float currentSkidRatio = 0.0f;
		float currentOffroadSpeed = 0.0f;
		int offroadWheels = 0;

		// Skid uses the sum of all wheels: a single wheel skidding to the top causes maximum value.
		// Offroad uses the average value of all wheels over offroad surface.

		foreach (WheelData wd in m_vehicle.wheelData)
			{
			// If no ground material is found, then the surface is considered "hard" (skid audio)

			if (wd.groundMaterial == null || wd.groundMaterial.surfaceType == GroundMaterial.SurfaceType.Hard)
				{
				// Skid value is the sum of the skid ratios based on the actual parameters

				currentSkidRatio += Mathf.InverseLerp(wheels.skidMinSlip, wheels.skidMaxSlip, wd.combinedTireSlip);
				}
			else
				{
				// Offroad value is the average velocity of the tire over the surface among all wheels.

				if (wd.grounded)
					{
					currentOffroadSpeed += wd.velocity.magnitude + Mathf.Abs(wd.tireSlip.y);
					offroadWheels++;
					}
				}
			}

		// Skid value receives the skid ratio based on wheels.skidMinSlip and wheels.skidMaxSlip

		m_skidRatio = Mathf.Lerp(m_skidRatio, currentSkidRatio, skidRatioChangeRate * Time.deltaTime);
		ProcessContinuousAudio(wheels.skidAudioSource, m_skidRatio, wheels.skidMinPitch, wheels.skidMaxPitch, 0.0f, wheels.skidMaxVolume);

		// Offroad value receives the actual velocity of the wheel over the surface.
		// It's split in two lineal ranges:
		//   - from cutout to min
		//	 - from min to max

		if (offroadWheels > 1) currentOffroadSpeed /= offroadWheels;
		m_offroadSpeed = Mathf.Lerp(m_offroadSpeed, currentOffroadSpeed, offroadSpeedChangeRate * Time.deltaTime);

		ProcessSpeedBasedAudio(wheels.offroadAudioSource,
			m_offroadSpeed, offroadCutoutSpeed, wheels.offroadMinSpeed, wheels.offroadMaxSpeed,
			0.0f, wheels.offroadMinPitch, wheels.offroadMaxPitch,
			wheels.offroadMinVolume, wheels.offroadMaxVolume);
		}


	void DoImpactAudio ()
		{
		// Body impacts

		if (impacts.hardImpactAudioClip != null || impacts.softImpactAudioClip != null)
			{
			float impactSpeed = m_vehicle.localImpactVelocity.magnitude;

			if (impactSpeed > impacts.minSpeed)
				{
				float impactRatio = Mathf.InverseLerp (impacts.minSpeed, impacts.maxSpeed, impactSpeed);
				AudioClip clip = null;

				if (!impacts.softImpactAudioClip)
					{
					clip = impacts.hardImpactAudioClip;
					}
				else
					{
					clip = m_vehicle.isHardImpact? impacts.hardImpactAudioClip : impacts.softImpactAudioClip;
					}

				if (clip)
					PlayOneTime(clip,
						m_vehicle.cachedTransform.TransformPoint(m_vehicle.localImpactPosition),
						Mathf.Lerp(impacts.minVolume, impacts.maxVolume, impactRatio) + UnityEngine.Random.Range(-impacts.randomVolume, impacts.randomVolume),
						Mathf.Lerp(impacts.minPitch, impacts.maxPitch, impactRatio) + UnityEngine.Random.Range(-impacts.randomPitch, impacts.randomPitch));

				// Debug.Log("Impact! " + impactRatio);
				}
			}
		}


	void DoBodyDragAudio ()
		{
		// Continuous drag audio

		float dragSpeed = m_vehicle.localDragVelocity.magnitude;
		float hardDragSpeed = m_vehicle.isHardDrag? dragSpeed : 0.0f;
		float softDragSpeed = m_vehicle.isHardDrag? 0.0f : dragSpeed;

		ProcessSpeedBasedAudio(drags.hardDragAudioSource, hardDragSpeed, dragCutoutSpeed, drags.minSpeed, drags.maxSpeed,
			0.0f, drags.minPitch, drags.maxPitch, drags.minVolume, drags.maxVolume);

		ProcessSpeedBasedAudio(drags.softDragAudioSource, softDragSpeed, dragCutoutSpeed, drags.minSpeed, drags.maxSpeed,
			0.0f, drags.minPitch, drags.maxPitch, drags.minVolume, drags.maxVolume);

		// Random body scratch sounds on hard surfaces only

		if (drags.scratchAudioClip != null)
			{
			if (dragSpeed > drags.scratchMinSpeed
				&& m_vehicle.isHardDrag
				&& UnityEngine.Random.value < drags.scratchRandomThreshold
				&& Time.time-m_lastScratchTime > drags.scratchMinInterval)
				{
				PlayOneTime(drags.scratchAudioClip,
					m_vehicle.cachedTransform.TransformPoint(m_vehicle.localDragPosition),
					UnityEngine.Random.Range(drags.scratchMinVolume, drags.scratchMaxVolume),
					UnityEngine.Random.Range(drags.scratchMinPitch, drags.scratchMaxPitch));
				m_lastScratchTime = Time.time;
				}
			}
		}


	void DoWheelBumpAudio ()
		{
		if (wheels.bumpAudioClip == null) return;

		for (int i=0, c=m_vehicle.wheelData.Length; i<c; i++)
			{
			WheelData wd = m_vehicle.wheelData[i];
			WheelAudioData ad = m_audioData[i];

			// Process wheel bumps first

			float suspensionForceDelta = wd.downforce - ad.lastDownforce;
			ad.lastDownforce = wd.downforce;

			if (suspensionForceDelta > wheels.bumpMinForceDelta && (Time.fixedTime - ad.lastWheelBumpTime) > 0.03f)
				{
				ProcessWheelBumpAudio(suspensionForceDelta, wd.transform.position);
				ad.lastWheelBumpTime = Time.fixedTime;
				}
			}
		}


	void DoWindAudio ()
		{
		float windRatio = Mathf.InverseLerp(wind.minSpeed, wind.maxSpeed, m_vehicle.cachedRigidbody.velocity.magnitude);

		ProcessContinuousAudio(wind.windAudioSource, windRatio,
			wind.minPitch, wind.maxPitch, 0.0f, wind.maxVolume);
		}


	void StopAllAudioSources ()
		{
		StopAudio(engine.audioSource);
		StopAudio(engineExtras.turboAudioSource);
		StopAudio(engineExtras.transmissionAudioSource);
		StopAudio(wheels.skidAudioSource);
		StopAudio(wheels.offroadAudioSource);
		StopAudio(drags.hardDragAudioSource);
		StopAudio(drags.softDragAudioSource);
		StopAudio(wind.windAudioSource);
		}


	//----------------------------------------------------------------------------------------------


	void StopAudio (AudioSource audio)
		{
		if (audio != null) audio.Stop();
		}


	void ProcessContinuousAudio (AudioSource audio, float ratio, float minPitch, float maxPitch, float minVolume, float maxVolume)
		{
		if (audio == null) return;

		audio.pitch = Mathf.Lerp(minPitch, maxPitch, ratio);
		audio.volume = Mathf.Lerp(minVolume, maxVolume, ratio);

		if (!audio.isPlaying && audio.isActiveAndEnabled) audio.Play();
		audio.loop = true;
		}


	void ProcessContinuousAudioPitch (AudioSource audio, float ratio, float minPitch, float maxPitch)
		{
		if (audio == null) return;

		audio.pitch = Mathf.Lerp(minPitch, maxPitch, ratio);

		if (!audio.isPlaying && audio.isActiveAndEnabled) audio.Play();
		audio.loop = true;
		}


	void ProcessVolume (AudioSource audio, float volume, float changeRateUp, float changeRateDown)
		{
		float changeRate = volume > audio.volume? changeRateUp : changeRateDown;
		audio.volume = Mathf.Lerp(audio.volume, volume, Time.deltaTime * changeRate);
		}


	void ProcessSpeedBasedAudio (AudioSource audio, float speed, float cutoutSpeed, float minSpeed, float maxSpeed, float cutoutPitch, float minPitch, float maxPitch, float minVolume, float maxVolume)
		{
		if (audio == null) return;

		if (speed < cutoutSpeed)
			{
			if (audio.isPlaying) audio.Stop();
			}
		else
			{
			if (speed < minSpeed)
				{
				float ratio = Mathf.InverseLerp(cutoutSpeed, minSpeed, speed);
				audio.pitch = Mathf.Lerp(cutoutPitch, minPitch, ratio);
				audio.volume = Mathf.Lerp(0.0f, minVolume, ratio);
				}
			else
				{
				float ratio = Mathf.InverseLerp(minSpeed, maxSpeed, speed);
				audio.pitch = Mathf.Lerp(minPitch, maxPitch, ratio);
				audio.volume = Mathf.Lerp(minVolume, maxVolume, ratio);
				}

			if (!audio.isPlaying && audio.isActiveAndEnabled) audio.Play();
			audio.loop = true;
			}
		}


	void ProcessWheelBumpAudio (float suspensionForceDelta, Vector3 position)
		{
		float bumpRatio = Mathf.InverseLerp(wheels.bumpMinForceDelta, wheels.bumpMaxForceDelta, suspensionForceDelta);
		PlayOneTime(wheels.bumpAudioClip, position, Mathf.Lerp(wheels.bumpMinVolume, wheels.bumpMaxVolume, bumpRatio));
		}


	// Playing audio effects.
	//
	// We don't use AudioSource.PlayClipAtPoint because we need our sounds to be
	// parented to the vehicle.

	void PlayOneTime (AudioClip clip, Vector3 position, float volume)
		{
		PlayOneTime(clip, position, volume, 1.0f);
		}


	void PlayOneTime (AudioClip clip, Vector3 position, float volume, float pitch)
		{
		if (clip == null || pitch < 0.01f || volume < 0.01f) return;

		GameObject go;
		AudioSource source;

		if (audioClipTemplate != null)
			{
			go = (GameObject)Instantiate(audioClipTemplate.gameObject, position, Quaternion.identity);
			source = go.GetComponent<AudioSource>();
			go.transform.parent = audioClipTemplate.transform.parent;
			}
		else
			{
			go = new GameObject("One shot audio");
			go.transform.parent = m_vehicle.cachedTransform;
			go.transform.position = position;
			source = null;
			}

		if (source == null)
			{
			source = go.AddComponent<AudioSource>() as AudioSource;
			source.spatialBlend = 1.0f;
			}

		if (source.isActiveAndEnabled)
			{
			source.clip = clip;
			source.volume = volume;
			source.pitch = pitch;
			source.dopplerLevel = 0.0f;		// Doppler causes artifacts as for positioning the audio source
			source.Play();
			}

		Destroy(go, clip.length / pitch);
		}


	void VerifyAudioSources ()
		{
		#if UNITY_EDITOR
		VerifyAudioSource(ref engine.audioSource);
		VerifyAudioSource(ref engineExtras.turboAudioSource);
		VerifyAudioSource(ref engineExtras.transmissionAudioSource);
		VerifyAudioSource(ref wheels.skidAudioSource);
		VerifyAudioSource(ref wheels.offroadAudioSource);
		VerifyAudioSource(ref drags.hardDragAudioSource);
		VerifyAudioSource(ref drags.softDragAudioSource);
		VerifyAudioSource(ref wind.windAudioSource);
		VerifyAudioSource(ref audioClipTemplate);
		#endif
		}


	void VerifyAudioSource (ref AudioSource audioSource)
		{
		if (audioSource != null && !audioSource.transform.IsChildOf(m_vehicle.transform))
			{
			Debug.LogWarning(m_vehicle.name + ": VehicleAudio: The audio source [" + audioSource.name + "] is not child of this vehicle. Disabled.", m_vehicle);
			audioSource = null;
			}
		}
	}
}
