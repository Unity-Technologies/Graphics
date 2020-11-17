//------------------------------------------------------------------------------------------------
// Edy's Vehicle Physics
// (c) Angel Garcia "Edy" - Oviedo, Spain
// http://www.edy.es
//------------------------------------------------------------------------------------------------
//
// Optimization notes:
//
//	Tire marks is a dynamically created mesh that gets updated whenever new marks are added.
//
//	- maxMarks: less is faster. Vertices, normals etc. are passed to the renderer on each change.
//	- fadeOutRange: smaller is faster. Each fading segment is adjusted on each change.


using UnityEngine;
using UnityEngine.Rendering;

namespace EVP
{

// Variables for each mark point created.
// A mark segment will be generated from each two consecutive marks.

public class MarkPoint
	{
	public Vector3 pos = Vector3.zero;
	public Vector3 normal = Vector3.zero;
	public Vector4 tangent = Vector4.zero;
	public Vector3 posl = Vector3.zero;
	public Vector3 posr = Vector3.zero;
	public float intensity = 0.0f;
	public int lastIndex = 0;
	}


public class TireMarksRenderer : MonoBehaviour
	{
	public enum Mode { PressureAndSkid, PressureOnly }

	public Mode mode = Mode.PressureAndSkid;
	[Range(0,1)]
	public float pressureBoost = 0.0f;

	[Space(5)]
	public int maxMarks = 1024;				// Maximum number of mark segments that can be rendered
	public float minDistance = 0.1f;		// Minimum distance between two consecutive marks.
	public float groundOffset = 0.02f;		// Distance the marks are placed above the surface they're placed upon (m)
	public float textureOffsetY = 0.05f;	// UV tweak for texture continuity
	[Range(0,1)]
	public float fadeOutRange = 0.5f;		// Proportion of the marks that are fade out

	public Material material;				// Material used for the marks


	int m_markCount;
	int m_markArraySize = 0;
	MarkPoint[] m_markPoints;

	CommonTools.BiasLerpContext m_biasCtx = new CommonTools.BiasLerpContext();

	bool m_segmentsUpdated;
	int m_segmentCount;
	int m_segmentArraySize;

	Mesh m_mesh;
	Vector3[] m_vertices;
	Vector3[] m_normals;
	Vector4[] m_tangents;
	Color[] m_colors;
	Vector2[] m_uvs;
	int[] m_triangles;
	Vector2[] m_values;


	void OnEnable ()
		{
		// Tire marks manager must be located at the origin with no rotation

		transform.position = Vector3.zero;
		transform.rotation = Quaternion.identity;

		// Create the mesh components for drawing the marks

		MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
		if (meshFilter == null)
			meshFilter = gameObject.AddComponent<MeshFilter>();

		MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
		if (meshRenderer == null)
			{
			meshRenderer = gameObject.AddComponent<MeshRenderer>();
			meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
			meshRenderer.material = material;
			}

		if (maxMarks < 10) maxMarks = 10;

		// Initialize mark points array

		m_markPoints = new MarkPoint[maxMarks*2];
		for (int i = 0, c = m_markPoints.Length; i < c; i++)
			m_markPoints[i] = new MarkPoint();

		m_markCount = 0;
		m_markArraySize = m_markPoints.Length;

		// Initialize the mesh source arrays.
		// These will be edited and passed to the mesh filter in runtime.

		m_vertices = new Vector3[maxMarks * 4];
		m_normals = new Vector3[maxMarks * 4];
		m_tangents = new Vector4[maxMarks * 4];
		m_colors = new Color[maxMarks * 4];
		m_uvs = new Vector2[maxMarks * 4];
		m_triangles = new int[maxMarks * 6];
		m_values = new Vector2[maxMarks];

		m_segmentCount = 0;
		m_segmentArraySize = maxMarks;
		m_segmentsUpdated = false;

		// Elements that will be invariant

		for (int i = 0; i < m_segmentArraySize; i++)
			{
			m_uvs[i * 4 + 0] = new Vector2(0, textureOffsetY);
			m_uvs[i * 4 + 1] = new Vector2(1, textureOffsetY);
			m_uvs[i * 4 + 2] = new Vector2(0, 1-textureOffsetY);
			m_uvs[i * 4 + 3] = new Vector2(1, 1-textureOffsetY);

			m_triangles[i * 6 + 0] = i * 4 + 0;
			m_triangles[i * 6 + 2] = i * 4 + 1;
			m_triangles[i * 6 + 1] = i * 4 + 2;

			m_triangles[i * 6 + 3] = i * 4 + 2;
			m_triangles[i * 6 + 5] = i * 4 + 1;
			m_triangles[i * 6 + 4] = i * 4 + 3;
			}

		// Initialize the mesh

		m_mesh = new Mesh();
		m_mesh.MarkDynamic();
		m_mesh.vertices = m_vertices;
		m_mesh.normals = m_normals;
		m_mesh.tangents = m_tangents;
		m_mesh.colors = m_colors;
		m_mesh.triangles = m_triangles;
		m_mesh.uv = m_uvs;
		m_mesh.RecalculateBounds();

		meshFilter.mesh = m_mesh;
		}


	void OnValidate ()
		{
		if (m_uvs != null)
			{
			for (int i = 0; i < m_uvs.Length/4; i++)
				{
				m_uvs[i * 4 + 0] = new Vector2(0, textureOffsetY);
				m_uvs[i * 4 + 1] = new Vector2(1, textureOffsetY);
				m_uvs[i * 4 + 2] = new Vector2(0, 1-textureOffsetY);
				m_uvs[i * 4 + 3] = new Vector2(1, 1-textureOffsetY);
				}
			}

		m_segmentsUpdated = true;
		}


