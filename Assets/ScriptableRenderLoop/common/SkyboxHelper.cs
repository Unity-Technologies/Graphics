using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;

public class SkyboxHelper
{
	public SkyboxHelper()
	{
	}

	const int NumFullSubdivisions = 3; // 3 subdivs == 2048 triangles
	const int NumHorizonSubdivisions = 2;

	public void CreateMesh()
	{
		Vector3[] vertData = new Vector3[8 * 3];
		for (int i = 0; i < 8 * 3; i++)
		{
			vertData[i] = octaVerts[i];
		}

		// Regular subdivisions
		for (int i = 0; i < NumFullSubdivisions; i++)
		{
			Vector3[] srcData = vertData.Clone() as Vector3[];
			List<Vector3> verts = new List<Vector3>();

			for (int k = 0; k < srcData.Length; k += 3)
			{
				Subdivide(verts, srcData[k], srcData[k + 1], srcData[k + 2]);
			}
			vertData = verts.ToArray();
		}

		// Horizon subdivisions
		float horizonLimit = 1.0f;
		for (int i = 0; i < NumHorizonSubdivisions; i++)
		{
			Vector3[] srcData = vertData.Clone() as Vector3[];
			List<Vector3> verts = new List<Vector3>();

			horizonLimit *= 0.5f; // First iteration limit to y < +-0.5, next one 0.25 etc.
			for (int k = 0; k < srcData.Length; k += 3)
			{
				float maxAbsY = Mathf.Max(Mathf.Abs(srcData[k].y), Mathf.Abs(srcData[k + 1].y), Mathf.Abs(srcData[k + 2].y));
				if (maxAbsY > horizonLimit)
				{
					// Pass through existing triangle
					verts.Add(srcData[k]);
					verts.Add(srcData[k + 1]);
					verts.Add(srcData[k + 2]);
				}
				else
				{
					SubdivideYOnly(verts, srcData[k], srcData[k + 1], srcData[k + 2]);
				}
			}
			vertData = verts.ToArray();
		}

		// Write out the mesh
		int vertexCount = vertData.Length;
		var triangles = new int[vertexCount];
		for (int i = 0; i < vertexCount; i++)
		{
			triangles[i] = i;
		}

		_mesh = new Mesh();
		_mesh.vertices = vertData;
		_mesh.triangles = triangles;
	}

	public UnityEngine.Mesh mesh
	{
		get { return _mesh; }
	}

	public void Draw(RenderLoop loop, Camera camera)
	{
		if (camera.clearFlags != CameraClearFlags.Skybox)
		{
			return;
		}

		Material mat = RenderSettings.skybox;

		if (mat == null)
		{
			return;
		}

		CommandBuffer cmd = new CommandBuffer();
		cmd.name = "Skybox";

		bool looksLikeSixSidedShader = true;
		looksLikeSixSidedShader &= (mat.passCount == 6); // should have six passes
		//looksLikeSixSidedShader &= !mat.GetShader()->GetShaderLabShader()->HasLightingPasses();

		if (looksLikeSixSidedShader)
		{
			Debug.LogWarning("Six sided skybox not yet supported.");
		}
		else
		{
			if (mesh == null)
			{
				CreateMesh();
			}
			
			float dist = camera.farClipPlane * 10.0f;

			Matrix4x4 world = Matrix4x4.TRS(camera.transform.position, Quaternion.identity, new Vector3(dist, dist, dist));

			Matrix4x4 skyboxProj = SkyboxHelper.GetProjectionMatrix(camera);
			cmd.SetProjectionAndViewMatrices(skyboxProj, camera.worldToCameraMatrix);
			cmd.DrawMesh(mesh, world, mat);

			cmd.SetProjectionAndViewMatrices(camera.projectionMatrix, camera.worldToCameraMatrix);
		}

		loop.ExecuteCommandBuffer(cmd);
		cmd.Dispose();
	}

	static public Matrix4x4 GetProjectionMatrix(Camera camera)
	{
		Matrix4x4 skyboxProj = Matrix4x4.Perspective(camera.fieldOfView, camera.aspect, camera.nearClipPlane, camera.farClipPlane);

		float nearPlane = camera.nearClipPlane * 0.01f;
		skyboxProj = AdjustDepthRange(skyboxProj, camera.nearClipPlane, nearPlane, camera.farClipPlane);
		return MakeProjectionInfinite(skyboxProj, nearPlane);
	}

	static Matrix4x4 MakeProjectionInfinite(Matrix4x4 m, float nearPlane)
	{
		const float epsilon = 1e-6f;

		Matrix4x4 r = m;
		r[2, 2] = -1.0f + epsilon;
		r[2, 3] = (-2.0f + epsilon) * nearPlane;
		r[3, 2] = -1.0f;
		return r;
	}

