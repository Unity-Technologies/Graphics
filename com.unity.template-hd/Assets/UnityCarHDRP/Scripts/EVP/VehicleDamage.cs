//------------------------------------------------------------------------------------------------
// Edy's Vehicle Physics
// (c) Angel Garcia "Edy" - Oviedo, Spain
// http://www.edy.es
//------------------------------------------------------------------------------------------------


// Deformable colliders are supported in Unity 5.2, with further backport to 5.1 as patch release.

#if !UNITY_5_0 && !UNITY_5_1
#define EVP_DEFORMABLE_COLLIDERS
#endif


using UnityEngine;

namespace EVP
{

[RequireComponent(typeof(VehicleController))]
public class VehicleDamage : MonoBehaviour
	{
	public MeshFilter[] meshes;
	public MeshCollider[] colliders;
	public Transform[] nodes;

	[Space(5)]
	public float minVelocity = 1.0f;
	public float multiplier = 1.0f;

	[Space(5)]
	public float damageRadius = 1.0f;
	public float maxDisplacement = 0.5f;
	public float maxVertexFracture = 0.1f;

	[Space(5)]
	public float nodeDamageRadius = 0.5f;
	public float maxNodeRotation = 14.0f;
	public float nodeRotationRate = 10.0f;

	[Space(5)]
	public float vertexRepairRate = 0.1f;

	public bool enableRepairKey = true;
	public KeyCode repairKey = KeyCode.R;


	VehicleController m_vehicle;

	Vector3[][] m_originalMeshes;
	Vector3[][] m_originalColliders;
	Vector3[] m_originalNodePositions;
	Quaternion[] m_originalNodeRotations;


	// Expose damage level and reparing state

	bool m_repairing = false;
	float m_meshDamage = 0.0f;
	float m_colliderDamage = 0.0f;
	float m_nodeDamage = 0.0f;

	public bool isRepairing { get { return m_repairing; } }
	public float meshDamage { get { return m_meshDamage; } }
	public float colliderDamage { get { return m_colliderDamage; } }
	public float nodeDamage { get { return m_nodeDamage; } }


	void OnEnable ()
		{
		m_vehicle = GetComponent<VehicleController>();
		m_vehicle.processContacts = true;
		m_vehicle.onImpact += ProcessImpact;

		// Store original vertices of the meshes

		m_originalMeshes = new Vector3[meshes.Length][];
		for (int i = 0; i < meshes.Length; i++)
			{
			Mesh mesh = meshes[i].mesh;

			m_originalMeshes[i] = mesh.vertices;
			mesh.MarkDynamic();
			}

		// Store original vertices of the colliders

		m_originalColliders = new Vector3[colliders.Length][];
		for (int i = 0; i < colliders.Length; i++)
			{
			Mesh mesh = colliders[i].sharedMesh;

			m_originalColliders[i] = mesh.vertices;
			mesh.MarkDynamic();
			}

		// Store original position and rotation of the nodes

		m_originalNodePositions = new Vector3[nodes.Length];
		m_originalNodeRotations = new Quaternion[nodes.Length];

		for (int i = 0; i < nodes.Length; i++)
			{
			m_originalNodePositions[i] = nodes[i].transform.localPosition;
			m_originalNodeRotations[i] = nodes[i].transform.localRotation;
			}

		// Initialize damage levels

		m_repairing = false;
		m_meshDamage = 0.0f;
		m_colliderDamage = 0.0f;
		m_nodeDamage = 0.0f;
		}


	void OnDisable ()
		{
		RestoreMeshes();
		RestoreNodes();
		RestoreColliders();

		m_repairing = false;
		m_meshDamage = 0.0f;
		m_colliderDamage = 0.0f;
		m_nodeDamage = 0.0f;
		}


	void Update ()
		{
		if (enableRepairKey && Input.GetKeyDown(repairKey))
			m_repairing = true;

		ProcessRepair();
		}


	public void Repair ()
		{
		m_repairing = true;
		}


	//----------------------------------------------------------------------------------------------