	// Called externally for each point where a mark might be created.
	//
	// The lastIndex value is the last value returned by this method for the same wheel,
	// and allows creating different continuous mark treads for each wheel.
	// lastIndex = -1 starts a new segment.

	public int AddMark (Vector3 pos, Vector3 normal, float pressureRatio, float skidRatio, float width, int lastIndex)
		{
		if (!isActiveAndEnabled || m_markArraySize == 0)
			return -1;

		pressureRatio = CommonTools.BiasedLerp(pressureRatio, 0.5f + pressureBoost*0.5f, m_biasCtx);

		float intensity = 0.0f;

		switch (mode)
			{
			case Mode.PressureAndSkid:
				intensity = Mathf.Clamp01(pressureRatio * skidRatio);
				break;

			case Mode.PressureOnly:
				intensity = Mathf.Clamp01(pressureRatio);
				break;
			}

		if (intensity <= 0.0f) return -1;
		if (intensity > 1.0f) intensity = 1.0f;

		Vector3 newPos = pos + normal * groundOffset;
		if (lastIndex >= 0 && Vector3.Distance(newPos, m_markPoints[lastIndex % m_markArraySize].pos) < minDistance)
				return lastIndex;

		// Create new mark segment

		MarkPoint current = m_markPoints[m_markCount % m_markArraySize];
		current.pos = newPos;
		current.normal = normal;
		current.intensity = intensity;
		current.lastIndex = lastIndex;

		if (lastIndex >= 0 && lastIndex > m_markCount - m_markArraySize)
			{
			MarkPoint last = m_markPoints[lastIndex % m_markArraySize];
			Vector3 dir = (current.pos - last.pos);
			Vector3 crossDir = Vector3.Cross(dir, normal).normalized;
			Vector3 widthDir = 0.5f * width * crossDir;

			current.posl = current.pos + widthDir;
			current.posr = current.pos - widthDir;
			current.tangent = new Vector4(crossDir.x, crossDir.y, crossDir.z, 1);

			if (last.lastIndex < 0)
				{
				last.tangent = current.tangent;
				last.posl = current.pos + widthDir;
				last.posr = current.pos - widthDir;
				}

			// Add the actual segment to the mesh

			AddSegment(last, current);
			}

		m_markCount++;
		return m_markCount-1;
		}


	public void Clear ()
		{
		if (isActiveAndEnabled)
			{
			m_markCount = 0;
			m_segmentCount = 0;

			for (int i = 0, c = m_vertices.Length; i < c; i++)
				m_vertices[i] = Vector3.zero;

			m_mesh.vertices = m_vertices;
			m_segmentsUpdated = true;
			}
		}


	void AddSegment (MarkPoint first, MarkPoint second)
		{
		int segmentIndex = (m_segmentCount % m_segmentArraySize) * 4;

		m_vertices[segmentIndex + 0] = first.posl;
		m_vertices[segmentIndex + 1] = first.posr;
		m_vertices[segmentIndex + 2] = second.posl;
		m_vertices[segmentIndex + 3] = second.posr;

		m_normals[segmentIndex + 0] = first.normal;
		m_normals[segmentIndex + 1] = first.normal;
		m_normals[segmentIndex + 2] = second.normal;
		m_normals[segmentIndex + 3] = second.normal;

		m_tangents[segmentIndex + 0] = first.tangent;
		m_tangents[segmentIndex + 1] = first.tangent;
		m_tangents[segmentIndex + 2] = second.tangent;
		m_tangents[segmentIndex + 3] = second.tangent;

		m_colors[segmentIndex + 0].a = first.intensity;
		m_colors[segmentIndex + 1].a = first.intensity;
		m_colors[segmentIndex + 2].a = second.intensity;
		m_colors[segmentIndex + 3].a = second.intensity;

		m_values[segmentIndex/4] = new Vector2(first.intensity, second.intensity);

		// The very first segment moves all vertices to that position.
		// This allows proper calculation of the bounds later.

		if (m_segmentCount == 0)
			{
			Vector3 v = m_vertices[0];
			for (int i = 4, c = m_vertices.Length; i < c; i++)
				m_vertices[i] = v;
			}

		m_segmentCount++;
		m_segmentsUpdated = true;
		}


	void LateUpdate ()
		{
		if (!m_segmentsUpdated) return;
		m_segmentsUpdated = false;

		// Progressively fade out the older marks

		int toFade = (int)(m_segmentArraySize * fadeOutRange);
		if (toFade > 0)
			{
			int segment = m_segmentCount - m_segmentArraySize;
			int fadeStart = 0;

			if (segment < 0)
				{
				fadeStart = -segment;
				segment = 0;
				}

			float fadeStep = 1.0f / toFade;

			for (int i = fadeStart; i < toFade; i++)
				{
				int valueIndex = segment % m_segmentArraySize;
				int colorIndex = valueIndex * 4;

				float decay = i * fadeStep;
				float intensity1 = m_values[valueIndex].x * decay;
				float intensity2 = m_values[valueIndex].y * decay + fadeStep;

				m_colors[colorIndex + 0].a = intensity1;
				m_colors[colorIndex + 1].a = intensity1;
				m_colors[colorIndex + 2].a = intensity2;
				m_colors[colorIndex + 3].a = intensity2;

				segment++;
				}
			}

		// Update the mesh

		m_mesh.MarkDynamic();
		m_mesh.vertices = m_vertices;
		m_mesh.normals = m_normals;
		m_mesh.tangents = m_tangents;
		m_mesh.colors = m_colors;
		m_mesh.RecalculateBounds();
		}
	}
}