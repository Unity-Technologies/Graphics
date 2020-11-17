//------------------------------------------------------------------------------------------------
// Edy's Vehicle Physics
// (c) Angel Garcia "Edy" - Oviedo, Spain
// http://www.edy.es
//------------------------------------------------------------------------------------------------

#if !UNITY_5_0 && !UNITY_5_1
#define UNITY_52_OR_GREATER
#endif

using UnityEngine;
using UnityEngine.Serialization;
using System;
using System.Collections.Generic;

namespace EVP
{

[Serializable]
public class Wheel
	{
	public WheelCollider wheelCollider;
	public Transform wheelTransform;
	public Transform caliperTransform;
	public bool steer = false;
	public bool drive = false;
	public bool brake = true;
	public bool handbrake = false;
	}


public class WheelData
	{
	public Wheel wheel;								// Wheel data from the inspector
	public WheelCollider collider;					// WheelCollider component for this wheel
	public Transform transform;						// Transform of the WheelCollider component
	public Vector3 origin;							// Origin point cosidering WheelCollider.center
	public float forceDistance;
	public float steerAngle = 0.0f;

	public bool grounded = false;
	public WheelHit hit;
	public GroundMaterial groundMaterial = null;

	public float suspensionCompression = 0.0f;
	public float downforce = 0.0f;

	public Vector3 velocity = Vector3.zero;
	public Vector2 localVelocity = Vector2.zero;
	public Vector2 localRigForce = Vector2.zero;

	public bool isBraking = false;
	public float finalInput = 0.0f;
	public Vector2 tireSlip = Vector2.zero;
	public Vector2 tireForce = Vector2.zero;
	public Vector2 dragForce = Vector2.zero;
	public Vector2 rawTireForce = Vector2.zero;

	public float angularVelocity = 0.0f;
	public float angularPosition = 0.0f;

	public PhysicMaterial lastPhysicMaterial = new PhysicMaterial();	// A new physic material ensures the first iteration to match the changes.
	public RaycastHit rayHit;											// Result of raycast for visual wheels. Used for precise positioning (i.e. tire marks).

	// Utility data

	public float positionRatio = 0.0f;
	public bool isWheelChildOfCaliper = false;

	// Extended tire data.
	// Calculated when extendedTireData is set to True by components.

