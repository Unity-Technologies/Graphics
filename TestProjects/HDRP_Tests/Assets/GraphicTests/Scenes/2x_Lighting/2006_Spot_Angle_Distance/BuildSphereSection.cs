using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class BuildSphereSection : MonoBehaviour
{
	public float radius = 1f;
	public float angle = 60f;
	public int segmentsX = 32;
	public int segmentsY = 5;

	public Mesh mesh;

	void OnValidate()
	{
		MeshFilter meshFilter = GetComponent<MeshFilter>();

		if ( segmentsX  < 3 ) segmentsX = 3;
		if ( segmentsY < 1 ) segmentsY = 1;
		angle = Mathf.Clamp(angle, 1f, 179f);

		int vertexcount = segmentsX * segmentsY + 1;

		Vector3[] vertices = new Vector3[ vertexcount ];
		List<int> triangles = new List<int>();
		Vector2[] uvs = new Vector2[ vertexcount ];
		Vector3[] normals = new Vector3[vertexcount];

		float halfAngleRad = Mathf.Deg2Rad * angle * 0.5f;

		float sphereRadius = radius / Mathf.Sin( halfAngleRad );

		int index;
		int[] quadIndices = new int[4];
		float angleA;
		float angleB;
		Quaternion rot;

		Vector3 offset = - Vector3.forward * sphereRadius;

		for (int y=0 ; y<segmentsY ; ++y)
		{
			angleB = angle * 0.5f * (1f - 1f * y / segmentsY );
			for (int x=0 ; x < segmentsX ; ++x)
			{
				angleA = 360f * x / segmentsX;

				index = x + y * segmentsX;

				//Debug.Log("x:"+x+", y:"+y+", angleA:"+(angleA*Mathf.Rad2Deg)+", angleB:"+(angleB*Mathf.Rad2Deg));
				rot = Quaternion.Euler(0f, 0f, angleA) * Quaternion.Euler(angleB, 0f, 0f);

				vertices[index] = rot * Vector3.forward * sphereRadius + offset;

				//Debug.DrawLine( vertices[index] + transform.position, transform.position, Color.cyan, 1f );

				normals[index] = (offset - vertices[index]).normalized;
				uvs[index] = ( Vector2.one + new Vector2( Mathf.Cos(angleA), Mathf.Sin(angleA)) * y / segmentsY ) * 0.5f ;

				if (y < (segmentsY-1))
				{
					quadIndices[0] = index;
					quadIndices[1] = (x==(segmentsX-1))? y*segmentsX : index+1;
					quadIndices[3] = index + segmentsX;
					quadIndices[2] = (x==(segmentsX-1))? (y+1)*segmentsX : quadIndices[3]+1;

					triangles.Add(quadIndices[0]);
					triangles.Add(quadIndices[2]);
					triangles.Add(quadIndices[1]);

					triangles.Add(quadIndices[0]);
					triangles.Add(quadIndices[3]);
					triangles.Add(quadIndices[2]);
				}
				else
				{
					quadIndices[0] = index;
					quadIndices[1] = (x==(segmentsX-1))? y*segmentsX : index+1;
					quadIndices[2] = vertexcount-1;

					triangles.Add(quadIndices[0]);
					triangles.Add(quadIndices[2]);
					triangles.Add(quadIndices[1]);
				}
			}
		}

		vertices[vertexcount-1] = Vector3.forward * sphereRadius + offset;
		normals[vertexcount-1] = (offset - vertices[vertexcount-1]).normalized;
		uvs[vertexcount-1] = Vector2.one * 0.5f; 

		if (mesh == null) mesh = new Mesh();
		mesh.name = "Sphere Section";
		mesh.Clear();
		mesh.vertices = vertices;
		mesh.triangles = triangles.ToArray();
		mesh.normals = normals;
		mesh.uv = uvs;
		mesh.RecalculateBounds();

		meshFilter.mesh = mesh;
	}
}