	void ProcessImpact ()
		{
		Vector3 impactVelocity = Vector3.zero;

		if (m_vehicle.localImpactVelocity.sqrMagnitude > minVelocity * minVelocity)
			impactVelocity = m_vehicle.cachedTransform.TransformDirection(m_vehicle.localImpactVelocity) * multiplier * 0.02f;

		if (impactVelocity.sqrMagnitude > 0.0f)
			{
			Vector3 contactPoint = transform.TransformPoint(m_vehicle.localImpactPosition);

			// Deform the meshes

			for (int i=0, c=meshes.Length; i<c; i++)
				m_meshDamage += DeformMesh(meshes[i].mesh, m_originalMeshes[i], meshes[i].transform, contactPoint, impactVelocity);

			// Deform the colliders

			m_colliderDamage = DeformColliders(contactPoint, impactVelocity);

			// Deform the nodes

			for (int i=0, c=nodes.Length; i<c; i++)
				m_nodeDamage += DeformNode(nodes[i], m_originalNodePositions[i], m_originalNodeRotations[i], contactPoint, impactVelocity * 0.5f);
			}
		}


	float DeformMesh (Mesh mesh, Vector3[] originalMesh, Transform localTransform, Vector3 contactPoint, Vector3 contactVelocity)
		{
		Vector3[] vertices = mesh.vertices;
		float sqrRadius = damageRadius * damageRadius;
		float sqrMaxDeform = maxDisplacement * maxDisplacement;

		Vector3 localContactPoint = localTransform.InverseTransformPoint(contactPoint);
		Vector3 localContactForce = localTransform.InverseTransformDirection(contactVelocity);

		float totalDamage = 0.0f;
		int damagedVertices = 0;

		for (int i=0; i<vertices.Length; i++)
			{
			float dist = (localContactPoint - vertices [i]).sqrMagnitude;

			if (dist < sqrRadius)
				{
				Vector3 damage = (localContactForce * (damageRadius - Mathf.Sqrt(dist)) / damageRadius) + Random.onUnitSphere * maxVertexFracture;
				vertices[i] += damage;

				Vector3 deform = vertices[i] - originalMesh[i];

				if (deform.sqrMagnitude > sqrMaxDeform)
					vertices[i] = originalMesh[i] + deform.normalized * maxDisplacement;

				totalDamage += damage.magnitude;
				damagedVertices++;
				}
			}

		mesh.vertices = vertices;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();

		return damagedVertices > 0? totalDamage / damagedVertices : 0.0f;
		}


	float DeformNode (Transform T, Vector3 originalLocalPos, Quaternion originalLocalRot, Vector3 contactPoint, Vector3 contactVelocity)
		{
		float dist = (contactPoint - T.position).sqrMagnitude;
		float damageRatio = (damageRadius - Mathf.Sqrt(dist)) / damageRadius;

		float totalDamage = 0.0f;

		// Distort position

		if (dist < damageRadius * damageRadius)
			{
			Vector3 damage = contactVelocity * damageRatio + Random.onUnitSphere * maxVertexFracture;
			T.position += damage;

			Vector3 deform = T.localPosition - originalLocalPos;

			if (deform.sqrMagnitude > maxDisplacement * maxDisplacement)
				T.localPosition = originalLocalPos + deform.normalized * maxDisplacement;

			totalDamage += damage.magnitude;
			}

		// Distort rotation

		if (dist < nodeDamageRadius * nodeDamageRadius)
			{
			Vector3 angles = AnglesToVector(T.localEulerAngles);

			Vector3 angleLimit = new Vector3(maxNodeRotation, maxNodeRotation, maxNodeRotation);
			Vector3 angleMax = angles + angleLimit;
			Vector3 angleMin = angles - angleLimit;

			Vector3 damage = damageRatio * nodeRotationRate * Random.onUnitSphere;
			angles += damage;

			T.localEulerAngles = new Vector3(
				Mathf.Clamp(angles.x, angleMin.x, angleMax.x),
				Mathf.Clamp(angles.y, angleMin.y, angleMax.y),
				Mathf.Clamp(angles.z, angleMin.z, angleMax.z));

			totalDamage += damage.magnitude / 45.0f;
			}

		return totalDamage;
		}


	Vector3 AnglesToVector (Vector3 Angles)
		{
		if (Angles.x > 180) Angles.x = -360 + Angles.x;
		if (Angles.y > 180) Angles.y = -360 + Angles.y;
		if (Angles.z > 180) Angles.z = -360 + Angles.z;
		return Angles;
		}


