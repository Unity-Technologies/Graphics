using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    [GenerateHLSL]
    public enum DebugProbeShadingMode
    {
        SH,
        Validity,
        ValidityOverDilationThreshold,
        Size
    }

    class ProbeVolumeDebug
    {
        public bool drawProbes;
        public bool drawBricks;
        public bool drawCells;
        public bool realtimeSubdivision;
        public int subdivisionCellUpdatePerFrame = 4;
        public float subdivisionDelayInSeconds = 1;
        public DebugProbeShadingMode probeShading;
        public float probeSize = 1.0f;
        public float subdivisionViewCullingDistance = 500.0f;
        public float probeCullingDistance = 200.0f;
        public int maxSubdivToVisualize = ProbeBrickIndex.kMaxSubdivisionLevels;
        public float exposureCompensation;
    }

    public partial class ProbeReferenceVolume
    {
        class CellInstancedDebugProbes
        {
            public List<Matrix4x4[]> probeBuffers;
            public List<MaterialPropertyBlock> props;
            public Hash128 cellHash;
            public Vector3 cellPosition;
        }

        const int kProbesPerBatch = 1023;

        internal ProbeVolumeDebug debugDisplay { get; } = new ProbeVolumeDebug();

        /// <summary>Colors that can be used for debug visualization of the brick structure subdivision.</summary>
        public Color[] subdivisionDebugColors { get; } = new Color[ProbeBrickIndex.kMaxSubdivisionLevels];

        DebugUI.Widget[] m_DebugItems;
        Mesh m_DebugMesh;
        Material m_DebugMaterial;
        List<CellInstancedDebugProbes> m_CellDebugData = new List<CellInstancedDebugProbes>();
        Plane[] m_DebugFrustumPlanes = new Plane[6];

        internal float dilationValidtyThreshold = 0.25f; // We ned to store this here to access it

        // Field used for the realtime subdivision preview
        internal Dictionary<ProbeReferenceVolume.Volume, List<ProbeBrickIndex.Brick>> realtimeSubdivisionInfo = new Dictionary<ProbeReferenceVolume.Volume, List<ProbeBrickIndex.Brick>>();

        /// <summary>
        /// Render Probe Volume related debug
        /// </summary>
        public void RenderDebug(Camera camera)
        {
            if (camera.cameraType != CameraType.Reflection && camera.cameraType != CameraType.Preview)
            {
                if (debugDisplay.drawProbes)
                {
                    DrawProbeDebug(camera);
                }
            }
        }

        void InitializeDebug(Mesh debugProbeMesh, Shader debugProbeShader)
        {
            m_DebugMesh = debugProbeMesh;
            m_DebugMaterial = CoreUtils.CreateEngineMaterial(debugProbeShader);
            m_DebugMaterial.enableInstancing = true;

            // Hard-coded colors for now.
            Debug.Assert(ProbeBrickIndex.kMaxSubdivisionLevels == 7); // Update list if this changes.
            subdivisionDebugColors[0] = new Color(1.0f, 0.0f, 0.0f);
            subdivisionDebugColors[1] = new Color(0.0f, 1.0f, 0.0f);
            subdivisionDebugColors[2] = new Color(0.0f, 0.0f, 1.0f);
            subdivisionDebugColors[3] = new Color(1.0f, 1.0f, 0.0f);
            subdivisionDebugColors[4] = new Color(1.0f, 0.0f, 1.0f);
            subdivisionDebugColors[5] = new Color(0.0f, 1.0f, 1.0f);
            subdivisionDebugColors[6] = new Color(0.5f, 0.5f, 0.5f);

            RegisterDebug();

#if UNITY_EDITOR
            UnityEditor.Lightmapping.lightingDataCleared += OnClearLightingdata;
#endif
        }

        void CleanupDebug()
        {
            UnregisterDebug(true);
            CoreUtils.Destroy(m_DebugMaterial);

#if UNITY_EDITOR
            UnityEditor.Lightmapping.lightingDataCleared -= OnClearLightingdata;
#endif
        }

        void RefreshDebug<T>(DebugUI.Field<T> field, T value)
        {
            UnregisterDebug(false);
            RegisterDebug();
        }

        void DebugCellIndexChanged<T>(DebugUI.Field<T> field, T value)
        {
            ClearDebugData();
        }

        void RegisterDebug()
        {
            var widgetList = new List<DebugUI.Widget>();

            var subdivContainer = new DebugUI.Container() { displayName = "Subdivision Visualization" };
            subdivContainer.children.Add(new DebugUI.BoolField { displayName = "Display Cells", getter = () => debugDisplay.drawCells, setter = value => debugDisplay.drawCells = value, onValueChanged = RefreshDebug });
            subdivContainer.children.Add(new DebugUI.BoolField { displayName = "Display Bricks", getter = () => debugDisplay.drawBricks, setter = value => debugDisplay.drawBricks = value, onValueChanged = RefreshDebug });
#if UNITY_EDITOR
            subdivContainer.children.Add(new DebugUI.BoolField { displayName = "Realtime Update", getter = () => debugDisplay.realtimeSubdivision, setter = value => debugDisplay.realtimeSubdivision = value, onValueChanged = RefreshDebug });
            if (debugDisplay.realtimeSubdivision)
            {
                var cellUpdatePerFrame = new DebugUI.IntField { displayName = "Number Of Cell Update Per Frame", getter = () => debugDisplay.subdivisionCellUpdatePerFrame, setter = value => debugDisplay.subdivisionCellUpdatePerFrame = value, min = () => 1, max = () => 100 };
                var delayBetweenUpdates = new DebugUI.FloatField { displayName = "Delay Between Two Updates In Seconds", getter = () => debugDisplay.subdivisionDelayInSeconds, setter = value => debugDisplay.subdivisionDelayInSeconds = value, min = () => 0.1f, max = () => 10 };
                subdivContainer.children.Add(new DebugUI.Container { children = { cellUpdatePerFrame, delayBetweenUpdates } });
            }
#endif

            if (debugDisplay.drawCells || debugDisplay.drawBricks)
            {
                subdivContainer.children.Add(new DebugUI.FloatField { displayName = "Culling Distance", getter = () => debugDisplay.subdivisionViewCullingDistance, setter = value => debugDisplay.subdivisionViewCullingDistance = value, min = () => 0.0f });
            }

            var probeContainer = new DebugUI.Container() { displayName = "Probe Visualization" };
            probeContainer.children.Add(new DebugUI.BoolField { displayName = "Display Probes", getter = () => debugDisplay.drawProbes, setter = value => debugDisplay.drawProbes = value, onValueChanged = RefreshDebug });
            if (debugDisplay.drawProbes)
            {
                probeContainer.children.Add(new DebugUI.EnumField
                {
                    displayName = "Probe Shading Mode",
                    getter = () => (int)debugDisplay.probeShading,
                    setter = value => debugDisplay.probeShading = (DebugProbeShadingMode)value,
                    autoEnum = typeof(DebugProbeShadingMode),
                    getIndex = () => (int)debugDisplay.probeShading,
                    setIndex = value => debugDisplay.probeShading = (DebugProbeShadingMode)value,
                    onValueChanged = RefreshDebug
                });
                probeContainer.children.Add(new DebugUI.FloatField { displayName = "Probe Size", getter = () => debugDisplay.probeSize, setter = value => debugDisplay.probeSize = value, min = () => 0.1f, max = () => 10.0f });
                if (debugDisplay.probeShading == DebugProbeShadingMode.SH)
                    probeContainer.children.Add(new DebugUI.FloatField { displayName = "Probe Exposure Compensation", getter = () => debugDisplay.exposureCompensation, setter = value => debugDisplay.exposureCompensation = value });

                probeContainer.children.Add(new DebugUI.FloatField { displayName = "Culling Distance", getter = () => debugDisplay.probeCullingDistance, setter = value => debugDisplay.probeCullingDistance = value, min = () => 0.0f });

                probeContainer.children.Add(new DebugUI.IntField
                {
                    displayName = "Max subdivision displayed",
                    getter = () => debugDisplay.maxSubdivToVisualize,
                    setter = (v) => debugDisplay.maxSubdivToVisualize = Mathf.Min(v, ProbeReferenceVolume.instance.GetMaxSubdivision()),
                    min = () => 0,
                    max = () => ProbeReferenceVolume.instance.GetMaxSubdivision(),
                });
            }

            widgetList.Add(subdivContainer);
            widgetList.Add(probeContainer);

            m_DebugItems = widgetList.ToArray();
            var panel = DebugManager.instance.GetPanel("Probe Volume", true);
            panel.children.Add(m_DebugItems);
        }

        void UnregisterDebug(bool destroyPanel)
        {
            if (destroyPanel)
                DebugManager.instance.RemovePanel("Probe Volume");
            else
                DebugManager.instance.GetPanel("Probe Volume", false).children.Remove(m_DebugItems);
        }

        bool ShouldCullCell(Vector3 cellPosition, Transform cameraTransform, Plane[] frustumPlanes)
        {
            var cellSize = MaxBrickSize();
            var originWS = GetTransform().posWS;
            Vector3 cellCenterWS = cellPosition * cellSize + originWS + Vector3.one * (cellSize / 2.0f);

            // We do coarse culling with cell, finer culling later.
            float distanceRoundedUpWithCellSize = Mathf.CeilToInt(debugDisplay.probeCullingDistance / cellSize) * cellSize;

            if (Vector3.Distance(cameraTransform.position, cellCenterWS) > distanceRoundedUpWithCellSize)
                return true;

            var volumeAABB = new Bounds(cellCenterWS, cellSize * Vector3.one);

            return !GeometryUtility.TestPlanesAABB(frustumPlanes, volumeAABB);
        }

        void DrawProbeDebug(Camera camera)
        {
            if (debugDisplay.drawProbes)
            {
                // TODO: Update data on ref vol changes
                if (m_CellDebugData.Count == 0)
                    CreateInstancedProbes();

                GeometryUtility.CalculateFrustumPlanes(camera, m_DebugFrustumPlanes);

                m_DebugMaterial.shaderKeywords = null;
                if (m_SHBands == ProbeVolumeSHBands.SphericalHarmonicsL1)
                    m_DebugMaterial.EnableKeyword("PROBE_VOLUMES_L1");
                else if (m_SHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                    m_DebugMaterial.EnableKeyword("PROBE_VOLUMES_L2");

                foreach (var debug in m_CellDebugData)
                {
                    if (ShouldCullCell(debug.cellPosition, camera.transform, m_DebugFrustumPlanes))
                        continue;

                    for (int i = 0; i < debug.probeBuffers.Count; ++i)
                    {
                        var probeBuffer = debug.probeBuffers[i];
                        var props = debug.props[i];
                        props.SetInt("_ShadingMode", (int)debugDisplay.probeShading);
                        props.SetFloat("_ExposureCompensation", debugDisplay.exposureCompensation);
                        props.SetFloat("_ProbeSize", debugDisplay.probeSize);
                        props.SetFloat("_CullDistance", debugDisplay.probeCullingDistance);
                        props.SetInt("_MaxAllowedSubdiv", debugDisplay.maxSubdivToVisualize);
                        props.SetFloat("_ValidityThreshold", dilationValidtyThreshold);

                        Graphics.DrawMeshInstanced(m_DebugMesh, 0, m_DebugMaterial, probeBuffer, probeBuffer.Length, props, ShadowCastingMode.Off, false, 0, camera, LightProbeUsage.Off, null);
                    }
                }
            }
        }

        void ClearDebugData()
        {
            m_CellDebugData.Clear();
            realtimeSubdivisionInfo.Clear();
        }

        void CreateInstancedProbes()
        {
            int maxSubdiv = ProbeReferenceVolume.instance.GetMaxSubdivision() - 1;
            foreach (var cell in ProbeReferenceVolume.instance.cells.Values)
            {
                if (cell.sh == null || cell.sh.Length == 0)
                    continue;

                float largestBrickSize = cell.bricks.Count == 0 ? 0 : cell.bricks[0].subdivisionLevel;
                List<Matrix4x4[]> probeBuffers = new List<Matrix4x4[]>();
                List<MaterialPropertyBlock> props = new List<MaterialPropertyBlock>();
                CellChunkInfo chunks;
                if (!m_ChunkInfo.TryGetValue(cell.index, out chunks))
                    continue;

                Vector4[] texels = new Vector4[kProbesPerBatch];
                float[] validity = new float[kProbesPerBatch];
                float[] relativeSize = new float[kProbesPerBatch];

                List<Matrix4x4> probeBuffer = new List<Matrix4x4>();

                var debugData = new CellInstancedDebugProbes();
                debugData.probeBuffers = probeBuffers;
                debugData.props = props;
                debugData.cellPosition = cell.position;

                int idxInBatch = 0;
                for (int i = 0; i < cell.probePositions.Length; i++)
                {
                    var brickSize = cell.bricks[i / 64].subdivisionLevel;

                    int chunkIndex = i / m_Pool.GetChunkSizeInProbeCount();
                    var chunk = chunks.chunks[chunkIndex];
                    int indexInChunk = i % m_Pool.GetChunkSizeInProbeCount();
                    int brickIdx = indexInChunk / 64;
                    int indexInBrick = indexInChunk % 64;

                    Vector2Int brickStart = new Vector2Int(chunk.x + brickIdx * 4, chunk.y);
                    int indexInSlice = indexInBrick % 16;
                    Vector3Int texelLoc = new Vector3Int(brickStart.x + (indexInSlice % 4), brickStart.y + (indexInSlice / 4), indexInBrick / 16);

                    probeBuffer.Add(Matrix4x4.TRS(cell.probePositions[i], Quaternion.identity, Vector3.one * (0.3f * (brickSize + 1))));
                    validity[idxInBatch] = cell.validity[i];
                    texels[idxInBatch] = new Vector4(texelLoc.x, texelLoc.y, texelLoc.z, brickSize);
                    relativeSize[idxInBatch] = (float)brickSize / (float)maxSubdiv;
                    idxInBatch++;

                    if (probeBuffer.Count >= kProbesPerBatch || i == cell.probePositions.Length - 1)
                    {
                        idxInBatch = 0;
                        MaterialPropertyBlock prop = new MaterialPropertyBlock();

                        prop.SetFloatArray("_Validity", validity);
                        prop.SetFloatArray("_RelativeSize", relativeSize);
                        prop.SetVectorArray("_IndexInAtlas", texels);

                        props.Add(prop);

                        probeBuffers.Add(probeBuffer.ToArray());
                        probeBuffer = new List<Matrix4x4>();
                    }
                }

                m_CellDebugData.Add(debugData);
            }
        }

        void OnClearLightingdata()
        {
            ClearDebugData();
        }
    }
}