	static Matrix4x4 AdjustDepthRange(Matrix4x4 mat, float origNear, float newNear, float newFar)
	{
		float x = mat[0, 0];
		float y = mat[1, 1];
		float w = mat[0, 2];
		float z = mat[1, 2];

		float r = ((2.0f * origNear) / x) * ((w + 1) * 0.5f);
		float t = ((2.0f * origNear) / y) * ((z + 1) * 0.5f);
		float l = ((2.0f * origNear) / x) * (((w + 1) * 0.5f) - 1);
		float b = ((2.0f * origNear) / y) * (((z + 1) * 0.5f) - 1);

		float ratio = (newNear / origNear);

		r *= ratio;
		t *= ratio;
		l *= ratio;
		b *= ratio;

		Matrix4x4 ret = new Matrix4x4();

		ret[0, 0] = (2.0f * newNear) / (r - l); ret[0, 1] = 0; ret[0, 2] = (r + l) / (r - l); ret[0, 3] = 0;
		ret[1, 0] = 0; ret[1, 1] = (2.0f * newNear) / (t - b); ret[1, 2] = (t + b) / (t - b); ret[1, 3] = 0;
		ret[2, 0] = 0; ret[2, 1] = 0; ret[2, 2] = -(newFar + newNear) / (newFar - newNear); ret[2, 3] = -(2.0f * newFar * newNear) / (newFar - newNear);
		ret[3, 0] = 0; ret[3, 1] = 0; ret[3, 2] = -1.0f; ret[3, 3] = 0;

		return ret;
	}

	// Octahedron vertices
	Vector3[] octaVerts =
	{
		new Vector3(0.0f, 1.0f, 0.0f),		new Vector3(0.0f, 0.0f, -1.0f),		new Vector3(1.0f, 0.0f, 0.0f),
		new Vector3(0.0f, 1.0f, 0.0f),		new Vector3(1.0f, 0.0f, 0.0f),		new Vector3(0.0f, 0.0f, 1.0f),
		new Vector3(0.0f, 1.0f, 0.0f),		new Vector3(0.0f, 0.0f, 1.0f),		new Vector3(-1.0f, 0.0f, 0.0f),
		new Vector3(0.0f, 1.0f, 0.0f),		new Vector3(-1.0f, 0.0f, 0.0f),		new Vector3(0.0f, 0.0f, -1.0f),
		new Vector3(0.0f, -1.0f, 0.0f),		new Vector3(1.0f, 0.0f, 0.0f),		new Vector3(0.0f, 0.0f, -1.0f),
		new Vector3(0.0f, -1.0f, 0.0f),		new Vector3(0.0f, 0.0f, 1.0f),		new Vector3(1.0f, 0.0f, 0.0f),
		new Vector3(0.0f, -1.0f, 0.0f),		new Vector3(-1.0f, 0.0f, 0.0f),		new Vector3(0.0f, 0.0f, 1.0f),
		new Vector3(0.0f, -1.0f, 0.0f),		new Vector3(0.0f, 0.0f, -1.0f),		new Vector3(-1.0f, 0.0f, 0.0f),
	};

	Vector3 SubDivVert(Vector3 v1, Vector3 v2)
	{
		return Vector3.Normalize(v1 + v2);
	}

	void Subdivide(List<Vector3> dest, Vector3 v1, Vector3 v2, Vector3 v3)
	{
		Vector3 v12 = SubDivVert(v1, v2);
		Vector3 v23 = SubDivVert(v2, v3);
		Vector3 v13 = SubDivVert(v1, v3);

		dest.Add(v1);
		dest.Add(v12);
		dest.Add(v13);
		dest.Add(v12);
		dest.Add(v2);
		dest.Add(v23);
		dest.Add(v23);
		dest.Add(v13);
		dest.Add(v12);
		dest.Add(v3);
		dest.Add(v13);
		dest.Add(v23);
	}

	void SubdivideYOnly(List<Vector3> dest, Vector3 v1, Vector3 v2, Vector3 v3)
	{
		// Find out which vertex is furthest out from the others on the y axis

		float d12 = Mathf.Abs(v2.y - v1.y);
		float d23 = Mathf.Abs(v2.y - v3.y);
		float d31 = Mathf.Abs(v3.y - v1.y);

		Vector3 top, va, vb;

		if (d12 < d23 && d12 < d31)
		{
			top = v3;
			va = v1;
			vb = v2;
		}
		else if (d23 < d12 && d23 < d31)
		{
			top = v1;
			va = v2;
			vb = v3;
		}
		else
		{
			top = v2;
			va = v3;
			vb = v1;
		}

		Vector3 v12 = SubDivVert(top, va);
		Vector3 v13 = SubDivVert(top, vb);

		dest.Add(top);
		dest.Add(v12);
		dest.Add(v13);

		// A bit of extra logic to prevent triangle slivers: choose the shorter of (13->va), (12->vb) as triangle base
		if ((v13 - va).sqrMagnitude > (v12 - vb).sqrMagnitude)
		{
			dest.Add(v12);
			dest.Add(va);
			dest.Add(vb);
			dest.Add(v13);
			dest.Add(v12);
			dest.Add(vb);
		}
		else
		{
			dest.Add(v13);
			dest.Add(v12);
			dest.Add(va);
			dest.Add(v13);
			dest.Add(va);
			dest.Add(vb);
		}

	}

	Mesh _mesh;
}
