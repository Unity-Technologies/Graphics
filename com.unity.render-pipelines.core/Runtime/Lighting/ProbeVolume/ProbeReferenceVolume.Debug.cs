using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    /// <summary>
    /// Modes for Debugging Probes
    /// </summary>
    [GenerateHLSL]
    public enum DebugProbeShadingMode
    {
        /// <summary>
        /// Based on Spherical Harmonics
        /// </summary>
        SH,
        /// <summary>
        /// Based on Spherical Harmonics first band only (ambient)
        /// </summary>
        SHL0,
        /// <summary>
        /// Based on Spherical Harmonics band zero and one only
        /// </summary>
        SHL0L1,
        /// <summary>
        /// Based on validity
        /// </summary>
        Validity,
        /// <summary>
        /// Based on validity over a dilation threshold
        /// </summary>
        ValidityOverDilationThreshold,
        /// <summary>
        /// Based on size
        /// </summary>
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
        public bool drawVirtualOffsetPush;
        public float offsetSize = 0.025f;
        public bool freezeStreaming;
    }

    public partial class ProbeReferenceVolume
    {
        internal class CellInstancedDebugProbes
        {
            public List<Matrix4x4[]> probeBuffers;
            public List<Matrix4x4[]> offsetBuffers;
            public List<MaterialPropertyBlock> props;
        }

        const int kProbesPerBatch = 511;

        /// <summary>Name of debug panel for Probe Volume</summary>
        public static readonly string k_DebugPanelName = "Probe Volume";

        internal ProbeVolumeDebug debugDisplay { get; } = new ProbeVolumeDebug();

        /// <summary>Colors that can be used for debug visualization of the brick structure subdivision.</summary>
        public Color[] subdivisionDebugColors { get; } = new Color[ProbeBrickIndex.kMaxSubdivisionLevels];

        DebugUI.Widget[] m_DebugItems;
        Mesh m_DebugMesh;
        Material m_DebugMaterial;
        Mesh m_DebugOffsetMesh;
        Material m_DebugOffsetMaterial;
        Plane[] m_DebugFrustumPlanes = new Plane[6];

        internal ProbeVolumeBakingProcessSettings bakingProcessSettings; /* DEFAULTS would be better but is implemented in PR#6174 = ProbeVolumeBakingProcessSettings.Defaults; */

        // Field used for the realtime subdivision preview
        internal Dictionary<ProbeReferenceVolume.Volume, List<ProbeBrickIndex.Brick>> realtimeSubdivisionInfo = new Dictionary<ProbeReferenceVolume.Volume, List<ProbeBrickIndex.Brick>>();

        /// <summary>
        ///  Render Probe Volume related debug
        /// </summary>
        /// <param name="camera">The <see cref="Camera"/></param>
        public void RenderDebug(Camera camera)
        {
            if (camera.cameraType != CameraType.Reflection && camera.cameraType != CameraType.Preview)
            {
                DrawProbeDebug(camera);
            }
        }

        void InitializeDebug(in ProbeVolumeSystemParameters parameters)
        {
            if (parameters.supportsRuntimeDebug)
            {
                m_DebugMesh = parameters.probeDebugMesh;
                m_DebugMaterial = CoreUtils.CreateEngineMaterial(parameters.probeDebugShader);
                m_DebugMaterial.enableInstancing = true;

                m_DebugOffsetMesh = parameters.offsetDebugMesh;
                m_DebugOffsetMaterial = CoreUtils.CreateEngineMaterial(parameters.offsetDebugShader);
                m_DebugOffsetMaterial.enableInstancing = true;

                // Hard-coded colors for now.
                Debug.Assert(ProbeBrickIndex.kMaxSubdivisionLevels == 7); // Update list if this changes.
                subdivisionDebugColors[0] = new Color(1.0f, 0.0f, 0.0f);
                subdivisionDebugColors[1] = new Color(0.0f, 1.0f, 0.0f);
                subdivisionDebugColors[2] = new Color(0.0f, 0.0f, 1.0f);
                subdivisionDebugColors[3] = new Color(1.0f, 1.0f, 0.0f);
                subdivisionDebugColors[4] = new Color(1.0f, 0.0f, 1.0f);
                subdivisionDebugColors[5] = new Color(0.0f, 1.0f, 1.0f);
                subdivisionDebugColors[6] = new Color(0.5f, 0.5f, 0.5f);
            }

            RegisterDebug(parameters);

#if UNITY_EDITOR
            UnityEditor.Lightmapping.lightingDataCleared += OnClearLightingdata;
#endif
        }

        void CleanupDebug()
        {
            UnregisterDebug(true);
            CoreUtils.Destroy(m_DebugMaterial);
            CoreUtils.Destroy(m_DebugOffsetMaterial);

#if UNITY_EDITOR
            UnityEditor.Lightmapping.lightingDataCleared -= OnClearLightingdata;
#endif
        }

        void DebugCellIndexChanged<T>(DebugUI.Field<T> field, T value)
        {
            ClearDebugData();
        }

        void RegisterDebug(ProbeVolumeSystemParameters parameters)
        {
            void RefreshDebug<T>(DebugUI.Field<T> field, T value)
            {
                UnregisterDebug(false);
                RegisterDebug(parameters);
            }

            const float kProbeSizeMin = 0.05f, kProbeSizeMax = 10.0f;
            const float kOffsetSizeMin = 0.001f, kOffsetSizeMax = 0.1f;

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

            subdivContainer.children.Add(new DebugUI.FloatField { displayName = "Culling Distance", getter = () => debugDisplay.subdivisionViewCullingDistance, setter = value => debugDisplay.subdivisionViewCullingDistance = value, min = () => 0.0f });

            var probeContainer = new DebugUI.Container() { displayName = "Probe Visualization" };
            probeContainer.children.Add(new DebugUI.BoolField { displayName = "Display Probes", getter = () => debugDisplay.drawProbes, setter = value => debugDisplay.drawProbes = value, onValueChanged = RefreshDebug });
            if (debugDisplay.drawProbes)
            {
                var probeContainerChildren = new DebugUI.Container();
                probeContainerChildren.children.Add(new DebugUI.EnumField
                {
                    displayName = "Probe Shading Mode",
                    getter = () => (int)debugDisplay.probeShading,
                    setter = value => debugDisplay.probeShading = (DebugProbeShadingMode)value,
                    autoEnum = typeof(DebugProbeShadingMode),
                    getIndex = () => (int)debugDisplay.probeShading,
                    setIndex = value => debugDisplay.probeShading = (DebugProbeShadingMode)value,
                    onValueChanged = RefreshDebug
                });
                probeContainerChildren.children.Add(new DebugUI.FloatField { displayName = "Probe Size", getter = () => debugDisplay.probeSize, setter = value => debugDisplay.probeSize = value, min = () => kProbeSizeMin, max = () => kProbeSizeMax });
                if (debugDisplay.probeShading == DebugProbeShadingMode.SH || debugDisplay.probeShading == DebugProbeShadingMode.SHL0 || debugDisplay.probeShading == DebugProbeShadingMode.SHL0L1)
                    probeContainerChildren.children.Add(new DebugUI.FloatField { displayName = "Probe Exposure Compensation", getter = () => debugDisplay.exposureCompensation, setter = value => debugDisplay.exposureCompensation = value });

                probeContainerChildren.children.Add(new DebugUI.IntField
                {
                    displayName = "Max subdivision displayed",
                    getter = () => debugDisplay.maxSubdivToVisualize,
                    setter = (v) => debugDisplay.maxSubdivToVisualize = Mathf.Min(v, ProbeReferenceVolume.instance.GetMaxSubdivision()),
                    min = () => 0,
                    max = () => ProbeReferenceVolume.instance.GetMaxSubdivision(),
                });

                probeContainer.children.Add(probeContainerChildren);
            }

            probeContainer.children.Add(new DebugUI.BoolField
            {
                displayName = "Virtual Offset",
                getter = () => debugDisplay.drawVirtualOffsetPush,
                setter = value =>
                {
                    debugDisplay.drawVirtualOffsetPush = value;

                    if (debugDisplay.drawVirtualOffsetPush && debugDisplay.drawProbes)
                    {
                        // If probes are being drawn when enabling offset, automatically scale them down to a reasonable size so the arrows aren't obscured by the probes.
                        var searchDistance = CellSize(0) * MinBrickSize() / ProbeBrickPool.kBrickCellCount * bakingProcessSettings.virtualOffsetSettings.searchMultiplier + bakingProcessSettings.virtualOffsetSettings.outOfGeoOffset;
                        debugDisplay.probeSize = Mathf.Min(debugDisplay.probeSize, Mathf.Clamp(searchDistance, kProbeSizeMin, kProbeSizeMax));
                    }
                },
                onValueChanged = RefreshDebug
            });
            if (debugDisplay.drawVirtualOffsetPush)
            {
                var voOffset = new DebugUI.FloatField { displayName = "Offset Size", getter = () => debugDisplay.offsetSize, setter = value => debugDisplay.offsetSize = value, min = () => kOffsetSizeMin, max = () => kOffsetSizeMax };
                probeContainer.children.Add(new DebugUI.Container { children = { voOffset } });
            }

            probeContainer.children.Add(new DebugUI.FloatField { displayName = "Culling Distance", getter = () => debugDisplay.probeCullingDistance, setter = value => debugDisplay.probeCullingDistance = value, min = () => 0.0f });

            var streamingContainer = new DebugUI.Container() { displayName = "Streaming" };
            streamingContainer.children.Add(new DebugUI.BoolField { displayName = "Freeze Streaming", getter = () => debugDisplay.freezeStreaming, setter = value => debugDisplay.freezeStreaming = value });

            if (parameters.supportsRuntimeDebug)
            {
                // Cells / Bricks visualization is not implemented in a runtime compatible way atm.
                if (Application.isEditor)
                    widgetList.Add(subdivContainer);

                widgetList.Add(probeContainer);
            }

            if (parameters.supportStreaming)
            {
                widgetList.Add(streamingContainer);
            }

            if (widgetList.Count > 0)
            {
                m_DebugItems = widgetList.ToArray();
                var panel = DebugManager.instance.GetPanel(k_DebugPanelName, true);
                panel.children.Add(m_DebugItems);
            }
        }

        void UnregisterDebug(bool destroyPanel)
        {
            if (destroyPanel)
                DebugManager.instance.RemovePanel(k_DebugPanelName);
            else
                DebugManager.instance.GetPanel(k_DebugPanelName, false).children.Remove(m_DebugItems);
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
            if (!enabledBySRP || !isInitialized)
                return;

            if (!debugDisplay.drawProbes && !debugDisplay.drawVirtualOffsetPush)
                return;

            GeometryUtility.CalculateFrustumPlanes(camera, m_DebugFrustumPlanes);

            m_DebugMaterial.shaderKeywords = null;
            if (m_SHBands == ProbeVolumeSHBands.SphericalHarmonicsL1)
                m_DebugMaterial.EnableKeyword("PROBE_VOLUMES_L1");
            else if (m_SHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                m_DebugMaterial.EnableKeyword("PROBE_VOLUMES_L2");

            // This is to force the rendering not to draw to the depth pre pass and still behave.
            // They are going to be rendered opaque anyhow, just using the transparent render queue to make sure
            // they properly behave w.r.t fog.
            m_DebugMaterial.renderQueue = (int)RenderQueue.Transparent;

            foreach (var cellInfo in ProbeReferenceVolume.instance.cells.Values)
            {
                if (ShouldCullCell(cellInfo.cell.position, camera.transform, m_DebugFrustumPlanes))
                    continue;

                var debug = CreateInstancedProbes(cellInfo);

                if (debug == null)
                    continue;

                for (int i = 0; i < debug.probeBuffers.Count; ++i)
                {
                    var props = debug.props[i];
                    props.SetInt("_ShadingMode", (int)debugDisplay.probeShading);
                    props.SetFloat("_ExposureCompensation", debugDisplay.exposureCompensation);
                    props.SetFloat("_ProbeSize", debugDisplay.probeSize);
                    props.SetFloat("_CullDistance", debugDisplay.probeCullingDistance);
                    props.SetInt("_MaxAllowedSubdiv", debugDisplay.maxSubdivToVisualize);
                    props.SetFloat("_ValidityThreshold", bakingProcessSettings.dilationSettings.dilationValidityThreshold);
                    props.SetFloat("_OffsetSize", debugDisplay.offsetSize);

                    if (debugDisplay.drawProbes)
                    {
                        var probeBuffer = debug.probeBuffers[i];
                        Graphics.DrawMeshInstanced(m_DebugMesh, 0, m_DebugMaterial, probeBuffer, probeBuffer.Length, props, ShadowCastingMode.Off, false, 0, camera, LightProbeUsage.Off, null);
                    }

                    if (debugDisplay.drawVirtualOffsetPush)
                    {
                        var offsetBuffer = debug.offsetBuffers[i];
                        Graphics.DrawMeshInstanced(m_DebugOffsetMesh, 0, m_DebugOffsetMaterial, offsetBuffer, offsetBuffer.Length, props, ShadowCastingMode.Off, false, 0, camera, LightProbeUsage.Off, null);
                    }
                }
            }
        }

        void ClearDebugData()
        {
            realtimeSubdivisionInfo.Clear();
        }

        CellInstancedDebugProbes CreateInstancedProbes(CellInfo cellInfo)
        {
            if (cellInfo.debugProbes != null)
                return cellInfo.debugProbes;

            int maxSubdiv = ProbeReferenceVolume.instance.GetMaxSubdivision() - 1;

            var cell = cellInfo.cell;

            if (!cell.shL0L1Data.IsCreated || cell.shL0L1Data.Length == 0 || !cellInfo.loaded)
                return null;

            List<Matrix4x4[]> probeBuffers = new List<Matrix4x4[]>();
            List<Matrix4x4[]> offsetBuffers = new List<Matrix4x4[]>();
            List<MaterialPropertyBlock> props = new List<MaterialPropertyBlock>();
            var chunks = cellInfo.chunkList;

            Vector4[] texels = new Vector4[kProbesPerBatch];
            float[] validity = new float[kProbesPerBatch];
            float[] relativeSize = new float[kProbesPerBatch];
            Vector4[] offsets = cell.offsetVectors.Length > 0 ? new Vector4[kProbesPerBatch] : null;

            List<Matrix4x4> probeBuffer = new List<Matrix4x4>();
            List<Matrix4x4> offsetBuffer = new List<Matrix4x4>();

            var debugData = new CellInstancedDebugProbes();
            debugData.probeBuffers = probeBuffers;
            debugData.offsetBuffers = offsetBuffers;
            debugData.props = props;

            int idxInBatch = 0;
            for (int i = 0; i < cell.probePositions.Length; i++)
            {
                var brickSize = cell.bricks[i / 64].subdivisionLevel;

                int chunkIndex = i / ProbeBrickPool.GetChunkSizeInProbeCount();
                var chunk = chunks[chunkIndex];
                int indexInChunk = i % ProbeBrickPool.GetChunkSizeInProbeCount();
                int brickIdx = indexInChunk / 64;
                int indexInBrick = indexInChunk % 64;

                Vector2Int brickStart = new Vector2Int(chunk.x + brickIdx * 4, chunk.y);
                int indexInSlice = indexInBrick % 16;
                Vector3Int texelLoc = new Vector3Int(brickStart.x + (indexInSlice % 4), brickStart.y + (indexInSlice / 4), indexInBrick / 16);

                probeBuffer.Add(Matrix4x4.TRS(cell.probePositions[i], Quaternion.identity, Vector3.one * (0.3f * (brickSize + 1))));
                validity[idxInBatch] = cell.GetValidity(i);
                texels[idxInBatch] = new Vector4(texelLoc.x, texelLoc.y, texelLoc.z, brickSize);
                relativeSize[idxInBatch] = (float)brickSize / (float)maxSubdiv;
                if (offsets != null)
                {
                    const float kOffsetThresholdSqr = 1e-6f;

                    var offset = cell.offsetVectors[i];
                    offsets[idxInBatch] = offset;

                    if (offset.sqrMagnitude < kOffsetThresholdSqr)
                    {
                        offsetBuffer.Add(Matrix4x4.identity);
                    }
                    else
                    {
                        var position = cell.probePositions[i] + offset;
                        var orientation = Quaternion.LookRotation(-offset);
                        var scale = new Vector3(0.5f, 0.5f, offset.magnitude);
                        offsetBuffer.Add(Matrix4x4.TRS(position, orientation, scale));
                    }
                }
                idxInBatch++;

                if (probeBuffer.Count >= kProbesPerBatch || i == cell.probePositions.Length - 1)
                {
                    idxInBatch = 0;
                    MaterialPropertyBlock prop = new MaterialPropertyBlock();

                    prop.SetFloatArray("_Validity", validity);
                    prop.SetFloatArray("_RelativeSize", relativeSize);
                    prop.SetVectorArray("_IndexInAtlas", texels);

                    if (offsets != null)
                        prop.SetVectorArray("_Offset", offsets);

                    props.Add(prop);

                    probeBuffers.Add(probeBuffer.ToArray());
                    probeBuffer = new List<Matrix4x4>();
                    probeBuffer.Clear();

                    offsetBuffers.Add(offsetBuffer.ToArray());
                    offsetBuffer.Clear();
                }
            }

            cellInfo.debugProbes = debugData;

            return debugData;
        }

        void OnClearLightingdata()
        {
            ClearDebugData();
        }
    }
}