	float DeformColliders (Vector3 contactPoint, Vector3 impactVelocity)
		{
		#if EVP_DEFORMABLE_COLLIDERS
		if (colliders.Length > 0)
			{
			Vector3 CoM = m_vehicle.cachedRigidbody.centerOfMass;
			float totalDeform = 0.0f;

			for (int i=0, c=colliders.Length; i<c; i++)
				{
				// Requires an intermediate mesh to be deformed and assigned

				Mesh mesh = new Mesh();
				mesh.vertices = colliders[i].sharedMesh.vertices;
				mesh.triangles = colliders[i].sharedMesh.triangles;

				totalDeform += DeformMesh(mesh, m_originalColliders[i], colliders[i].transform, contactPoint, impactVelocity);
				colliders[i].sharedMesh = mesh;
				}

			m_vehicle.cachedRigidbody.centerOfMass = CoM;
			return totalDeform;
			}
		else
			{
			return 0.0f;
			}
		#else
		return 0.0f;
		#endif
		}


	//----------------------------------------------------------------------------------------------


	void ProcessRepair ()
        {
		if (m_repairing)
			{
			float repairedThreshold = 0.002f;
			bool repaired = true;

			// Move vertices towards their original positions

			for (int i=0, c=meshes.Length; i<c; i++)
				repaired = RepairMesh(meshes[i].mesh, m_originalMeshes[i], vertexRepairRate, repairedThreshold) && repaired;

			// Move nodes towards their original positions and rotations

			for (int i=0, c=nodes.Length; i<c; i++)
				repaired = RepairNode(nodes[i], m_originalNodePositions[i], m_originalNodeRotations[i], vertexRepairRate, repairedThreshold) && repaired;

			// After completing the progressive restauration, nodes and colliders are reset to
			// their exact original state.

			if (repaired)
				{
				m_repairing = false;
				m_meshDamage = 0.0f;
				m_colliderDamage = 0.0f;
				m_nodeDamage = 0.0f;

				RestoreNodes();
				RestoreColliders();
				}
			}
		}


	bool RepairMesh (Mesh mesh, Vector3[] originalMesh, float repairRate, float repairedThreshold)
		{
		bool result = true;
		Vector3[] vertices = mesh.vertices;

		repairRate *= Time.deltaTime;
		repairedThreshold *= repairedThreshold;  // Using squared distances

		for (int i=0, c=vertices.Length; i<c; i++)
			{
			vertices[i] = Vector3.MoveTowards(vertices[i], originalMesh[i], repairRate);

			if ((originalMesh[i] - vertices[i]).sqrMagnitude >= repairedThreshold)
				result = false;
			}

		mesh.vertices = vertices;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();

		return result;
		}


	bool RepairNode (Transform T, Vector3 originalLocalPosition, Quaternion originalLocalRotation, float repairRate, float repairedThreshold)
		{
		repairRate *= Time.deltaTime;

		T.localPosition = Vector3.MoveTowards(T.localPosition, originalLocalPosition, repairRate);
		T.localRotation = Quaternion.RotateTowards(T.localRotation, originalLocalRotation, repairRate * 50.0f);

		return (originalLocalPosition - T.localPosition).sqrMagnitude < (repairedThreshold*repairedThreshold) &&
			Quaternion.Angle(originalLocalRotation, T.localRotation) < repairedThreshold;
		}


	void RestoreMeshes ()
		{
		for (int i=0, c=meshes.Length; i<c; i++)
			{
			Mesh mesh =  meshes[i].mesh;

			mesh.vertices = m_originalMeshes[i];
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			}
		}


	void RestoreNodes ()
		{
		for (int i=0, c=nodes.Length; i<c; i++)
			{
			nodes[i].localPosition = m_originalNodePositions[i];
			nodes[i].localRotation = m_originalNodeRotations[i];
			}
		}


	void RestoreColliders ()
		{
		#if EVP_DEFORMABLE_COLLIDERS
		if (colliders.Length > 0)
			{
			Vector3 CoM = m_vehicle.cachedRigidbody.centerOfMass;

			for (int i=0, c=colliders.Length; i<c; i++)
				{
				Mesh mesh = new Mesh();
				mesh.vertices = m_originalColliders[i];
				mesh.triangles = colliders[i].sharedMesh.triangles;

				mesh.RecalculateNormals();
				mesh.RecalculateBounds();

				colliders[i].sharedMesh = mesh;
				}

			m_vehicle.cachedRigidbody.centerOfMass = CoM;
			}
		#endif
		}
	}
}