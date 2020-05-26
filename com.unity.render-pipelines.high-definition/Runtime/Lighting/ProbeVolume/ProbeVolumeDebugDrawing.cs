using System.Collections.Generic;
using UnityEditor;
using System.Linq;

namespace UnityEngine.Rendering.HighDefinition
{
    internal class ProbeVolumeDebugDrawing
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
        internal bool DrawProbes { get; set; }
        internal bool DrawBricks { get; set; }

        // Debug rendering
        private bool probesDirty;
        private Stack<ProbeBatch> instanceData;
        private Mesh debugMesh;
        private float largestBrickSize;

        private ProbeVolumeDebugDrawing()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            Dirty();
        }

        ~ProbeVolumeDebugDrawing()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        internal void Dirty()
        {
            probesDirty = true;

            var bricks = ProbeVolumeManager.manager.bricks;
            if (bricks != null && bricks.Count > 0)
                largestBrickSize = bricks.Max(x => x.size);
        }

        private void OnSceneGUI(SceneView sv)
        {
            var refVol = ProbeVolumeManager.manager.refVol;
            if (refVol == null)
                return;

            if (DrawBricks && ProbeVolumeManager.manager.bricks != null)
            {
                Handles.color = Color.blue;
                Handles.matrix = ProbeVolumeManager.manager.refVol.GetRefSpaceToWS();

                foreach (ProbeReferenceVolume.Brick b in ProbeVolumeManager.manager.bricks)
                {
                    Vector3 scaledSize = Vector3.one * Mathf.Pow(3, b.size);
                    Vector3 scaledPos = b.position + scaledSize / 2;

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
            var probePositions = ProbeVolumeManager.manager.probePositions;
            var bricks = ProbeVolumeManager.manager.bricks;

            if (debugMesh == null)
                debugMesh = Resources.GetBuiltinResource<Mesh>("icosphere.fbx");

            instanceData = new Stack<ProbeBatch>();
            for (int k = 0; k < (int)largestBrickSize + 1; k++)
            {
                int j = 0;
                for (int i = 0; i < probePositions.Length; i++)
                {
                    float size = bricks[i / 64].size;
                    if (size != k)
                        continue;

                    if (j % 1023 == 0)
                    {
                        ProbeBatch batch = new ProbeBatch
                        {
                            matrix = new List<Matrix4x4>(),
                            material = new Material(Shader.Find("HDRP/Unlit")) { enableInstancing = true }
                        };
                        if (largestBrickSize == 0)
                        {
                            batch.material.SetColor("_UnlitColor", Color.Lerp(Color.red, Color.green, 1));
                        }
                        else
                        {
                            batch.material.SetColor("_UnlitColor", Color.Lerp(Color.red, Color.green, k / largestBrickSize));
                        }
                        instanceData.Push(batch);
                    }

                    Matrix4x4 mat = Matrix4x4.TRS(probePositions[i], Quaternion.identity, Vector3.one * (0.3f * (size + 1)));
                    instanceData.Peek().matrix.Add(mat);

                    j++;
                }
            }

            probesDirty = false;
        }

        // Struct for instanced rendering of probes
        private struct ProbeBatch
        {
            internal List<Matrix4x4> matrix;
            internal Material material;
        }
    }

    internal class ProbeVolumeDebugDrawingWindow : EditorWindow
    {
        private float selectedMinCellSize;

        private void OnEnable()
        {
            this.titleContent.text = "Probe Volume Debug";
            this.maxSize = new Vector2(200, 100);
            this.minSize = this.maxSize;
            this.selectedMinCellSize = 5f;
        }

        void OnGUI()
        {
            EditorGUI.BeginChangeCheck();

            var drawing = ProbeVolumeDebugDrawing.drawing;
            drawing.DrawProbes = EditorGUILayout.Toggle("Draw probes", drawing.DrawProbes);
            drawing.DrawBricks = EditorGUILayout.Toggle("Draw bricks", drawing.DrawBricks);

            selectedMinCellSize = EditorGUILayout.FloatField("Min cell size", selectedMinCellSize);
            if (selectedMinCellSize < 0.5f)
            {
                selectedMinCellSize = 0.5f;
            }

            if (GUILayout.Button("Build grid"))
            {
                ProbeVolumeManager.manager.BuildBrickStructure(selectedMinCellSize);
                ProbeVolumeDebugDrawing.drawing.Dirty();
            }

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.lastActiveSceneView.Repaint();
            }
        }
     
        [MenuItem("Window/Rendering/Probe Volume Debug")]
        internal static void ShowWindow()
        {
            EditorWindow.GetWindow<ProbeVolumeDebugDrawingWindow>();
        }
    }
}
