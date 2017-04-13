using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    public class SkyboxHelper
    {
        public SkyboxHelper()
        {
        }

        const int k_NumFullSubdivisions = 3; // 3 subdivs == 2048 triangles
        const int k_NumHorizonSubdivisions = 2;

        public void CreateMesh()
        {
            var vertData = new Vector3[8 * 3];
            for (int i = 0; i < 8 * 3; i++)
            {
                vertData[i] = m_OctaVerts[i];
            }

            // Regular subdivisions
            for (int i = 0; i < k_NumFullSubdivisions; i++)
            {
                var srcData = vertData.Clone() as Vector3[];
                var verts = new List<Vector3>();

                for (int k = 0; k < srcData.Length; k += 3)
                {
                    Subdivide(verts, srcData[k], srcData[k + 1], srcData[k + 2]);
                }
                vertData = verts.ToArray();
            }

            // Horizon subdivisions
            var horizonLimit = 1.0f;
            for (int i = 0; i < k_NumHorizonSubdivisions; i++)
            {
                var srcData = vertData.Clone() as Vector3[];
                var verts = new List<Vector3>();

                horizonLimit *= 0.5f; // First iteration limit to y < +-0.5, next one 0.25 etc.
                for (int k = 0; k < srcData.Length; k += 3)
                {
                    var maxAbsY = Mathf.Max(Mathf.Abs(srcData[k].y), Mathf.Abs(srcData[k + 1].y), Mathf.Abs(srcData[k + 2].y));
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
            var vertexCount = vertData.Length;
            var triangles = new int[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                triangles[i] = i;
            }

            m_Mesh = new Mesh
            {
                vertices = vertData,
                triangles = triangles
            };
        }

        public UnityEngine.Mesh mesh
        {
            get { return m_Mesh; }
        }

        public void Draw(ScriptableRenderContext loop, Camera camera)
        {
            if (camera.clearFlags != CameraClearFlags.Skybox)
            {
                return;
            }

            var mat = RenderSettings.skybox;

            if (mat == null)
            {
                return;
            }

            var cmd = new CommandBuffer { name = "Skybox" };

            var looksLikeSixSidedShader = true;
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

                var dist = camera.farClipPlane * 10.0f;

                var world = Matrix4x4.TRS(camera.transform.position, Quaternion.identity, new Vector3(dist, dist, dist));

                var skyboxProj = SkyboxHelper.GetProjectionMatrix(camera);
                cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, skyboxProj);
                cmd.DrawMesh(mesh, world, mat);

                cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
            }

            loop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        public static Matrix4x4 GetProjectionMatrix(Camera camera)
        {
            var skyboxProj = Matrix4x4.Perspective(camera.fieldOfView, camera.aspect, camera.nearClipPlane, camera.farClipPlane);

            var nearPlane = camera.nearClipPlane * 0.01f;
            skyboxProj = AdjustDepthRange(skyboxProj, camera.nearClipPlane, nearPlane, camera.farClipPlane);
            return MakeProjectionInfinite(skyboxProj, nearPlane);
        }

        static Matrix4x4 MakeProjectionInfinite(Matrix4x4 m, float nearPlane)
        {
            const float epsilon = 1e-6f;

            var r = m;
            r[2, 2] = -1.0f + epsilon;
            r[2, 3] = (-2.0f + epsilon) * nearPlane;
            r[3, 2] = -1.0f;
            return r;
        }

        static Matrix4x4 AdjustDepthRange(Matrix4x4 mat, float origNear, float newNear, float newFar)
        {
            var x = mat[0, 0];
            var y = mat[1, 1];
            var w = mat[0, 2];
            var z = mat[1, 2];

            var r = ((2.0f * origNear) / x) * ((w + 1) * 0.5f);
            var t = ((2.0f * origNear) / y) * ((z + 1) * 0.5f);
            var l = ((2.0f * origNear) / x) * (((w + 1) * 0.5f) - 1);
            var b = ((2.0f * origNear) / y) * (((z + 1) * 0.5f) - 1);

            var ratio = (newNear / origNear);

            r *= ratio;
            t *= ratio;
            l *= ratio;
            b *= ratio;

            var ret = new Matrix4x4();

            ret[0, 0] = (2.0f * newNear) / (r - l); ret[0, 1] = 0; ret[0, 2] = (r + l) / (r - l); ret[0, 3] = 0;
            ret[1, 0] = 0; ret[1, 1] = (2.0f * newNear) / (t - b); ret[1, 2] = (t + b) / (t - b); ret[1, 3] = 0;
            ret[2, 0] = 0; ret[2, 1] = 0; ret[2, 2] = -(newFar + newNear) / (newFar - newNear); ret[2, 3] = -(2.0f * newFar * newNear) / (newFar - newNear);
            ret[3, 0] = 0; ret[3, 1] = 0; ret[3, 2] = -1.0f; ret[3, 3] = 0;

            return ret;
        }

        // Octahedron vertices
        readonly Vector3[] m_OctaVerts =
        {
            new Vector3(0.0f, 1.0f, 0.0f),      new Vector3(0.0f, 0.0f, -1.0f),     new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),      new Vector3(1.0f, 0.0f, 0.0f),      new Vector3(0.0f, 0.0f, 1.0f),
            new Vector3(0.0f, 1.0f, 0.0f),      new Vector3(0.0f, 0.0f, 1.0f),      new Vector3(-1.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),      new Vector3(-1.0f, 0.0f, 0.0f),     new Vector3(0.0f, 0.0f, -1.0f),
            new Vector3(0.0f, -1.0f, 0.0f),     new Vector3(1.0f, 0.0f, 0.0f),      new Vector3(0.0f, 0.0f, -1.0f),
            new Vector3(0.0f, -1.0f, 0.0f),     new Vector3(0.0f, 0.0f, 1.0f),      new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(0.0f, -1.0f, 0.0f),     new Vector3(-1.0f, 0.0f, 0.0f),     new Vector3(0.0f, 0.0f, 1.0f),
            new Vector3(0.0f, -1.0f, 0.0f),     new Vector3(0.0f, 0.0f, -1.0f),     new Vector3(-1.0f, 0.0f, 0.0f),
        };

        Vector3 SubDivVert(Vector3 v1, Vector3 v2)
        {
            return Vector3.Normalize(v1 + v2);
        }

        void Subdivide(ICollection<Vector3> dest, Vector3 v1, Vector3 v2, Vector3 v3)
        {
            var v12 = SubDivVert(v1, v2);
            var v23 = SubDivVert(v2, v3);
            var v13 = SubDivVert(v1, v3);

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

        void SubdivideYOnly(ICollection<Vector3> dest, Vector3 v1, Vector3 v2, Vector3 v3)
        {
            // Find out which vertex is furthest out from the others on the y axis

            var d12 = Mathf.Abs(v2.y - v1.y);
            var d23 = Mathf.Abs(v2.y - v3.y);
            var d31 = Mathf.Abs(v3.y - v1.y);

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

            var v12 = SubDivVert(top, va);
            var v13 = SubDivVert(top, vb);

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

        Mesh m_Mesh;
    }
}
