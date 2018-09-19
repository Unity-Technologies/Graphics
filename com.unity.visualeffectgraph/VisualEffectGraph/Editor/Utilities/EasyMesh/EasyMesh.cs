using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace UnityEditor.VFX.Utilities
{
    public partial class EasyMesh
    {
        public List<Primitive> m_Primitives;
        public List<Vertex> m_Vertices;
        public List<Point> m_Points;
        public List<Edge> m_Edges;

        public readonly int m_NumMeshes;

        private readonly Vector3 DEFAULT_NORMAL = new Vector3(0, 1, 0);
        private readonly Color DEFAULT_COLOR = new Color(1, 1, 1, 1);
        private readonly Vector4 DEFAULT_TANGENT = new Vector4(1, 1, 1, 1);
        private readonly Vector4 DEFAULT_UV = new Vector4(0, 0, 0, 0);

        public SearchablePointGrid m_SearchablePointGrid;

        private GameObject m_PreviewGameObject;

        public Mesh mesh { get { return m_Mesh; } }
        private Mesh m_Mesh;

        public EasyMesh(Mesh mesh, GameObject previewGameObject, float searchGridCellSize = 1.0f)
        {
            m_Mesh = mesh;
            m_PreviewGameObject = previewGameObject;

            int submesh = mesh.subMeshCount;
            int[] tris; //= mesh.triangles;
            Vector3[] vertices = mesh.vertices;
            Color[] colors = mesh.colors;
            Vector3[] normals = mesh.normals;
            Vector4[] tangents = mesh.tangents;

            List<Vector4> uv = new List<Vector4>();
            List<Vector4> uv2 = new List<Vector4>();
            List<Vector4> uv3 = new List<Vector4>();
            List<Vector4> uv4 = new List<Vector4>();
            mesh.GetUVs(0, uv);
            mesh.GetUVs(1, uv2);
            mesh.GetUVs(2, uv3);
            mesh.GetUVs(3, uv4);

            bool hasColor = colors.Length > 0;
            bool hasNormal = normals.Length > 0;
            bool hasTangent = tangents.Length > 0;
            bool hasUV = uv.Count > 0;
            bool hasUV2 = uv2.Count > 0;
            bool hasUV3 = uv3.Count > 0;
            bool hasUV4 = uv4.Count > 0;

            m_Primitives = new List<Primitive>();
            m_Vertices = new List<Vertex>();
            m_Points = new List<Point>();
            m_Edges = new List<Edge>();

            EditorUtility.ClearProgressBar();

            int vxCount = vertices.Length;
            bool cancel = false;

            for (int id = 0; id < vxCount && !cancel; id++)
            {
                cancel = EditorUtility.DisplayCancelableProgressBar("EasyMesh", "Analyzing Vertex #" + id, (float)id / vxCount);
                Vector3 pos = vertices[id];
                AddVertex(
                    pos,
                    hasNormal ? normals[id] : DEFAULT_NORMAL,
                    hasTangent ? tangents[id] : DEFAULT_TANGENT,
                    hasColor ? colors[id] : DEFAULT_COLOR,
                    hasUV ? uv[id] : DEFAULT_UV,
                    hasUV2 ? uv2[id] : DEFAULT_UV,
                    hasUV3 ? uv3[id] : DEFAULT_UV,
                    hasUV4 ? uv4[id] : DEFAULT_UV
                    );
            }

            if (cancel)
            {
                EditorUtility.ClearProgressBar();
                return;
            }

            m_NumMeshes = mesh.subMeshCount;

            for (int j = 0; j < m_NumMeshes; j++)
            {
                tris = mesh.GetTriangles(j);
                int triCount = tris.Length;

                for (int i = 0; i < triCount; i += 3)
                {
                    cancel = EditorUtility.DisplayCancelableProgressBar("EasyMesh", "Analyzing Triangle #" + i / 3 + " for submesh #" + j, (float)i / triCount);
                    AddPrimitive(m_Vertices[tris[i]], m_Vertices[tris[i + 1]], m_Vertices[tris[i + 2]], j);
                }
            }

            EditorUtility.ClearProgressBar();

            int k = 0;
            int ptcount = m_Points.Count;

            m_SearchablePointGrid = new SearchablePointGrid(searchGridCellSize);
            foreach (Point p in m_Points)
            {
                cancel = EditorUtility.DisplayCancelableProgressBar("EasyMesh", "Generating Searchable Grid...", (float)k / ptcount);
                m_SearchablePointGrid.AddPoint(p);
                k++;
                if (cancel) break;
            }
            EditorUtility.ClearProgressBar();

            if (cancel)
            {
                EditorUtility.ClearProgressBar();
                return;
            }
        }

        #region MESH EXPORT API


        /// <summary>
        /// Generates Geometry based on wanted attributes. Can be used for exporting vertex stream overrides
        /// </summary>
        /// <param name="setPositions"></param>
        /// <param name="setNormals"></param>
        /// <param name="setTangents"></param>
        /// <param name="setColor"></param>
        /// <param name="setUV"></param>
        /// <param name="setUV2"></param>
        /// <param name="setUV3"></param>
        /// <param name="setUV4"></param>
        /// <param name="setTriangles"></param>
        /// <returns></returns>
        public Mesh CreateGeometry(bool setNormals, bool setTangents, bool setColor, bool setUV, bool setUV2, bool setUV3, bool setUV4, bool setTriangles)
        {
            Mesh mesh = new Mesh();
            mesh.name = "EasyMesh";
            mesh.subMeshCount = m_NumMeshes;
            int vxCount = m_Vertices.Count;

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector4> tangents = new List<Vector4>();
            List<Color> colors = new List<Color>();
            List<Vector4> uv = new List<Vector4>();
            List<Vector4> uv2 = new List<Vector4>();
            List<Vector4> uv3 = new List<Vector4>();
            List<Vector4> uv4 = new List<Vector4>();

            int i = 0;
            int count = m_Vertices.Count;
            EditorUtility.ClearProgressBar();

            bool cancel = false;

            foreach (Vertex v in m_Vertices)
            {
                float t = (float)i / count;
                cancel = EditorUtility.DisplayCancelableProgressBar("EasyMesh", "Generating Geometry (Vertices)...", t);
                if (cancel) break;

                vertices.Add(v.Position);
                if (setNormals) normals.Add(v.Normal);
                if (setTangents) tangents.Add(v.Tangent);
                if (setColor) colors.Add(v.Color);
                if (setUV) uv.Add(v.UV);
                if (setUV2) uv2.Add(v.UV2);
                if (setUV3) uv3.Add(v.UV3);
                if (setUV4) uv4.Add(v.UV4);
                i++;
            }
            EditorUtility.ClearProgressBar();

            if (cancel) return null;

            EditorUtility.ClearProgressBar();
            mesh.SetVertices(vertices);
            if (setNormals) mesh.SetNormals(normals);
            if (setTangents) mesh.SetTangents(tangents);
            if (setColor) mesh.SetColors(colors);
            if (setUV) mesh.SetUVs(0, uv);
            if (setUV2) mesh.SetUVs(1, uv2);
            if (setUV3) mesh.SetUVs(2, uv3);
            if (setUV4) mesh.SetUVs(3, uv4);

            if (setTriangles)
            {
                for (int j = 0; j < m_NumMeshes; j++)
                {
                    var prims = m_Primitives.Where((prim) => prim.MeshID == j);
                    count = prims.Count();
                    int[] triangles = new int[count * 3];

                    i = 0;
                    foreach (Primitive p in prims)
                    {
                        float t = (float)i / count;
                        cancel = EditorUtility.DisplayCancelableProgressBar("EasyMesh", "Generating Triangles (Submesh #" + j + ")...", t / 3);
                        if (cancel) break;
                        triangles[i] = m_Vertices.IndexOf(p.A);
                        triangles[i + 1] = m_Vertices.IndexOf(p.B);
                        triangles[i + 2] = m_Vertices.IndexOf(p.C);
                        i += 3;
                    }

                    EditorUtility.ClearProgressBar();

                    if (cancel) return null;

                    mesh.SetTriangles(triangles, j);
                }
            }

            mesh.RecalculateBounds();
            if (!setNormals)
                mesh.RecalculateNormals();

            return mesh;
        }

        #endregion

        #region SEARCH

        public List<PointSearchResult> FindClosePoints(Vector3 position, float radius)
        {
            return new List<PointSearchResult>(m_SearchablePointGrid.Search(position, radius));
        }

        public List<PointSearchResult> FindClosePointsOrdered(Vector3 position, float radius, int maxpoints)
        {
            return new List<PointSearchResult>(m_SearchablePointGrid.Search(position, radius).OrderBy(r => r.Distance).Take(maxpoints));
        }

        public struct PointSearchResult
        {
            public float Distance;
            public Point Point;
        }
        public class SearchablePointGrid
        {
            public struct Int3
            {
                public int x;
                public int y;
                public int z;

                public Int3(int x, int y, int z)
                {
                    this.x = x;
                    this.y = y;
                    this.z = z;
                }

                public List<Int3> GetAdjacentCells()
                {
                    List<Int3> output = new List<Int3>();
                    output.Add(new Int3(x - 1, y - 1, z - 1));
                    output.Add(new Int3(x - 1, y - 1, z - 0));
                    output.Add(new Int3(x - 1, y - 1, z + 1));
                    output.Add(new Int3(x - 1, y - 0, z - 1));
                    output.Add(new Int3(x - 1, y - 0, z - 0));
                    output.Add(new Int3(x - 1, y - 0, z + 1));
                    output.Add(new Int3(x - 1, y + 1, z - 1));
                    output.Add(new Int3(x - 1, y + 1, z - 0));
                    output.Add(new Int3(x - 1, y + 1, z + 1));

                    output.Add(new Int3(x, y - 1, z - 1));
                    output.Add(new Int3(x, y - 1, z - 0));
                    output.Add(new Int3(x, y - 1, z + 1));
                    output.Add(new Int3(x, y - 0, z - 1));
                    //output.Add(new Int3(x, y - 0, z - 0)); <-- current cell
                    output.Add(new Int3(x, y - 0, z + 1));
                    output.Add(new Int3(x, y + 1, z - 1));
                    output.Add(new Int3(x, y + 1, z - 0));
                    output.Add(new Int3(x, y + 1, z + 1));

                    output.Add(new Int3(x + 1, y - 1, z - 1));
                    output.Add(new Int3(x + 1, y - 1, z - 0));
                    output.Add(new Int3(x + 1, y - 1, z + 1));
                    output.Add(new Int3(x + 1, y - 0, z - 1));
                    output.Add(new Int3(x + 1, y - 0, z - 0));
                    output.Add(new Int3(x + 1, y - 0, z + 1));
                    output.Add(new Int3(x + 1, y + 1, z - 1));
                    output.Add(new Int3(x + 1, y + 1, z - 0));
                    output.Add(new Int3(x + 1, y + 1, z + 1));

                    return output;
                }
            }

            public Dictionary<Int3, List<Point>> cells;

            public readonly float CellSize;

            public SearchablePointGrid(float cellsize)
            {
                cells = new Dictionary<Int3, List<Point>>();
                CellSize = cellsize;
            }

            public void AddPoint(Point p)
            {
                Int3 cell = GetCell(p.Position);
                if (!cells.ContainsKey(cell))
                    cells.Add(cell, new List<Point>());

                cells[cell].Add(p);
            }

            public Int3 GetCell(Vector3 pos)
            {
                var v = pos / CellSize;
                return new Int3
                {
                    x = (int)Mathf.Floor(v.x),
                    y = (int)Mathf.Floor(v.y),
                    z = (int)Mathf.Floor(v.z)
                };
            }

            public bool InRange(Int3 cell, Vector3 pos, float radius)
            {
                // Approx : our cell as a bounding sphere
                float r = CellSize * 0.866025f;
                Vector3 cellCenter = new Vector3(cell.x + 0.5f, cell.y + 0.5f, cell.z + 0.5f) * CellSize;
                return Vector3.Magnitude(cellCenter - pos) < (radius + r);
            }

            public List<Int3> GetStartCells(Vector3 pos)
            {
                var v = pos / CellSize;
                List<Int3> outCells = new List<Int3>();

                int i = (int)Mathf.Floor(v.x);
                int j = (int)Mathf.Floor(v.y);
                int k = (int)Mathf.Floor(v.z);

                bool fx = (v.x % 1.0f) > 0.5f;
                bool fy = (v.y % 1.0f) > 0.5f;
                bool fz = (v.z % 1.0f) > 0.5f;

                outCells.Add(new Int3 { x = i, y = j, z = k });
                outCells.Add(new Int3 { x = (fx ? i + 1 : i - 1), y = j, z = k });
                outCells.Add(new Int3 { x = i, y = (fy ? j + 1 : j - 1), z = k });
                outCells.Add(new Int3 { x = i, y = j, z = (fz ? k + 1 : k - 1) });
                return outCells;
            }

            public IEnumerable<PointSearchResult> Search(Vector3 position, float radius)
            {
                List<PointSearchResult> results = new List<PointSearchResult>();

                Queue<Int3> searchqueue = new Queue<Int3>();
                Queue<Int3> usedqueue = new Queue<Int3>();

                var startCells = GetStartCells(position);
                foreach (var n in startCells)
                {
                    searchqueue.Enqueue(n);
                }

                while (searchqueue.Count > 0)
                {
                    //  Empty the current iteration queue
                    while (searchqueue.Count > 0)
                    {
                        Int3 cell = searchqueue.Dequeue();

                        // If the cell does not exists, early out then go to next cell in queue
                        if (!cells.ContainsKey(cell))
                        {
                            usedqueue.Enqueue(cell);
                            continue;
                        }

                        // Add all points within range
                        List<Point> current = cells[cell];
                        foreach (Point p in current)
                        {
                            if (p.Position != position)
                            {
                                float currentDist = Vector3.Distance(p.Position, position);

                                if (currentDist < radius)
                                {
                                    var result = new PointSearchResult
                                    {
                                        Distance = currentDist,
                                        Point = p
                                    };
                                    results.Add(result);
                                }
                            }
                        }
                        usedqueue.Enqueue(cell);
                    }

                    // Prepare Next iteration
                    foreach (var cell in usedqueue)
                    {
                        var next = cell.GetAdjacentCells();
                        foreach (Int3 c in next)
                        {
                            if (usedqueue.Contains(c))
                                continue;

                            if (InRange(c, position, radius))
                                searchqueue.Enqueue(c);
                        }
                    }
                }
                return results;
            }
        }

        #endregion

        #region PRIVATE_API

        private Point AddPoint(Vector3 pos)
        {
            var point = m_Points.FirstOrDefault((p) => p.Position == pos);

            if (point == null)
            {
                point = new Point(pos);
                m_Points.Add(point);
            }
            return point;
        }

        private Vertex AddVertex(Vector3 position, Vector3 normal, Vector4 tangent, Color color, Vector4 uv, Vector4 uv2, Vector4 uv3, Vector4 uv4)
        {
            Point point = AddPoint(position);
            Vertex vertex = new Vertex(point, normal, tangent, color, uv, uv2, uv3, uv4);
            m_Vertices.Add(vertex);
            return vertex;
        }

        private Edge AddEdge(Vertex A, Vertex B)
        {
            var edge = m_Edges.FirstOrDefault((e) => e.Contains(A, B));

            if (edge == null)
            {
                edge = new Edge(A, B);
                m_Edges.Add(edge);
                A.RegisterEdge(edge);
                B.RegisterEdge(edge);
            }

            return edge;
        }

        private Primitive AddPrimitive(Vertex A, Vertex B, Vertex C, int MeshID)
        {
            var prim = m_Primitives.FirstOrDefault((p) => p.ContainsOrdered(A, B, C));

            if (prim == null)
            {
                Edge E = AddEdge(A, B);
                Edge F = AddEdge(B, C);
                Edge G = AddEdge(C, A);

                prim = new Primitive(A, B, C, E, F, G, MeshID);
                m_Primitives.Add(prim);
            }

            return prim;
        }

        #endregion

        #region GEOMETRY CLASSES
        /// <summary>
        /// Structuve for vertex connectivity
        /// </summary>
        public class Edge
        {
            public Vertex A;
            public Vertex B;

            public List<Primitive> Primitives;

            public Edge(Vertex a, Vertex b)
            {
                if (a.GetHashCode() < b.GetHashCode())
                {
                    A = a;
                    B = b;
                }
                else
                {
                    A = b;
                    B = a;
                }
                Primitives = new List<Primitive>();
            }

            /// <summary>
            /// Returns the vertex reference at the other end of the edge
            /// </summary>
            /// <param name="source"></param>
            /// <returns></returns>
            public Vertex Next(Vertex source)
            {
                if (source == A)
                    return B;
                else if (source == B)
                    return A;
                else return null;
            }

            /// <summary>
            /// Returns whether the vertex is one of the two vertices of the edge
            /// </summary>
            /// <param name="vertex"></param>
            /// <returns></returns>
            public bool Contains(Vertex vertex)
            {
                return (vertex == A || vertex == B);
            }

            /// <summary>
            /// Returns whether both of the points define the edge
            /// </summary>
            /// <param name="one"></param>
            /// <param name="other"></param>
            /// <returns></returns>
            public bool Contains(Vertex one, Vertex other)
            {
                return ((one == A && other == B) || (other == A && one == B));
            }

            public void RegisterPrimitive(Primitive p)
            {
                if (!Primitives.Contains(p))
                    Primitives.Add(p);
            }

            /// <summary>
            /// Returns Edge length
            /// </summary>
            /// <returns></returns>
            public float Length()
            {
                return Vector3.Distance(A.Position, B.Position);
            }
        }

        /// <summary>
        /// Structure for a Point, which is a local referencer for multiple vertices.
        /// </summary>
        public class Point
        {
            public Vector3 Position;
            public List<Vertex> Vertices;

            public Point(Vector3 position)
            {
                Position = position;
                Vertices = new List<Vertex>();
            }

            public void AddVertex(Vertex v)
            {
                if (!Vertices.Contains(v))
                    Vertices.Add(v);
            }

            /// <summary>
            /// Returs all connected vertices to this point(using point connectivity)
            /// </summary>
            /// <param name="depth">search depth</param>
            /// <returns></returns>
            public List<Vertex> GetNeighbours(int depth = 0)
            {
                List<Vertex> neighbours = new List<Vertex>();
                foreach (Vertex v in Vertices)
                {
                    v.AddToConnectedVertexList(neighbours, depth);
                }
                return neighbours;
            }

            /// <summary>
            /// Returns (if any) the first connecting edge between the two points
            /// </summary>
            /// <param name="v"></param>
            /// <returns></returns>
            public Edge GetEdgeTo(Vertex v)
            {
                foreach (var vert in Vertices)
                {
                    var e = vert.GetEdgeTo(v);
                    if (e != null)
                        return e;
                }
                return null;
            }
        }

        /// <summary>
        /// Structure for vertex information
        /// </summary>
        public class Vertex
        {
            public Vector3 Position
            {
                get { return Point.Position; }
            }

            public Point Point;
            public Vector3 Normal;
            public Vector4 Tangent;
            public Color Color;
            public Vector4 UV;
            public Vector4 UV2;
            public Vector4 UV3;
            public Vector4 UV4;
            public List<Primitive> InPrimitives;
            public List<Edge> InEdges;

            public Vertex(Point point, Vector3 normal, Vector4 tangent, Color color, Vector4 uv, Vector4 uv2, Vector4 uv3, Vector4 uv4)
            {
                Point = point;
                Normal = normal;
                Tangent = tangent;
                Color = color;
                UV = uv;
                UV2 = uv2;
                UV3 = uv3;
                UV4 = uv4;
                InPrimitives = new List<Primitive>();
                InEdges = new List<Edge>();
                point.AddVertex(this);
            }

            public void RegisterPrimitive(Primitive p)
            {
                if (!InPrimitives.Contains(p))
                    InPrimitives.Add(p);
            }

            public void RegisterEdge(Edge e)
            {
                if (!InEdges.Contains(e))
                    InEdges.Add(e);
            }

            /// <summary>
            /// Pass a vertex list to feed connected neighbours (with depth)
            /// </summary>
            /// <param name="vertices">list of vertices</param>
            /// <param name="depth"></param>
            public void AddToConnectedVertexList(List<Vertex> vertices, int depth = 0)
            {
                foreach (Edge e in InEdges)
                {
                    Vertex next = e.Next(this);
                    if (!vertices.Contains(next))
                        vertices.Add(next);
                    if (depth > 0)
                        next.AddToConnectedVertexList(vertices, depth - 1);
                }
            }

            /// <summary>
            /// Returns a list containing all connected neighbours vertices with depth
            /// </summary>
            /// <param name="depth"></param>
            /// <returns></returns>
            public List<Vertex> GetConnectedVertices(int depth = 0)
            {
                List<Vertex> neighbours = new List<Vertex>();
                AddToConnectedVertexList(neighbours, depth);
                return neighbours;
            }

            /// <summary>
            /// Returns an edge to another vertex (if found in connectivity)
            /// </summary>
            /// <param name="target">Target Vertex</param>
            /// <returns></returns>
            public Edge GetEdgeTo(Vertex target)
            {
                return InEdges.FirstOrDefault((e) => e.Next(this) == target);
            }
        }

        /// <summary>
        /// Reference class for a Triangle (3 points, 3 edges)
        /// </summary>
        public class Primitive
        {
            public int MeshID;

            public Vertex A;
            public Vertex B;
            public Vertex C;

            public Edge E;
            public Edge F;
            public Edge G;

            public Primitive(Vertex a, Vertex b, Vertex c, Edge e, Edge f, Edge g, int meshID)
            {
                A = a;
                B = b;
                C = c;
                E = e;
                F = f;
                G = g;
                MeshID = meshID;
            }

            public bool Contains(Vertex vertex)
            {
                return (vertex == A || vertex == B || vertex == C);
            }

            public bool ContainsOrdered(Vertex a, Vertex b, Vertex c)
            {
                return (a == A && b == B && c == C);
            }

            public bool Contains(Edge edge)
            {
                return (edge == E || edge == F || edge == G);
            }

            public Primitive Backface
            {
                get { return new Primitive(A, C, B, E, F, G, MeshID); }
            }

            public float Area()
            {
                float a = E.Length();
                float b = F.Length();
                float c = G.Length();
                float s = (a + b + c) / 2;
                return Mathf.Sqrt(s * (s - a) * (s - b) * (s - c));
            }

            public List<Primitive> GetNeighbours(bool sameMeshID = false)
            {
                Primitive me = this;
                List<Primitive> result = new List<Primitive>();
                result.AddRange(E.Primitives.Where((other) => other != this && (sameMeshID ? (other.MeshID == MeshID) : true)));
                result.AddRange(F.Primitives.Where((other) => other != this && (sameMeshID ? (other.MeshID == MeshID) : true)));
                result.AddRange(G.Primitives.Where((other) => other != this && (sameMeshID ? (other.MeshID == MeshID) : true)));
                return result;
            }
        }

        #endregion
    }
}
