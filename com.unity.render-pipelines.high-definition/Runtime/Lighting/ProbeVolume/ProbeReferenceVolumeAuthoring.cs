using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Rendering.HighDefinition
{ 
    [ExecuteAlways]
    [AddComponentMenu("Light/Experimental/Probe Reference Volume")]
    public class ProbeReferenceVolumeAuthoring : MonoBehaviour
    {
        // debug gizmo data
        class CellInstancedDebugProbes
        {
            public List<Matrix4x4[]> probeBuffers;
            public List<MaterialPropertyBlock> props;
            public List<int[]> probeMaps;
            public Hash128 cellHash;
        }

        private const int probesPerBatch = 1023;
        private Mesh debugMesh;
        private Material debugMaterial;
        private List<CellInstancedDebugProbes> cellDebugData = new List<CellInstancedDebugProbes>();

        // settings
        public enum BrickSizeMode
        {
            Length,
            Density
        }

        public enum ProbeShadingMode
        {
            Size,
            SH,
            Validity
        }

        public int CellSize = 64;
        public BrickSizeMode SizeMode;
        public float BrickSize = 4;
        public int MaxSubdivision = 2;

        public bool DrawProbes = false;
        public bool DrawBricks = false;
        public bool DrawCells = false;
        public ProbeShadingMode ProbeShading;
        public float CullingDistance = 200;

        public float Exposure = 0f;
        public float NormalBias = 0.2f;

        public bool Dilate = false;
        public int MaxDilationSamples = 16;
        public float MaxDilationSampleDistance = 1f;
        public float DilationValidityThreshold = 0.25f;
        public bool GreedyDilation = false;

        public ProbeVolumeAsset VolumeAsset = null;

        protected void Start()
        {
            CheckInit();
        }

        void Update()
        {
            UpdateCulling();
        }

        private void UpdateCulling()
        {
            /*
            var ecb = ecbSystem.CreateCommandBuffer().ToConcurrent();

            // Add culling component to cells which lack it
            Entities
                .WithAll<CellMetadata>()
                .WithNone<CellDebugCulling>()
                .ForEach((Entity entity, int entityInQueryIndex) => ecb.AddComponent<CellDebugCulling>(entityInQueryIndex, entity))
                .ScheduleParallel();

            // Prepare fields that are global to the job
            var settingsEntity = GetSingletonEntity<ReferenceVolumeSettings>();
            var settings = EntityManager.GetComponentData<ReferenceVolumeSettings>(settingsEntity);
            var refVolTranslation = EntityManager.GetComponentData<Translation>(settingsEntity);
            var refVolRotation = EntityManager.GetComponentData<Rotation>(settingsEntity);
            Transform cam = SceneView.lastActiveSceneView.camera.transform;
            Vector3 camPos = cam.position;
            Vector3 camVec = cam.forward;
            float halfCellSize = settings.CellSize / 2;

            // Update culling flags
            Entities.ForEach((Entity entity, int entityInQueryIndex, ref CellDebugCulling culling, in CellMetadata cell) =>
            {
                Vector3 cellPos = new Vector3(cell.Position.x, cell.Position.y, cell.Position.z) * settings.CellSize + settings.CellSize * 0.5f * Vector3.one;

                Vector3 camToCell = cellPos - camPos;
                float angle = Vector3.Angle(camVec, camToCell);

                culling.shouldRender = (camToCell.magnitude < settings.CullingDistance && angle <= 90) ||
                    (Mathf.Abs(camToCell.x) < halfCellSize && Mathf.Abs(camToCell.y) < halfCellSize && Mathf.Abs(camToCell.z) < halfCellSize);
            }).ScheduleParallel();

            ecbSystem.AddJobHandleForProducer(Dependency);
            */
        }

        private void CreateInstancedProbes()
        {
            foreach (var cell in ProbeReferenceVolume.instance.cells)
            {
                float largestBrickSize = cell.bricks.Count == 0 ? 0 : cell.bricks[0].size;
                
                List<Matrix4x4[]> probeBuffers = new List<Matrix4x4[]>();
                List<MaterialPropertyBlock> props = new List<MaterialPropertyBlock>();
                List<int[]> probeMaps = new List<int[]>();

                // Batch probes for instanced rendering
                for (int brickSize = 0; brickSize < largestBrickSize + 1; brickSize++)
                {
                    List<Matrix4x4> probeBuffer = new List<Matrix4x4>();
                    List<int> probeMap = new List<int>();

                    for (int i = 0; i < cell.probePositions.Length; i++)
                    {
                        // Skip probes which aren't of current brick size
                        if (cell.bricks[i / 64].size == brickSize)
                        {
                            probeBuffer.Add(Matrix4x4.TRS(cell.probePositions[i], Quaternion.identity, Vector3.one * (0.3f * (brickSize + 1))));
                            probeMap.Add(i);
                        }

                        // Batch limit reached or out of probes
                        if (probeBuffer.Count >= probesPerBatch || i == cell.probePositions.Length - 1)
                        {
                            MaterialPropertyBlock prop = new MaterialPropertyBlock();
                            float gradient = largestBrickSize == 0 ? 1 : brickSize / largestBrickSize;
                            prop.SetColor("_Color", Color.Lerp(Color.red, Color.green, gradient));
                            props.Add(prop);

                            probeBuffers.Add(probeBuffer.ToArray());
                            probeBuffer = new List<Matrix4x4>();
                            probeMaps.Add(probeMap.ToArray());
                            probeMap = new List<int>();
                        }
                    }
                }
            
                var debugData = new CellInstancedDebugProbes();
                debugData.probeBuffers = probeBuffers;
                debugData.props = props;
                debugData.probeMaps = probeMaps;

                Vector4[][] shBuffer = new Vector4[4][];
                for (int i = 0; i < shBuffer.Length; i++)
                    shBuffer[i] = new Vector4[probesPerBatch];

                Vector4[] validityColors = new Vector4[probesPerBatch];

                for (int batchIndex = 0; batchIndex < debugData.probeMaps.Count; batchIndex++)
                {
                    for (int indexInBatch = 0; indexInBatch < debugData.probeMaps[batchIndex].Length; indexInBatch++)
                    {
                        int probeIdx = debugData.probeMaps[batchIndex][indexInBatch];

                        shBuffer[0][indexInBatch] = cell.sh[probeIdx].shAr;
                        shBuffer[1][indexInBatch] = cell.sh[probeIdx].shAg;
                        shBuffer[2][indexInBatch] = cell.sh[probeIdx].shAb;

                        validityColors[indexInBatch] = Color.Lerp(Color.green, Color.red, cell.validity[probeIdx]);
                    }

                    debugData.props[batchIndex].SetVectorArray("_R", shBuffer[0]);
                    debugData.props[batchIndex].SetVectorArray("_G", shBuffer[1]);
                    debugData.props[batchIndex].SetVectorArray("_B", shBuffer[2]);

                    debugData.props[batchIndex].SetVectorArray("_Validity", validityColors);
                }

                cellDebugData.Add(debugData);
            }
        }

        private void CleanCachedProbeData()
        {
            //(Entity e, CellMetadata metadata, CellInstancedDebugProbes cached)
            //if (metadata.Hash != cached.cellHash)
            cellDebugData = new List<CellInstancedDebugProbes>();
        }

        private void DrawInstancedProbes(ProbeShadingMode shading, float exposure)
        {
            foreach (var debug in cellDebugData)
            {
                //if (!culling.shouldRender)
                  //  return;

                for (int i = 0; i < debug.probeBuffers.Count; ++i)
                {
                    var probeBuffer = debug.probeBuffers[i];
                    var props = debug.props[i];
                    props.SetInt("_ShadingMode", (int)shading);
                    props.SetFloat("_Exposure", -exposure);
                    props.SetFloat("_ProbeSize", Gizmos.probeSize);
                    Graphics.DrawMeshInstanced(debugMesh, 0, debugMaterial, probeBuffer, probeBuffer.Length, props, ShadowCastingMode.Off, false, 0, null, LightProbeUsage.Off, null);
                }
            }
        }

        public void OnDrawGizmos()
        {
            if (DrawCells)
            {
                // Fetching this from components instead of from the reference volume allows the user to
                // preview how cells will look before they commit to a bake.
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
                Gizmos.color = Color.green;
 
                foreach (var cell in ProbeReferenceVolume.instance.cells)
                {
                    var positionF = new Vector3(cell.position.x, cell.position.y, cell.position.z);
                    var center = positionF * CellSize + CellSize * 0.5f * Vector3.one;
                    Gizmos.DrawWireCube(center, Vector3.one * CellSize);
                }
            }

            if (DrawBricks)
            {
                Gizmos.matrix = ProbeReferenceVolume.instance.GetRefSpaceToWS();
                Gizmos.color = Color.blue;

                // Read refvol transform
                foreach (var cell in ProbeReferenceVolume.instance.cells)
                {
                    //if (!culling.shouldRender)
                    //    return;

                    foreach (var brick in cell.bricks)
                    {
                        Vector3 scaledSize = Vector3.one * Mathf.Pow(3, brick.size);
                        Vector3 scaledPos = brick.position + scaledSize / 2;
                        Gizmos.DrawWireCube(scaledPos, scaledSize);
                    }
                }
            }

            if (DrawProbes)
            {
                if (cellDebugData.Count == 0)
                    CreateInstancedProbes();

                DrawInstancedProbes(ProbeShading, Exposure);
            }
        }

        private void CheckInit()
        {
            if (debugMesh == null || debugMaterial == null)
            {
                // Load debug mesh, material
                debugMesh = Resources.Load<Mesh>("DebugProbe");
                debugMaterial = new Material(Shader.Find("APV/InstancedProbeShader")) { enableInstancing = true };
            }
        }
    }
}
