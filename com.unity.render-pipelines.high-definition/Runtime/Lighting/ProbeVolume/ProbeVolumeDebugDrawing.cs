using System.Collections.Generic;
using UnityEditor;
using System.Linq;

namespace UnityEngine.Rendering.HighDefinition
{
    public class ProbeVolumeDebugDrawing
    {
        private static ProbeVolumeDebugDrawing _instance;

        internal static ProbeVolumeDebugDrawing drawing
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ProbeVolumeDebugDrawing();
                }
                return _instance;
            }
        }

        // Settings
        public bool DrawProbes { get; set; }
        public bool DrawBricks { get; set; }
        public bool DrawReferenceVolume { get; set; }

        // Probe rendering
        private bool probesDirty;
        private Stack<ProbeBatch> instanceData;
        private Mesh debugMesh;

        private ProbeVolumeDebugDrawing()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            probesDirty = true;
        }

        ~ProbeVolumeDebugDrawing()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        public void Dirty()
        {
            probesDirty = true;
        }

        private void OnSceneGUI(SceneView sv)
        {
            var pos = ProbeVolumeManager.manager.positioning;

            if (DrawReferenceVolume)
            {
                Handles.color = Color.red;
                Handles.DrawWireCube(pos.ReferenceBounds.center, pos.ReferenceBounds.size);
            }
            if (DrawBricks && pos.Bricks != null)
            {
                Handles.color = Color.blue;
                foreach (ProbeVolumeBrick b in pos.Bricks)
                {
                    Vector3 scaledSize = new Vector3(b.size, b.size, b.size) * pos.MinCellSize;
                    Vector3 scaledPos = pos.GridToWorld(b.Position) + scaledSize / 2;

                    Handles.DrawWireCube(scaledPos, scaledSize);
                }
            }
            if (DrawProbes)
            {
                DrawInstancedProbes();
            }
        }

        private void DrawInstancedProbes()
        {
            if (probesDirty)
                UpdateInstancedProbes();

            if (instanceData == null)
                return;

            foreach (ProbeBatch probes in instanceData)
            {
                Graphics.DrawMeshInstanced(
                    debugMesh,
                    0,
                    probes.material,
                    probes.matrix.ToArray(),
                    probes.matrix.Count,
                    null,
                    ShadowCastingMode.Off,
                    false,
                    0,
                    null,
                    LightProbeUsage.Off,
                    null);
            }
        }

        private void UpdateInstancedProbes()
        {
            var pos = ProbeVolumeManager.manager.positioning;
            var probes = pos.ProbePositions;

            if (probes == null)
                return;

            if (debugMesh == null)
                debugMesh = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");

            float largestBrickSize = Mathf.Log(pos.Bricks.Max(x => x.size), 3);

            instanceData = new Stack<ProbeBatch>();
            for (int k = 0; k < (int)largestBrickSize + 1; k++)
            {
                int j = 0;
                for (int i = 0; i < probes.Length; i++)
                {
                    float size = Mathf.Log(pos.Bricks[i / 64].size, 3);
                    if (size != k)
                        continue;

                    if (j % 1023 == 0)
                    {
                        ProbeBatch batch = new ProbeBatch
                        {
                            matrix = new List<Matrix4x4>(),
                            material = new Material(Shader.Find("HDRP/Unlit")) { enableInstancing = true }
                        };
                        batch.material.SetColor("_UnlitColor", Color.Lerp(Color.red, Color.green, k / largestBrickSize));
                        instanceData.Push(batch);
                    }

                    Matrix4x4 mat = Matrix4x4.TRS(probes[i], Quaternion.identity, Vector3.one * (0.3f * (size + 1)));
                    instanceData.Peek().matrix.Add(mat);

                    j++;
                }
            }

            probesDirty = false;
        }

        // Struct for instanced rendering of probes
        private struct ProbeBatch
        {
            public List<Matrix4x4> matrix;
            public Material material;
        }
    }

    public class ProbeVolumeDebugDrawingWindow : EditorWindow
    {
        private void OnEnable()
        {
            this.titleContent.text = "Probe Volume Debug";
            this.maxSize = new Vector2(200, 100);
            this.minSize = this.maxSize;   
        }

        void OnGUI()
        {
            EditorGUI.BeginChangeCheck();

            var drawing = ProbeVolumeDebugDrawing.drawing;
            drawing.DrawProbes = EditorGUILayout.Toggle("Draw probes", drawing.DrawProbes);
            drawing.DrawBricks = EditorGUILayout.Toggle("Draw bricks", drawing.DrawBricks);
            drawing.DrawReferenceVolume = EditorGUILayout.Toggle("Draw reference volume", drawing.DrawReferenceVolume);

            var pos = ProbeVolumeManager.manager.positioning;
            pos.MinCellSize = EditorGUILayout.FloatField("Min cell size", pos.MinCellSize);
            if (pos.MinCellSize < 0.5f)
            {
                pos.MinCellSize = 0.5f;
            }

            if (GUILayout.Button("Build grid"))
            {
                ProbeVolumeManager.manager.positioning.BuildBrickStructure();
            }

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.lastActiveSceneView.Repaint();
            }
        }
     
        [MenuItem("Window/Rendering/Probe Volume Debug")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow<ProbeVolumeDebugDrawingWindow>();
        }
    }
}