	public float combinedTireSlip = 0.0f;		// Combined tire slip magnitude, in m/s
	public float downforceRatio = 0.0f;			// A relative measure of the amout of weight supported by each wheel. Will be 1.0 in a balanced car at rest.
	}


[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour
	{
	public Wheel[] wheels = new Wheel[0];

	public enum CenterOfMassMode { Transform, Parametric };

    [Header("Center of Mass")]

	public CenterOfMassMode centerOfMassMode = CenterOfMassMode.Parametric;
	[Range(0.1f, 0.9f)]
	public float centerOfMassPosition = 0.5f;
	[Range(-1.0f, 1.0f)]
	public float centerOfMassHeightOffset = 0.0f;
	[FormerlySerializedAs("centerOfMass")]
	public Transform centerOfMassTransform;

	[Header("Vehicle Setup")]

	[FormerlySerializedAs("maxSpeed")]
	public float maxSpeedForward = 27.78f;
	public float maxSpeedReverse = 12.0f;
	[Range(0,3)]
	public float tireFriction = 1.0f;
	[Range(0,1)]
	public float rollingResistance = 0.05f;
	[Range(0,1)]
	public float antiRoll = 0.2f;
	[Range(0,89)]
	public float maxSteerAngle = 35.0f;
	[Range(0,4)]
	public float aeroDrag = 0.0f;
	[Range(0,2)]
	public float aeroDownforce = 1.0f;

	[Header("Vehicle Balance")]

	[Range(0, 1)]
	public float driveBalance = 0.5f;
	[Range(0, 1)]
	public float brakeBalance = 0.5f;
	[Range(0.3f, 0.7f)]
	public float tireFrictionBalance = 0.5f;
	[Range(0,1)]
	public float aeroBalance = 0.5f;
	[Range(0,1)]
	public float handlingBias = 0.5f;

	[Header("Motor")]

	// [Space(5)]
	public float maxDriveForce = 2000.0f;

	[Range(0.0001f, 0.9999f)]
	public float forceCurveShape = 0.5f;
	public float maxDriveSlip = 4.0f;
	public float driveForceToMaxSlip = 1000.0f;

	[Header("Brakes")]

	public float maxBrakeForce = 3000.0f;
	public float brakeForceToMaxSlip = 1000.0f;

	public enum BrakeMode { Slip, Ratio };
	public BrakeMode brakeMode = BrakeMode.Slip;
	public float maxBrakeSlip = 2.0f;
	[Range(0,1)]
	public float maxBrakeRatio = 0.5f;

	public BrakeMode handbrakeMode = BrakeMode.Slip;
	public float maxHandbrakeSlip = 10.0f;
	[Range(0,1)]
	public float maxHandbrakeRatio = 1.0f;

	[Header("Driving Aids")]

	[FormerlySerializedAs("tcEnabled")]
	public bool tractionControl = false;
	[Range(0,1)]
	[FormerlySerializedAs("tcRatio")]
	public float tractionControlRatio = 1.0f;

	[FormerlySerializedAs("absEnabled")]
	public bool brakeAssist = false;
	[Range(0,1)]
	[FormerlySerializedAs("absRatio")]
	public float brakeAssistRatio = 1.0f;

	[FormerlySerializedAs("espEnabled")]
	public bool steeringLimit = false;
	[Range(0,1)]
	[FormerlySerializedAs("espRatio")]
	public float steeringLimitRatio = 0.5f;

	public bool steeringAssist = true;
	[Range(0,1)]
	public float steeringAssistRatio = 0.5f;

	// Vehicle controls

	[Range(-1,1)]
	public float steerInput = 0.0f;
	[Range(-1,1)]
	public float throttleInput = 0.0f;
	[Range(0,1)]
	public float brakeInput = 0.0f;
	[Range(0,1)]
	public float handbrakeInput = 0.0f;

	public enum UpdateRate { OnUpdate, OnFixedUpdate, Disabled };
	public enum PositionMode { Accurate, Fast };

	[Header("Visual Wheels")]
	[FormerlySerializedAs("wheelUpdateRate")]
	public UpdateRate wheelUpdateRate = UpdateRate.OnUpdate;
	public PositionMode wheelPositionMode = PositionMode.Accurate;

	[Header("Wheel Contact")]
	[Range(0,0.5f)]
	public float sleepVelocity = 0.2f;

	// Ground physic properties applied when no proper ground material is available.
	public float defaultGroundGrip = 1.0f;
	public float defaultGroundDrag = 0.0f;

	[Header("Optimization & Debug")]

	// Runtime updates can be disabled when the mass, the center of mass and the suspension
	// are not expected to change in runtime. They may still change, but the vehicle should
	// be disabled / enabled for the new values to have effect.

	public bool disallowRuntimeChanges = false;

	// In PhysX wheels are considered to point in the same direction as the vehicle's body.
	// The steer angle correction allows the wheels to point in any direction as defined
	// by their transform hierarchy.

	public bool disableSteerAngleCorrection = false;

	[FormerlySerializedAs("showContactGizmos")]
	public bool showCollisionGizmos = false;

	// Public non-exposed properties. To be configured from scripting if necessary.

	[NonSerialized] public bool processContacts = false;		// This is set to True by the components that use contacts (Audio, Damage)
	[NonSerialized] public float impactThreeshold = 0.6f;		// 0.0 - 1.0. The DotNormal of the impact is calculated. Less than this value means drag, more means impact.
	[NonSerialized] public float impactInterval = 0.2f;			// Time interval between processing impacts for visual or sound effects.
	[NonSerialized] public float impactIntervalRandom = 0.4f;	// Random percentaje for the impact interval, avoiding regularities.
	[NonSerialized] public float impactMinSpeed = 2.0f;			// Minimum relative velocity at which conctacts may be consideered impacts.

	[NonSerialized] public bool computeExtendedTireData = false;// Components using extended tire data (tire marks, smoke, particles, audio) set this to True


	// Add-on components subscribe to this delegate to get notified on impacts.
	// Use VehicleController.current to access the values of the controller that sent the event.

	public delegate void OnImpact();
	public OnImpact onImpact;

	public static VehicleController current = null;


	// Impact properties. Use from an OnImpact event only.
	// Private values get masaged properly just before invoking the event.

	public Vector3 localImpactPosition { get { return m_sumImpactPosition; } }
	public Vector3 localImpactVelocity { get { return m_sumImpactVelocity; } }
	public bool isHardImpact { get { return m_sumImpactHardness >= 0; } }

	// Body drag properties. Can be queued continuously.

	public Vector3 localDragPosition { get { return m_localDragPosition; } }
	public Vector3 localDragVelocity { get { return m_localDragVelocity; } }
	public bool isHardDrag { get { return m_localDragHardness >= 0; } }


	// Utility, also available for add-on components

	public static float RpmToW = (2.0f * Mathf.PI) / 60.0f;
	public static float WToRpm = 60.0f / (2.0f * Mathf.PI);

	public WheelData[] wheelData { get { return m_wheelData; } }

	public float speed { get { return m_speed; } }
	public float speedAngle { get { return m_speedAngle; } }
	public float steerAngle { get { return m_steerAngle; } }

	public bool invertVisualWheelSpinDirection { get; set; }


    // Cached and internal data, some accesible from scripting

	Transform m_transform;
	Rigidbody m_rigidbody;
	GroundMaterialManager m_groundMaterialManager;

	public Transform cachedTransform { get { return m_transform; } }
	public Rigidbody cachedRigidbody { get { return m_rigidbody; } }

	Rigidbody m_referenceBody = null;
	Rigidbody m_referenceCandidate = null;
	int m_referenceCandidateCount = 0;

	// Debug

	[NonSerialized] public string debugText = "";


	// Internal data

	WheelData[] m_wheelData = new WheelData[0];
	float m_speed = 0.0f;
	float m_speedAngle = 0.0f;
	float m_steerAngle = 0.0f;
	bool m_usesHandbrake = false;

	CommonTools.BiasLerpContext m_forceBiasCtx = new CommonTools.BiasLerpContext();

	VehicleFrame m_vehicleFrame;


	void OnValidate ()
		{
		// Some parameters must be non-negative.
		// This method is called from the Editor only.

		maxDriveSlip = Mathf.Max(maxDriveSlip, 0.0f);
		maxBrakeSlip = Mathf.Max(maxBrakeSlip, 0.0f);
		maxHandbrakeSlip = Mathf.Max(maxHandbrakeSlip, 0.0f);

		maxDriveForce = Mathf.Max(maxDriveForce, 0.0f);
		maxBrakeForce = Mathf.Max(maxBrakeForce, 0.0f);
		driveForceToMaxSlip = Mathf.Max(driveForceToMaxSlip, 1.0f);
		brakeForceToMaxSlip = Mathf.Max(brakeForceToMaxSlip, 1.0f);
		maxSpeedForward = Mathf.Max(maxSpeedForward, 0.0f);
		maxSpeedReverse = Mathf.Max(maxSpeedReverse, 0.0f);

		aeroDrag = Mathf.Max(aeroDrag, 0.0f);
		}


	void OnEnable ()
		{
		// Cache/find components and configure rigidbody

		m_transform = GetComponent<Transform>();
		m_rigidbody = GetComponent<Rigidbody>();
		m_groundMaterialManager = FindObjectOfType<GroundMaterialManager>();
		FindColliders();

		m_rigidbody.maxAngularVelocity = 14.0f;
		m_rigidbody.maxDepenetrationVelocity = 8.0f;

		if (wheels.Length == 0)
			{
			Debug.LogWarning("The wheels property is empty. You must configure wheels and WheelColliders first. Component is disabled.");
			enabled = false;
			return;
			}

		// Compute the reference frame for balancing parameters in runtime

		m_vehicleFrame = ComputeVehicleFrame();
		ConfigureCenterOfMass();

		// Initialize wheel data

		m_usesHandbrake = false;

		m_wheelData = new WheelData[wheels.Length];
		for (int i = 0; i < m_wheelData.Length; i++)
			{
			Wheel w = wheels[i];
			WheelData wd = new WheelData();

			if (w.wheelCollider == null)
				{
				Debug.LogError("A WheelCollider is missing in the list of wheels for this vehicle: " + gameObject.name);
				enabled = false;
				return;
				}

			if (w.caliperTransform != null && w.wheelTransform != null && w.caliperTransform.IsChildOf(w.wheelTransform))
				{
				Debug.LogWarning(this.ToString() + ": caliper (" + w.caliperTransform.name + ") should not be child of wheel (" + w.wheelTransform.name + ").\n"
					+ "Visual issues will surely appear. Either make wheel child of caliper, or put both at the same level (siblings).");
				}

			wd.isWheelChildOfCaliper = w.caliperTransform != null && w.wheelTransform != null && w.wheelTransform.IsChildOf(w.caliperTransform);

			wd.collider = w.wheelCollider;
			wd.transform = w.wheelCollider.transform;
			if (w.handbrake) m_usesHandbrake = true;

			// Calculate the force distance for center of mass and anti-roll

			UpdateWheelCollider(wd.collider);
			wd.forceDistance = GetWheelForceDistance(wd.collider);

			// Determine whether this wheel is "front" or "rear"

			float zPos = m_transform.InverseTransformPoint(wd.transform.TransformPoint(wd.collider.center)).z;
			wd.positionRatio = zPos >= m_vehicleFrame.middlePoint? 1.0f : 0.0f;

			// Store the data

			wd.wheel = w;
			m_wheelData[i] = wd;
			}

		// Configure WheelColliders

		foreach (Wheel wheel in wheels)
			{
			SetupWheelCollider(wheel.wheelCollider);
			UpdateWheelCollider(wheel.wheelCollider);

			// Ensure all wheels for any rigidbody are properly set up
			wheel.wheelCollider.ConfigureVehicleSubsteps(1000.0f, 1, 1);
			}

		// Initialize other data

		m_lastImpactedMaterial = new PhysicMaterial();	// A new reference to ensure cache missmatch at the first query
		}


	void Update ()
		{
		// Single Update step.
		// It takes place after the single fixed update step is completed.
		//
		// When paused, a single time step here should use Time.fixedDeltaTime instead of Time.deltaTime.

		if (paused)
			{
			if ((m_singleFixedStep || !m_singleUpdateStep)) return;
			m_singleUpdateStep = false;
			}

		// Update the visual wheels

		if (wheelUpdateRate == UpdateRate.Disabled)
			{
			ComputeSteerAngle();

			foreach (WheelData wd in m_wheelData)
				{
				UpdateSteering(wd);
				}
			}
		else
		if (wheelUpdateRate == UpdateRate.OnUpdate || wheelPositionMode == PositionMode.Accurate)
			{
			bool needDisableColliders =	m_rigidbody.interpolation != RigidbodyInterpolation.None
				&& wheelPositionMode == PositionMode.Accurate;

			if (needDisableColliders)
				DisableCollidersRaycast();

			ComputeSteerAngle();

			foreach (WheelData wd in m_wheelData)
				{
				UpdateSteering(wd);
				UpdateTransform(wd, paused? Time.fixedDeltaTime : Time.deltaTime);
				}

			if (needDisableColliders)
				EnableCollidersRaycast();
			}

		// Drag state is smoothly faded to zero. It gets raised/modified from the drag contacts.

		if (processContacts)
			{
			UpdateDragState(Vector3.zero, Vector3.zero, m_localDragHardness);
			// debugText = string.Format("Drag Pos: {0}  Drag Velocity: {1,5:0.00}  Drag Friction: {2,4:0.00}", localDragPosition, localDragVelocity.magnitude, localDragFriction);
			}
		}


	void FixedUpdate ()
		{
		// If paused, allow to perform a single time step

		if (paused)
			{
			if (!m_singleFixedStep) return;
			m_singleFixedStep = false;
			}

		// Keep center of mass up to date

		if (!disallowRuntimeChanges)
			ConfigureCenterOfMass();

		// Ensure input values within range

		throttleInput = Mathf.Clamp (throttleInput, -1.0f, +1.0f);
		brakeInput = Mathf.Clamp01(brakeInput);
		handbrakeInput = Mathf.Clamp01(handbrakeInput);

		// Calculate the velocity of the vehicle

		if (m_referenceCandidateCount > m_wheelData.Length/2)
			m_referenceBody = m_referenceCandidate;

		Vector3 currentVelocity = m_rigidbody.velocity;
		if (m_referenceBody != null) currentVelocity -= m_referenceBody.velocity;

		m_speed = Vector3.Dot(currentVelocity, m_transform.forward);
		m_speedAngle = Vector3.Angle(currentVelocity, m_transform.forward) * Mathf.Sign(Vector3.Dot(currentVelocity, m_transform.right));

		// Prepare common data

		float referenceDownforce =
			computeExtendedTireData? (m_rigidbody.mass * Physics.gravity.magnitude) / m_wheelData.Length : 1.0f;

		// Apply wheel physics

		bool needUpdateVisuals =
			wheelUpdateRate == UpdateRate.OnFixedUpdate && wheelPositionMode == PositionMode.Fast;

		int groundedWheels = 0;
		m_referenceCandidateCount = 0;

		if (needUpdateVisuals)
			ComputeSteerAngle();

		foreach (WheelData wd in m_wheelData)
			{
			if (!disallowRuntimeChanges)
				UpdateWheelCollider(wd.collider);

			if (needUpdateVisuals)
				UpdateSteering(wd);

			UpdateSuspension(wd);
			UpdateLocalFrame(wd);
			UpdateGroundMaterial(wd);

			ComputeTireForces(wd);
			ApplyTireForces(wd);

			UpdateWheelSleep(wd);

			// Update visual wheel object

			if (needUpdateVisuals)
				UpdateTransform(wd, Time.deltaTime);

			if (wd.grounded) groundedWheels++;

			// Calculate extended tire data

			if (computeExtendedTireData)
				ComputeExtendedTireData(wd, referenceDownforce);
			}

		// Apply aerodynamic properties

		float sqrVelocity = m_rigidbody.velocity.sqrMagnitude;
		Vector3 normalizedVelocity = m_rigidbody.velocity.normalized;
		float forwardVelocityFactor = Vector3.Dot(m_transform.forward, normalizedVelocity);

		Vector3 dragForce = -aeroDrag * sqrVelocity * normalizedVelocity;
		Vector3 loadForce = -aeroDownforce * sqrVelocity * forwardVelocityFactor * m_transform.up;

		Vector3 aeroAppPoint = m_transform.TransformPoint(new Vector3(0.0f,
			m_rigidbody.centerOfMass.y,
			Mathf.Lerp(m_vehicleFrame.rearPosition, m_vehicleFrame.frontPosition, aeroBalance)));

		m_rigidbody.AddForceAtPosition(dragForce, aeroAppPoint);
		if (groundedWheels > 0) m_rigidbody.AddForceAtPosition(loadForce, aeroAppPoint);

		// CommonTools.DrawCrossMark (aeroAppPoint, m_transform.forward, m_transform.right, m_transform.up, Color.magenta);
		// debugText = string.Format("AeroDrag: {0,6:0.} AeroForce: {1,6:0.}", dragForce.magnitude, loadForce.magnitude);

		// Handle impacts

		if (processContacts)
			HandleImpacts();
		}


	//----------------------------------------------------------------------------------------------


	// Public Pause feature
	//
	// Equivalent to disabling the component but the internal state is preserved.
	// Note that only pauses the internal updates. The Physics rigidbody and friction-less
	// WheelColliders keep operating normally.
	//
	// Pause is currently designed to be used by other components (replay, pause vehicle...)


	bool m_paused = false;
	bool m_singleFixedStep = false;
	bool m_singleUpdateStep = false;


	public bool paused
		{
		get
			{
			return m_paused;
			}

		set
			{
			if (m_paused != value)
				{
				m_singleFixedStep = false;
				m_singleUpdateStep = false;
				m_paused = value;
				}
			}
		}


	public void SingleStep ()
		{
		// Both steps (fixed / update) must have been completed before initiating
		// a new single step.

		if (paused && m_singleFixedStep == false && m_singleUpdateStep == false)
			{
			m_singleFixedStep = true;
			m_singleUpdateStep = true;
			}
		}


	//----------------------------------------------------------------------------------------------


	void ComputeSteerAngle ()
		{
		float inputSteerAngle = maxSteerAngle * steerInput;

		float speedFactor = Mathf.InverseLerp(0.1f, 3.0f, m_speed);

		if (steeringLimit)
			{
			float forwardSpeed = m_speed * steeringLimitRatio * speedFactor;
			float maxEspAngle = Mathf.Asin(Mathf.Clamp01(3.0f / forwardSpeed)) * Mathf.Rad2Deg;
			float steerAngleLimit = Mathf.Min(maxSteerAngle, Mathf.Max(maxEspAngle, Mathf.Abs(m_speedAngle)));
			inputSteerAngle = Mathf.Clamp(inputSteerAngle, -steerAngleLimit, +steerAngleLimit);
			}

		float assistedSteerAngle = 0.0f;
		if (steeringAssist)
			assistedSteerAngle = m_speedAngle * steeringAssistRatio * speedFactor * Mathf.InverseLerp(2.0f, 3.0f, Mathf.Abs(m_speedAngle));

		m_steerAngle = Mathf.Clamp(inputSteerAngle + assistedSteerAngle, -maxSteerAngle, +maxSteerAngle);
		}


	void UpdateSteering (WheelData wd)
		{
		if (wd.wheel.steer)
			{
			wd.steerAngle = m_steerAngle;
			if (wd.positionRatio < 0.5f) wd.steerAngle = -wd.steerAngle;
			}
		else
			{
			wd.steerAngle = 0.0f;
			}

		wd.collider.steerAngle = disableSteerAngleCorrection? wd.steerAngle : FixSteerAngle(wd, wd.steerAngle);
		}


	float FixSteerAngle (WheelData wd, float inputSteerAngle)
		{
		// World-space forward vector for the wheel in the desired steer angle

		Quaternion steerRot = Quaternion.AngleAxis(inputSteerAngle, wd.transform.up);
		Vector3 wheelForward = steerRot * wd.transform.forward;

		// Stupid PhysX Vehicle SDK assumes all wheels point in the same direction as the rigidbody.
		//
		// Step 1:	Project the forward direction into the rigidbody's XZ plane.
		// 			This is the vector we want our wheel to point to as seen from the rigidbody.

		Vector3 rbWheelForward = wheelForward - Vector3.Project(wheelForward, m_transform.up);

		// Step 2:	Calculate the final steer angle to feed PhysX with.

		return Vector3.Angle(m_transform.forward, rbWheelForward) * Mathf.Sign(Vector3.Dot(m_transform.right, rbWheelForward));
		}


 	void UpdateSuspension (WheelData wd)
		{
		// Retrieve the wheel's contact point

		wd.grounded = wd.collider.GetGroundHit(out wd.hit);
		wd.origin = wd.transform.TransformPoint(wd.collider.center);
		wd.hit.point += m_rigidbody.velocity * Time.deltaTime;

		// Suspension compression and downforce

		if (wd.grounded)
			{
			wd.suspensionCompression = 1.0f - (-wd.transform.InverseTransformPoint(wd.hit.point).y - wd.collider.radius) / wd.collider.suspensionDistance;
			if (wd.hit.force < 0.0f) wd.hit.force = 0.0f;
			wd.downforce = wd.hit.force;
			}
		else
			{
			wd.suspensionCompression = 0.0f;
			wd.downforce = 0.0f;
			}
		}


	void UpdateLocalFrame (WheelData wd)
		{
		// Speed of the wheel rig

		if (!wd.grounded)
			{
			// Ensure continuity even when the wheel is lifted

			wd.hit.point = wd.origin - wd.transform.up * (wd.collider.suspensionDistance + wd.collider.radius);
			wd.hit.normal = wd.transform.up;
			wd.hit.collider = null;
			}

		Vector3 wheelV = m_rigidbody.GetPointVelocity(wd.hit.point);

		if (wd.hit.collider != null)
			{
			Rigidbody rb = wd.hit.collider.attachedRigidbody;
			if (rb != null)
				{
				wheelV -= rb.GetPointVelocity(wd.hit.point);
				}

			// Contribute to change the reference body if the touching
			// rigidbody is different to the actual reference.

			if (rb != m_referenceBody)
				{
				m_referenceCandidate = rb;
				m_referenceCandidateCount++;
				}
			}

		wd.velocity = wheelV - Vector3.Project(wheelV, wd.hit.normal);
		wd.localVelocity.y = Vector3.Dot(wd.hit.forwardDir, wd.velocity);
		wd.localVelocity.x = Vector3.Dot(wd.hit.sidewaysDir, wd.velocity);

		// Forces related to the wheel rig

		if (!wd.grounded)
			{
			wd.localRigForce = Vector2.zero;
			return;
			}

		Vector2 localSurfaceForce;

		float surfaceForceRatio = Mathf.InverseLerp(1.0f, 0.25f, wd.velocity.sqrMagnitude);
		if (surfaceForceRatio > 0.0f)
			{
			Vector3 surfaceForce;

			float upNormal = Vector3.Dot(Vector3.up, wd.hit.normal);
			if (upNormal > 0.000001f)
				{
				Vector3 downForceUp = Vector3.up * wd.hit.force / upNormal;
				surfaceForce = downForceUp - Vector3.Project(downForceUp, wd.hit.normal);
				}
			else
				{
				surfaceForce = Vector3.up * 100000.0f;
				}

			localSurfaceForce.y = Vector3.Dot(wd.hit.forwardDir, surfaceForce);
			localSurfaceForce.x = Vector3.Dot(wd.hit.sidewaysDir, surfaceForce);
			localSurfaceForce *= surfaceForceRatio;
			}
		else
			{
			localSurfaceForce = Vector2.zero;
			}

		float estimatedSprungMass = Mathf.Clamp(wd.hit.force / -Physics.gravity.y, 0.0f, wd.collider.sprungMass) * 0.5f;
		Vector2 localVelocityForce = -estimatedSprungMass * wd.localVelocity / Time.deltaTime;

		wd.localRigForce = localVelocityForce + localSurfaceForce;
		}


	void UpdateGroundMaterial (WheelData wd)
		{
		if (wd.grounded)
			UpdateGroundMaterialCached(wd.hit.collider.sharedMaterial, ref wd.lastPhysicMaterial, ref wd.groundMaterial);
		}


	void ComputeTireForces (WheelData wd)
		{
		// Throttle for this wheel

		float wheelThrottleInput = wd.wheel.drive? throttleInput : 0.0f;
		float wheelMaxDriveSlip = maxDriveSlip;

		if (Mathf.Sign(wheelThrottleInput) != Mathf.Sign(wd.localVelocity.y))
			wheelMaxDriveSlip -= wd.localVelocity.y * Mathf.Sign(wheelThrottleInput);

		// Calculate the combined brake out of brake and handbrake for this wheel

		float wheelBrakeInput = 0.0f;
		float wheelBrakeRatio = 0.0f;
		float wheelBrakeSlip = 0.0f;

		if (wd.wheel.brake && wd.wheel.handbrake)
			{
			wheelBrakeInput = Mathf.Max(brakeInput, handbrakeInput);

			if (handbrakeInput >= brakeInput)
				ComputeBrakeValues(wd, handbrakeMode, maxHandbrakeSlip, maxHandbrakeRatio, out wheelBrakeSlip, out wheelBrakeRatio);
			else
				ComputeBrakeValues(wd, brakeMode, maxBrakeSlip, maxBrakeRatio, out wheelBrakeSlip, out wheelBrakeRatio);
			}
		else
		if (wd.wheel.brake)
			{
			wheelBrakeInput = brakeInput;
			ComputeBrakeValues(wd, brakeMode, maxBrakeSlip, maxBrakeRatio, out wheelBrakeSlip, out wheelBrakeRatio);
			}
		else
		if (wd.wheel.handbrake)
			{
			wheelBrakeInput = handbrakeInput;
			ComputeBrakeValues(wd, handbrakeMode, maxHandbrakeSlip, maxHandbrakeRatio, out wheelBrakeSlip, out wheelBrakeRatio);
			}

		// Combine throttle and brake inputs. There can be only one.
		// (Not really - EVP uses this simplication. Vehicle Physics Pro (VPP) combines
		// throttle and brake in the physically correct way, which is WAY more complex)

		float absThrottleInput = Mathf.Abs(wheelThrottleInput);
		float combinedInput = -rollingResistance + absThrottleInput * (1.0f + rollingResistance) - wheelBrakeInput * (1.0f - rollingResistance);

		if (combinedInput >= 0)
			{
			wd.finalInput = combinedInput * Mathf.Sign(wheelThrottleInput);
			wd.isBraking = false;
			}
		else
			{
			wd.finalInput = -combinedInput;
			wd.isBraking = true;
			}

		// Calculate demanded force coming from the wheel's axle

		float demandedForce;

		if (wd.isBraking)
			{
			demandedForce = wd.finalInput * GetRampBalancedValue(maxBrakeForce, brakeBalance, wd.positionRatio);
			}
		else
			{
			float balancedDriveForce = GetRampBalancedValue(maxDriveForce, driveBalance, wd.positionRatio);
			demandedForce = ComputeDriveForce(wd.finalInput * balancedDriveForce, balancedDriveForce, wd.grounded);
			}

		// ABS and TC limits

		if (wd.grounded)
			{
			if (tractionControl)
				wheelMaxDriveSlip = Mathf.Lerp(wheelMaxDriveSlip, 0.1f, tractionControlRatio);

			if (brakeAssist && brakeInput > handbrakeInput)
				{
				wheelBrakeSlip = Mathf.Lerp(wheelBrakeSlip, 0.1f, brakeAssistRatio);
				wheelBrakeRatio = Mathf.Lerp(wheelBrakeRatio, wheelBrakeRatio * 0.1f, brakeAssistRatio);
				}
			}

		// Calculate tire forces

		if (wd.grounded)
			{
			wd.tireSlip.x = wd.localVelocity.x;
			wd.tireSlip.y = wd.localVelocity.y - wd.angularVelocity * wd.collider.radius;

			// Get the ground properties

			float groundGrip;
			float groundDrag;

			if (wd.groundMaterial != null)
				{
				groundGrip = wd.groundMaterial.grip;
				groundDrag = wd.groundMaterial.drag;
				}
			else
				{
				groundGrip = defaultGroundGrip;
				groundDrag = defaultGroundDrag;
				}

			// Calculate the total tire force available

			float balancedFriction = GetBalancedValue(tireFriction, tireFrictionBalance, wd.positionRatio);
			float forceMagnitude = balancedFriction * wd.downforce * groundGrip;

			// Ensure there's longitudinal slip enough for the demanded longitudinal force

			float minSlipY;

			if (wd.isBraking)
				{
				float wheelMaxBrakeSlip = Mathf.Max(Mathf.Abs(wd.localVelocity.y * wheelBrakeRatio),  wheelBrakeSlip);
				minSlipY = Mathf.Clamp(Mathf.Abs(demandedForce * wd.tireSlip.x) / forceMagnitude, 0.0f, wheelMaxBrakeSlip);
				}
			else
				{
				minSlipY = Mathf.Min(Mathf.Abs(demandedForce * wd.tireSlip.x) / forceMagnitude, wheelMaxDriveSlip);
				if (demandedForce != 0.0f && minSlipY < 0.1f) minSlipY = 0.1f;
				}

			if (Mathf.Abs(wd.tireSlip.y) < minSlipY) wd.tireSlip.y = minSlipY * Mathf.Sign(wd.tireSlip.y);

			// Compute combined tire forces

			wd.rawTireForce = -forceMagnitude * wd.tireSlip.normalized;
			wd.rawTireForce.x = Mathf.Abs(wd.rawTireForce.x);
			wd.rawTireForce.y = Mathf.Abs(wd.rawTireForce.y);

			// Sideways force

			wd.tireForce.x = Mathf.Clamp(wd.localRigForce.x, -wd.rawTireForce.x, +wd.rawTireForce.x);

			// Forward force

			if (wd.isBraking)
				{
				float maxFy = Mathf.Min(wd.rawTireForce.y, demandedForce);
				wd.tireForce.y = Mathf.Clamp(wd.localRigForce.y, -maxFy, +maxFy);
				}
			else
				{
				wd.tireForce.y = Mathf.Clamp(demandedForce, -wd.rawTireForce.y, +wd.rawTireForce.y);
				}

			// Drag force as for the surface resistance

			wd.dragForce = -(forceMagnitude * wd.localVelocity.magnitude * groundDrag * 0.001f) * wd.localVelocity;
			}
		else
			{
			wd.tireSlip = Vector2.zero;
			wd.tireForce = Vector2.zero;
			wd.dragForce = Vector2.zero;
			}

		// Compute angular velocity for the next step

		float slipToForce = wd.isBraking? brakeForceToMaxSlip : driveForceToMaxSlip;
		float slipRatio = Mathf.Clamp01((Mathf.Abs(demandedForce) - Mathf.Abs(wd.tireForce.y)) / slipToForce);

		float slip;

		if (wd.isBraking)
			slip = Mathf.Clamp(-slipRatio * wd.localVelocity.y * wheelBrakeRatio, -wheelBrakeSlip, wheelBrakeSlip);
		else
			slip = slipRatio * wheelMaxDriveSlip * Mathf.Sign(demandedForce);

		wd.angularVelocity = (wd.localVelocity.y + slip) / wd.collider.radius;
		}


	void ApplyTireForces (WheelData wd)
		{
		if (wd.grounded)
			{
			if (!disallowRuntimeChanges)
				wd.forceDistance = GetWheelForceDistance(wd.collider);

			Vector3 forwardForce = wd.hit.forwardDir * (wd.tireForce.y + wd.dragForce.y);
			Vector3 sidewaysForce = wd.hit.sidewaysDir * (wd.tireForce.x + wd.dragForce.x);
			Vector3 sidewaysForcePoint = GetSidewaysForceAppPoint(wd, wd.hit.point);

			m_rigidbody.AddForceAtPosition(forwardForce, wd.hit.point);
			m_rigidbody.AddForceAtPosition(sidewaysForce, sidewaysForcePoint);

			Rigidbody otherRb = wd.hit.collider.attachedRigidbody;
			if (otherRb != null && !otherRb.isKinematic)
				{
				otherRb.AddForceAtPosition(-forwardForce, wd.hit.point);
				otherRb.AddForceAtPosition(-sidewaysForce, sidewaysForcePoint);
				}
			}
		}

	// Methods for calculating tire data


	// Application point for the sideways force

	public Vector3 GetSidewaysForceAppPoint (WheelData wd, Vector3 contactPoint)
		{
		Vector3 sidewaysForcePoint = contactPoint + wd.transform.up * antiRoll * wd.forceDistance;

		if (wd.wheel.steer && wd.steerAngle != 0.0f && Mathf.Sign(wd.steerAngle) != Mathf.Sign(wd.tireSlip.x))
			sidewaysForcePoint += wd.transform.forward * (m_vehicleFrame.frontPosition - m_vehicleFrame.rearPosition) * (handlingBias - 0.5f);

		return sidewaysForcePoint;
		}


	// Slip angle based on velocity

	static float ComputeSlipAngle (Vector2 localVelocity)
		{
		return localVelocity.magnitude > 0.01f
			? Mathf.Atan2(localVelocity.x, Mathf.Abs(localVelocity.y))
			: 0.0f;
		}


	// Combined slip value. It's the magnitude of tireSlip, but the x component being weighted with
	// the velocity-based slip angle. The implementation is equivalent to:
	//
	// float combinedSlip = new Vector2(tireSlip.x * Mathf.Sin(GetSlipAngle()), tireSlip.y).magnitude

	static float ComputeCombinedSlip (Vector2 localVelocity, Vector2 tireSlip)
		{
		float h = localVelocity.magnitude;

		if (h > 0.01f)
			{
			float sx = tireSlip.x * localVelocity.x / h;
			float sy = tireSlip.y;
			return Mathf.Sqrt(sx*sx + sy*sy);
			}
		else
			{
			return tireSlip.magnitude;
			}
		}


	void ComputeExtendedTireData (WheelData wd, float referenceDownforce)
		{
		wd.combinedTireSlip = ComputeCombinedSlip(wd.localVelocity, wd.tireSlip);
		wd.downforceRatio = wd.hit.force / referenceDownforce;
		}


	// Calculate current maximum force according to speed


	float ComputeDriveForce (float demandedForce, float maxForce, bool grounded)
		{
		float absSpeed = Mathf.Abs(m_speed);
		float speedLimit = m_speed >= 0.0f? maxSpeedForward : maxSpeedReverse;

		if (absSpeed < speedLimit)
			{
			if (m_speed < 0.0f && demandedForce > 0.0f || m_speed > 0.0f && demandedForce < 0.0f)
				{
				// Do not clamp the drive force that opposes the speed direction
				}
			else
				{
				maxForce *= CommonTools.BiasedLerp(1.0f - absSpeed/speedLimit, forceCurveShape, m_forceBiasCtx);
				}

			return Mathf.Clamp(demandedForce, -maxForce, +maxForce);
			}
		else
			{
			float opposingForce = maxForce * Mathf.Max(1.0f - absSpeed/speedLimit, -1.0f) * Mathf.Sign(m_speed);

			if (m_speed < 0.0f && demandedForce > 0.0f || m_speed > 0.0f && demandedForce < 0.0f)
				{
				// Drive force that opposes the speed direction is added to the actual resistive speed

				opposingForce = Mathf.Clamp(opposingForce + demandedForce, -maxForce, +maxForce);
				}

			return opposingForce;
			}
		}


	// Calculate brake ratio and slip based on the current brake method


	void ComputeBrakeValues (WheelData wd, BrakeMode mode, float maxSlip, float maxRatio, out float brakeSlip, out float brakeRatio)
		{
		if (mode == BrakeMode.Slip)
			{
			brakeSlip = maxSlip;
			brakeRatio = 1.0f;
			}
		else
			{
			brakeSlip = Mathf.Abs(wd.localVelocity.y);
			brakeRatio = maxRatio;
			}
		}


	// Set the visual transform for the wheel


	void UpdateTransform (WheelData wd, float deltaTime)
		{
		if (wd.wheel.wheelTransform != null || wd.wheel.caliperTransform != null)
			{
			// Disabled wheels get hidden

			if (!wd.collider.enabled || !wd.collider.gameObject.activeInHierarchy)
				{
				if (wd.wheel.wheelTransform) wd.wheel.wheelTransform.gameObject.SetActive(false);
				if (wd.wheel.caliperTransform) wd.wheel.caliperTransform.gameObject.SetActive(false);
				return;
				}

			// Wheel spin

			float deltaPos = wd.angularVelocity * deltaTime;
			if (invertVisualWheelSpinDirection) deltaPos = -deltaPos;

			wd.angularPosition = (wd.angularPosition + deltaPos) % (Mathf.PI*2.0f);

			// Wheel position

			float elongation;

			if (wheelPositionMode == PositionMode.Fast)
				{
				elongation = wd.collider.suspensionDistance * (1.0f - wd.suspensionCompression) + wd.collider.radius * 0.05f;
				wd.rayHit.point = wd.hit.point;
				wd.rayHit.normal = wd.hit.normal;
				}
			else
				{
				#if UNITY_52_OR_GREATER
				bool collided = Physics.Raycast(wd.origin, -wd.transform.up, out wd.rayHit, (wd.collider.suspensionDistance + wd.collider.radius), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
				#else
				bool collided = Physics.Raycast(wd.origin, -wd.transform.up, out wd.rayHit, (wd.collider.suspensionDistance + wd.collider.radius), Physics.DefaultRaycastLayers);
				#endif

				if (collided)
					{
					// If these layers are intended to ignore collisions then just use the actual WheelHit information

					if (Physics.GetIgnoreLayerCollision(wd.collider.gameObject.layer, wd.rayHit.collider.gameObject.layer))
						{
						elongation = wd.collider.suspensionDistance * (1.0f - wd.suspensionCompression) + wd.collider.radius * 0.05f;
						wd.rayHit.point = wd.hit.point;
						wd.rayHit.normal = wd.hit.normal;
						}
					else
						{
						elongation = wd.rayHit.distance - wd.collider.radius * 0.95f;
						}
					}
				else
					{
					elongation = wd.collider.suspensionDistance + wd.collider.radius * 0.05f;
					}
				}

			Vector3 wheelPosition = wd.transform.position - wd.transform.up * elongation;

			// Caliper transform

			if (wd.wheel.caliperTransform != null)
				{
				wd.wheel.caliperTransform.gameObject.SetActive(true);

				wd.wheel.caliperTransform.position = wheelPosition;

				// Rotation due to steering (Y)

				wd.wheel.caliperTransform.rotation = wd.transform.rotation * Quaternion.Euler(0.0f, wd.steerAngle, 0.0f);
				}

			if (wd.wheel.wheelTransform != null)
				{
				wd.wheel.wheelTransform.gameObject.SetActive(true);

				if (wd.isWheelChildOfCaliper)
					{
					// Wheel is child of caliper. Only local wheel spin is required.

					wd.wheel.wheelTransform.localRotation = Quaternion.Euler(wd.angularPosition * Mathf.Rad2Deg, 0.0f, 0.0f);
					}
				else
					{
					// Wheel is not child of caliper. Apply full position & rotation

					wd.wheel.wheelTransform.position = wheelPosition;
					wd.wheel.wheelTransform.rotation = wd.transform.rotation * Quaternion.Euler(wd.angularPosition * Mathf.Rad2Deg, wd.steerAngle, 0.0f);
					}
				}
			}
		else
			{
			wd.rayHit.point = wd.hit.point;
			wd.rayHit.normal = wd.hit.normal;
			}

		}


	// Updates a ground material reference based on the physics material assigned to a collider
	// using a cached reference for the physics material. This way the ground material manager
	// is queried only when the physic material changes.

	void UpdateGroundMaterialCached (PhysicMaterial colliderMaterial, ref PhysicMaterial cachedMaterial, ref GroundMaterial groundMaterial)
		{
		if (m_groundMaterialManager != null)
			{
			// Query the ground material (slow, table look-up) only when the physic material changes.
			// Otherwise keep the actual known material.

			if (colliderMaterial != cachedMaterial)
				{
				cachedMaterial = colliderMaterial;
				groundMaterial = m_groundMaterialManager.GetGroundMaterial(colliderMaterial);
				}
			}

		// Don't do anything if no GroundMaterialManager is present.
		// Ground materials may still be supplied externally via wheelData[].groundMaterial
		}


	//----------------------------------------------------------------------------------------------


	public void ResetVehicle ()
		{
		Vector3 eulerAngles = transform.localEulerAngles;
		m_rigidbody.MoveRotation(Quaternion.Euler(0, eulerAngles.y, 0));
		m_rigidbody.MovePosition(m_rigidbody.position + Vector3.up * 1.6f);

		m_rigidbody.velocity = Vector3.zero;
		m_rigidbody.angularVelocity = Vector3.zero;
		}


	//----------------------------------------------------------------------------------------------

	// Methods for dealing with colliders


	Collider[] m_colliders = new Collider[0];
	int[] m_colLayers = new int[0];


	void FindColliders ()
		{
		Collider[] originalColliders = GetComponentsInChildren<Collider>(true);
		List<Collider> filteredColliders = new List<Collider>();

		// Keep non-trigger and non-wheel colliders only

		foreach (Collider col in originalColliders)
			{
			if (!col.isTrigger && !(col is WheelCollider))
				filteredColliders.Add(col);
			}

		m_colliders = filteredColliders.ToArray();
		m_colLayers = new int[m_colliders.Length];
		}


	void DisableCollidersRaycast ()
		{
		for (int i=0, c=m_colliders.Length; i<c; i++)
			{
			GameObject go = m_colliders[i].gameObject;
			m_colLayers[i] = go.layer;
			go.layer = 2;
			}
		}


	void EnableCollidersRaycast ()
		{
		for (int i=0, c=m_colliders.Length; i<c; i++)
			m_colliders[i].gameObject.layer = m_colLayers[i];
		}


	public Vector3 RaycastOthers (Vector3 from, Vector3 to, int layerMask = Physics.DefaultRaycastLayers)
		{
		Vector3 path = to - from;
		RaycastHit hit;

		DisableCollidersRaycast();
		#if UNITY_52_OR_GREATER
		bool collided = Physics.Raycast(from, path, out hit, path.magnitude, layerMask, QueryTriggerInteraction.Ignore);
		#else
		bool collided = Physics.Raycast(from, path, out hit, path.magnitude, layerMask);
		#endif
		EnableCollidersRaycast();

		return collided? hit.point : to;
		}


	public float SphereRaycastOthers (Vector3 origin, Vector3 direction, float radius, float maxDistance, int layerMask = Physics.DefaultRaycastLayers)
		{
		RaycastHit hit;

		DisableCollidersRaycast();
		#if UNITY_52_OR_GREATER
		bool collided = Physics.SphereCast(origin, radius, direction, out hit, maxDistance, layerMask, QueryTriggerInteraction.Ignore);
		#else
		bool collided = Physics.SphereCast(origin, radius, direction, out hit, maxDistance, layerMask);
		#endif
		EnableCollidersRaycast();

		return collided? hit.distance : maxDistance;
		}


	float GetWheelForceDistance (WheelCollider col)
		{
		return m_rigidbody.centerOfMass.y - m_transform.InverseTransformPoint(col.transform.position).y
			+ col.radius + (1.0f - col.suspensionSpring.targetPosition) * col.suspensionDistance;
		}


	void UpdateWheelCollider (WheelCollider col)
		{
		if (!col.enabled) return;

		JointSpring suspension = col.suspensionSpring;
		float sprungForce = -col.sprungMass * Physics.gravity.y;
		float pos = sprungForce / suspension.spring;
		suspension.targetPosition = Mathf.Clamp01(pos / col.suspensionDistance);

		float minSpringRate = sprungForce / col.suspensionDistance;
		if (suspension.spring < minSpringRate) suspension.spring = minSpringRate;
		col.suspensionSpring = suspension;
		}


	WheelFrictionCurve m_colliderFriction = new WheelFrictionCurve();

	void SetupWheelCollider (WheelCollider col)
		{
		m_colliderFriction.stiffness = 0.0f;
		col.sidewaysFriction = m_colliderFriction;
		col.forwardFriction = m_colliderFriction;
		col.motorTorque = 0.00001f;
		}


	void UpdateWheelSleep (WheelData wd)
		{
		if (wd.localVelocity.magnitude < sleepVelocity
			&& Time.time-m_lastStrongImpactTime > 0.2f
			&& (wd.isBraking && wd.rawTireForce.y >= Mathf.Abs(wd.localRigForce.y)
				&& wd.rawTireForce.x >= Mathf.Abs(wd.localRigForce.x) || m_usesHandbrake && handbrakeInput > 0.1f)
			)
			{
			wd.collider.motorTorque = 0.0f;
			}
		else
			{
			wd.collider.motorTorque = 0.00001f;
			}
		}


	//----------------------------------------------------------------------------------------------

	// Vehicle balancing


	struct VehicleFrame
		{
		public float frontPosition;		// Average forwards position of all "front" wheels
		public float rearPosition;		// Average forwards position of all "rear" wheels
		public float baseHeight;		// Average vertical position of all wheels (center.y)
		public float frontWidth;		// Average semi-axle distance for all "front" wheels
		public float rearWidth;			// Average semi-axle distance for all "rear" wheels

		public float middlePoint;		// Forwards position that separates "front" and "rear" wheels
		}


	VehicleFrame ComputeVehicleFrame ()
		{
		// Compute the middle position

		float middlePoint = 0.0f;
		int middleCount = 0;
		foreach (Wheel w in wheels)
			{
			if (w.wheelCollider != null)
				{
				middlePoint += transform.InverseTransformPoint(w.wheelCollider.transform.TransformPoint(w.wheelCollider.center)).z;
				middleCount++;
				}
			}

		if (middleCount > 0) middlePoint /= middleCount;

		// Compute the front / rear positions and the base height

		float frontPos = 0.0f;
		float frontWidth = 0.0f;
		int frontCount = 0;

		float rearPos = 0.0f;
		float rearWidth = 0.0f;
		int rearCount = 0;

		float baseHeight = 0.0f;
		int baseHeightCount = 0;

		foreach (Wheel w in wheels)
			{
			if (w.wheelCollider != null)
				{
				Vector3 localPos = transform.InverseTransformPoint(w.wheelCollider.transform.TransformPoint(w.wheelCollider.center));

				float wheelPos = localPos.z;
				float axleWidth = Mathf.Abs(localPos.x);

				if (wheelPos >= middlePoint)
					{
					frontPos += wheelPos;
					frontWidth += axleWidth;
					frontCount++;
					}
				else
					{
					rearPos += wheelPos;
					rearWidth += axleWidth;
					rearCount++;
					}

				baseHeight += localPos.y;
				baseHeightCount++;
				}
			}

		if (frontCount > 0)
			{
			frontPos = frontPos / frontCount;
			frontWidth = frontWidth / frontCount;
			}

		if (rearCount > 0)
			{
			rearPos = rearPos / rearCount;
			rearWidth = rearWidth / rearCount;
			}
		else
			{
			rearPos = frontPos;
			rearWidth = frontWidth;
			}

		if (baseHeightCount > 0)
			baseHeight = baseHeight / baseHeightCount;

		// Return the results

		VehicleFrame frame = new VehicleFrame();

		frame.frontPosition = frontPos;
		frame.rearPosition = rearPos;
		frame.baseHeight = baseHeight;
		frame.frontWidth = frontWidth;
		frame.rearWidth = rearWidth;
		frame.middlePoint = middlePoint;

		return frame;
		}


	void ConfigureCenterOfMass ()
		{
		// Checking whether rigidbody.centerOfMass has changed really makes a difference.
		// Otherwise all internal calculations are triggered (inertia tensor, sprung masses...)

		if (centerOfMassMode == CenterOfMassMode.Parametric)
			{
			Vector3 CoM = new Vector3(0.0f,
				m_vehicleFrame.baseHeight + centerOfMassHeightOffset,
				Mathf.Lerp(m_vehicleFrame.rearPosition, m_vehicleFrame.frontPosition, centerOfMassPosition));

			if (m_rigidbody.centerOfMass != CoM)
				m_rigidbody.centerOfMass = CoM;
			}
		else
			{
			if (centerOfMassTransform != null)
				{
				Vector3 CoM =  m_transform.InverseTransformPoint(centerOfMassTransform.position);

				if (m_rigidbody.centerOfMass != CoM)
					m_rigidbody.centerOfMass = CoM;
				}
			}
		}


	// Balanced value (linear cross-faded):
	//
	//		bias	rear	front
	//		0.0		0.0		2.0
	//		0.5		1.0		1.0
	//		1.0		2.0		0.0

	public static float GetBalancedValue (float value, float bias, float positionRatio)
		{
		float frontRatio = bias;
		float rearRatio = 1.0f - bias;

		return value * (positionRatio * frontRatio + (1.0f-positionRatio) * rearRatio) * 2.0f;
		}


	// Ramp balanced value:
	//
	//		bias	rear	front
	//		0.0		0.0		1.0
	//		0.5		1.0		1.0
	//		1.0		1.0		0.0

	public static float GetRampBalancedValue (float value, float bias, float positionRatio)
		{
		float frontRatio = Mathf.Clamp01(2.0f * bias);
		float rearRatio = Mathf.Clamp01(2.0f * (1.0f - bias));

		return value * (positionRatio * frontRatio + (1.0f-positionRatio) * rearRatio);
		}


	//----------------------------------------------------------------------------------------------

	// Contact processing


	// Private data for internal use

	int m_sumImpactCount = 0;
	Vector3 m_sumImpactPosition = Vector3.zero;
	Vector3 m_sumImpactVelocity = Vector3.zero;
	int m_sumImpactHardness = 0;
	float m_lastImpactTime = 0.0f;

	Vector3 m_localDragPosition = Vector3.zero;
	Vector3 m_localDragVelocity = Vector3.zero;
	int m_localDragHardness = 0;

	float m_lastStrongImpactTime = 0.0f;
	PhysicMaterial m_lastImpactedMaterial;
	GroundMaterial m_impactedGroundMaterial = null;


	void OnCollisionEnter (Collision collision)
		{
		// Prevent the wheels to sleep for some time if a strong impact occurs

		if (collision.relativeVelocity.magnitude > 4.0f)
			m_lastStrongImpactTime = Time.time;

		if (processContacts)
			ProcessContacts(collision, true);
		}


	void OnCollisionStay (Collision collision)
		{
		if (processContacts)
			ProcessContacts(collision, false);
		}


	void ProcessContacts (Collision col, bool forceImpact)
		{
		int impactCount = 0;						// All impacts
		Vector3 impactPosition = Vector3.zero;
		Vector3 impactVelocity = Vector3.zero;
		int impactHardness = 0;

		int dragCount = 0;
		Vector3 dragPosition = Vector3.zero;
		Vector3 dragVelocity = Vector3.zero;
		int dragHardness = 0;

		float sqrImpactSpeed = impactMinSpeed*impactMinSpeed;

		// We process all contacts individually and get an impact and/or drag amount out of each one.

		foreach (ContactPoint contact in col.contacts)
			{
			Collider collider = contact.otherCollider;

			// Get the type of the impacted material: hard +1, soft -1

			int hardness = 0;
			UpdateGroundMaterialCached(collider.sharedMaterial, ref m_lastImpactedMaterial, ref m_impactedGroundMaterial);

			if (m_impactedGroundMaterial != null)
				hardness = m_impactedGroundMaterial.surfaceType == GroundMaterial.SurfaceType.Hard? +1 : -1;

			// Calculate the velocity of the body in the contact point with respect to the colliding object

			Vector3 v = m_rigidbody.GetPointVelocity(contact.point);
			if (collider.attachedRigidbody != null)
				v -= collider.attachedRigidbody.GetPointVelocity(contact.point);

			float dragRatio = Vector3.Dot(v, contact.normal);

			// Determine whether this contact is an impact or a drag

			if (dragRatio < -impactThreeshold || forceImpact && col.relativeVelocity.sqrMagnitude > sqrImpactSpeed)
				{
				// Impact

				impactCount++;
				impactPosition += contact.point;
				impactVelocity += col.relativeVelocity;
				impactHardness += hardness;

				if (showCollisionGizmos)
					Debug.DrawLine(contact.point, contact.point + CommonTools.Lin2Log(v), Color.red);
				}
			else if (dragRatio < impactThreeshold)
				{
				// Drag

				dragCount++;
				dragPosition += contact.point;
				dragVelocity += v;
				dragHardness += hardness;

				if (showCollisionGizmos)
					Debug.DrawLine(contact.point, contact.point + CommonTools.Lin2Log(v), Color.cyan);
				}

			// Debug.DrawLine(contact.point, contact.point + CommonTools.Lin2Log(v), Color.Lerp(Color.cyan, Color.red, Mathf.Abs(dragRatio)));
			if (showCollisionGizmos)
				Debug.DrawLine(contact.point, contact.point + contact.normal*0.25f, Color.yellow);
			}

		// Accumulate impact values received.

		if (impactCount > 0)
			{
			float invCount = 1.0f / impactCount;
			impactPosition *= invCount;
			impactVelocity *= invCount;

			m_sumImpactCount++;
			m_sumImpactPosition += m_transform.InverseTransformPoint(impactPosition);
			m_sumImpactVelocity += m_transform.InverseTransformDirection(impactVelocity);
			m_sumImpactHardness += impactHardness;
			}

		// Update the current drag value

		if (dragCount > 0)
			{
			float invCount = 1.0f / dragCount;
			dragPosition *= invCount;
			dragVelocity *= invCount;

			UpdateDragState(m_transform.InverseTransformPoint(dragPosition), m_transform.InverseTransformDirection(dragVelocity), dragHardness);
			}
		}


	// Impact processing

	void HandleImpacts ()
		{
		// Multiple impacts within an impact interval are accumulated and averaged later.

		if (Time.time-m_lastImpactTime >= impactInterval && m_sumImpactCount > 0)
			{
			// Prepare the impact parameters

			float invCount = 1.0f / m_sumImpactCount;

			m_sumImpactPosition *= invCount;
			m_sumImpactVelocity *= invCount;

			// Notify the listeners on the impact

			if (onImpact != null)
				{
				current = this;
				onImpact();
				current = null;
				}

			// debugText = string.Format("Count: {4}  Impact Pos: {0}  Impact Velocity: {1} ({2,5:0.00})  Impact Friction: {3,4:0.00}", localImpactPosition, localImpactVelocity, localImpactVelocity.magnitude, localImpactFriction, m_sumImpactCount);
			if (showCollisionGizmos && localImpactVelocity.sqrMagnitude > 0.001f)
				Debug.DrawLine(transform.TransformPoint(localImpactPosition), transform.TransformPoint(localImpactPosition) + CommonTools.Lin2Log(transform.TransformDirection(localImpactVelocity)), Color.red, 0.2f, false);

			// Reset impact data

			m_sumImpactCount = 0;
			m_sumImpactPosition = Vector3.zero;
			m_sumImpactVelocity = Vector3.zero;
			m_sumImpactHardness = 0;

			m_lastImpactTime = Time.time + impactInterval * UnityEngine.Random.Range(-impactIntervalRandom, impactIntervalRandom);	// Add a random variation for avoiding regularities
			}
		}


	// Drag processing
	// The values come from OnCollisionEnter/Stay so the actual drag value is updated accordingly.
	//
	// This function is invoked from both OnCollision (increase the drag value) and Update
	// (smoothly decrease the value to zero).

	void UpdateDragState (Vector3 dragPosition, Vector3 dragVelocity, int dragHardness)
		{
		if (dragVelocity.sqrMagnitude > 0.001f)
			{
			m_localDragPosition = Vector3.Lerp(m_localDragPosition, dragPosition, 10.0f * Time.deltaTime);
			m_localDragVelocity = Vector3.Lerp(m_localDragVelocity, dragVelocity, 20.0f * Time.deltaTime);
			m_localDragHardness = dragHardness;
			}
		else
			{
			m_localDragVelocity = Vector3.Lerp(m_localDragVelocity, Vector3.zero, 10.0f * Time.deltaTime);
			}

		if (showCollisionGizmos && localDragVelocity.sqrMagnitude > 0.001f)
			Debug.DrawLine(transform.TransformPoint(localDragPosition), transform.TransformPoint(localDragPosition) + CommonTools.Lin2Log(transform.TransformDirection(localDragVelocity)), Color.cyan, 0.05f, false);
		}


	//----------------------------------------------------------------------------------------------


	[ContextMenu("Adjust WheelColliders to their meshes")]
	void AdjustWheelColliders ()
		{
		foreach (Wheel wheel in wheels)
			{
			if (wheel.wheelCollider != null)
				AdjustColliderToWheelMesh(wheel.wheelCollider, wheel.wheelTransform);
			}
		}


	static void AdjustColliderToWheelMesh (WheelCollider wheelCollider, Transform wheelTransform)
		{
		// Adjust position and rotation

		if (wheelTransform == null)
			{
			Debug.LogError(wheelCollider.gameObject.name + ": A Wheel transform is required");
			return;
			}

		wheelCollider.transform.position = wheelTransform.position + wheelTransform.up * wheelCollider.suspensionDistance * 0.5f;
		wheelCollider.transform.rotation = wheelTransform.rotation;

		// Adjust radius

		MeshFilter[] meshFilters = wheelTransform.GetComponentsInChildren<MeshFilter>();
		if (meshFilters == null || meshFilters.Length == 0)
			{
			Debug.LogWarning(wheelTransform.gameObject.name + ": Couldn't calculate radius. There are no meshes in the Wheel transform or its children");
			return;
			}

		// Calculate the bounds of the meshes contained in the Wheel transform

		Bounds bounds = GetScaledBounds(meshFilters[0]);

		for (int i=1, c=meshFilters.Length; i<c; i++)
			{
			Bounds meshBounds = GetScaledBounds(meshFilters[i]);
			bounds.Encapsulate(meshBounds.min);
			bounds.Encapsulate(meshBounds.max);
			}

		// If this is a correct round wheel then extents for y and z should be approximately the same.

		if (Mathf.Abs(bounds.extents.y-bounds.extents.z) > 0.01f)
			Debug.LogWarning(wheelTransform.gameObject.name + ": The Wheel mesh might not be a correct wheel. The calculated radius is different along forward and vertical axis.");

		wheelCollider.radius = bounds.extents.y;
		}


	static Bounds GetScaledBounds (MeshFilter meshFilter)
		{
		Bounds bounds = meshFilter.sharedMesh.bounds;
		Vector3 scale = meshFilter.transform.lossyScale;
		bounds.max = Vector3.Scale(bounds.max, scale);
		bounds.min = Vector3.Scale(bounds.min, scale);
		return bounds;
		}


	[ContextMenu("Convert Center of Mass from Transform to Parametric")]
	void FromTransformToParametricCoM()
		{
		if (centerOfMassTransform != null)
			{
			VehicleFrame frame = ComputeVehicleFrame();

			Vector3 transformCom = transform.InverseTransformPoint(centerOfMassTransform.position);
			centerOfMassPosition = Mathf.InverseLerp(frame.rearPosition, frame.frontPosition, transformCom.z);
			centerOfMassHeightOffset = transformCom.y - frame.baseHeight;
			centerOfMassMode = CenterOfMassMode.Parametric;
			}
		}


	//----------------------------------------------------------------------------------------------


	#if UNITY_EDITOR


	public void OnDrawGizmos ()
		{
		if (!enabled) return;

		VehicleFrame frame = ComputeVehicleFrame();

		// Draw the wheel gizmos

		Color originalColor = UnityEditor.Handles.color;
		UnityEditor.Handles.color = AlphaColor(Color.green, 0.1f);

		foreach (Wheel w in wheels)
			{
			if (w.wheelCollider != null)
				{
				Vector3 basePos = w.wheelCollider.transform.TransformPoint(w.wheelCollider.center);
				UnityEditor.Handles.DrawSolidDisc(basePos, transform.right, w.wheelCollider.radius * 0.2f);
				}
			}

		// Draw the vehicle frame

		UnityEditor.Handles.color = AlphaColor(Color.green, 0.5f);

		Vector3 front = transform.TransformPoint(new Vector3 (0.0f, frame.baseHeight, frame.frontPosition));
		Vector3 middle = transform.TransformPoint(new Vector3 (0.0f, frame.baseHeight, (frame.frontPosition + frame.rearPosition)*0.5f));
		Vector3 rear = transform.TransformPoint(new Vector3 (0.0f, frame.baseHeight, frame.rearPosition));

		UnityEditor.Handles.DrawLine(front, rear);
		UnityEditor.Handles.DrawLine(front - transform.right * frame.frontWidth, front + transform.right * frame.frontWidth);
		UnityEditor.Handles.DrawLine(rear - transform.right * frame.rearWidth, rear + transform.right * frame.rearWidth);

		// float middleWidth = (frame.frontWidth + frame.rearWidth) * 0.5f * 0.25f;
		// UnityEditor.Handles.DrawLine(middle - transform.right * middleWidth, middle + transform.right * middleWidth);

		UnityEditor.Handles.color = AlphaColor(Color.white, 0.05f);
		UnityEditor.Handles.DrawSolidDisc(middle, transform.up, 0.1f);
		UnityEditor.Handles.color = AlphaColor(Color.white, 0.5f);
		UnityEditor.Handles.DrawLine(middle - transform.right * 0.1f, middle + transform.right * 0.1f);

		// Draw tire friction

		float featureWidth = (frame.frontWidth + frame.rearWidth) * 0.5f * 0.25f;
		Vector3 featureDir = transform.right * featureWidth;

		UnityEditor.Handles.color = AlphaColor(Color.Lerp(Color.yellow, Color.red, 0.5f), 0.5f);
		Vector3 frictionPos = Vector3.Lerp(rear, front, tireFrictionBalance);
		UnityEditor.Handles.DrawLine(frictionPos - featureDir, frictionPos + featureDir);

		// Draw brake balance

		UnityEditor.Handles.color = AlphaColor(Color.red, 0.5f);
		Vector3 brakePos = Vector3.Lerp(rear, front, brakeBalance);
		UnityEditor.Handles.DrawLine(brakePos - featureDir, brakePos + featureDir);

		// Draw drive balance

		UnityEditor.Handles.color = AlphaColor(Color.green, 0.5f);
		Vector3 drivePos = Vector3.Lerp(rear, front, driveBalance);
		UnityEditor.Handles.DrawLine(drivePos - featureDir, drivePos + featureDir);
		Vector3 driveForwardDir = transform.forward * 0.05f;
		UnityEditor.Handles.DrawLine(drivePos - featureDir - driveForwardDir, drivePos - featureDir + driveForwardDir);
		UnityEditor.Handles.DrawLine(drivePos + featureDir - driveForwardDir, drivePos + featureDir + driveForwardDir);

		// Draw handling bias

		UnityEditor.Handles.color = AlphaColor(Color.green, 0.5f);
		Vector3 semiLengthDir = (front-middle);

		Vector3 handlingPos = Vector3.Lerp(front - semiLengthDir, front + semiLengthDir, handlingBias);
		UnityEditor.Handles.DrawLine(handlingPos - featureDir, handlingPos + featureDir);
		UnityEditor.Handles.DrawLine(handlingPos + featureDir, front + featureDir);
		UnityEditor.Handles.DrawLine(handlingPos - featureDir, front - featureDir);


		// Draw Center of Mass

		float comPos = Mathf.Lerp(frame.rearPosition, frame.frontPosition, centerOfMassPosition);
		Vector3 localCom = new Vector3(0.0f, frame.baseHeight + centerOfMassHeightOffset, comPos);
		Vector3 CoM = transform.TransformPoint(localCom);

		UnityEditor.Handles.color = AlphaColor(Color.white, 0.8f);
		DrawCrossMarkHandle(CoM, transform.forward, transform.right, transform.up);

		Vector3 comBase = new Vector3(0.0f, frame.baseHeight, comPos);

		UnityEditor.Handles.color = AlphaColor(Color.white, 0.1f);
		UnityEditor.Handles.DrawLine(transform.TransformPoint(comBase), CoM);

		// Draw aerodynamics

		float aeroForwardPos = Mathf.Lerp(frame.rearPosition, frame.frontPosition, aeroBalance);
		Vector3 localAeroPos = new Vector3(0.0f, localCom.y, aeroForwardPos);
		Vector3 aeroPos = transform.TransformPoint(localAeroPos);

		float aeroDragLength = Mathf.Min(Mathf.Abs(aeroDrag), featureWidth);
		float aeroDownforceLength = Mathf.Min(Mathf.Abs(aeroDownforce), 0.1f);

		if (aeroDragLength > 0.000001f || aeroDownforceLength > 0.00001f)
			{
			UnityEditor.Handles.color = AlphaColor(Color.cyan, 0.1f);
			UnityEditor.Handles.DrawLine(aeroPos, CoM);
			}

		UnityEditor.Handles.color = AlphaColor(Color.cyan, 0.8f);
		DrawCrossMarkHandle(transform.TransformPoint(localAeroPos),
			Vector3.zero, transform.right * aeroDragLength, transform.up * aeroDownforceLength, 1.0f);

		// Restore handles color

		UnityEditor.Handles.color = originalColor;
		}


	static Color AlphaColor (Color col, float alpha = 1.0f)
		{
		col.a = alpha;
		return col;
		}


	static void DrawCrossMarkHandle (Vector3 pos, Vector3 forward, Vector3 right, Vector3 up, float length = 0.1f)
		{
		length *= 0.5f;

		Vector3 F = forward * length;
		Vector3 U = up * length;
		Vector3 R = right * length;

		UnityEditor.Handles.DrawLine(pos - F, pos + F);
		UnityEditor.Handles.DrawLine(pos - U, pos + U);
		UnityEditor.Handles.DrawLine(pos - R, pos + R);
		}

	#endif
	}
}
