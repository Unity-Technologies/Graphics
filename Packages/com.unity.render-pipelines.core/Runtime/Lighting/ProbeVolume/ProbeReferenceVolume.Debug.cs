using System.Collections.Generic;

namespace UnityEngine.Rendering
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
        /// Show in red probes that have been made invalid by touchup volumes. Important to note that this debug view will only show result for volumes still present in the scene.
        /// </summary>
        InvalidatedByTouchupVolumes,
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
        public float probeSize = 0.3f;
        public float subdivisionViewCullingDistance = 500.0f;
        public float probeCullingDistance = 200.0f;
        public int maxSubdivToVisualize = ProbeBrickIndex.kMaxSubdivisionLevels;
        public int minSubdivToVisualize = 0;
        public float exposureCompensation;
        public bool drawVirtualOffsetPush;
        public float offsetSize = 0.025f;
        public bool freezeStreaming;
        public int otherStateIndex = 0;
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

        internal ProbeVolumeDebug probeVolumeDebug { get; } = new ProbeVolumeDebug();

        /// <summary>Colors that can be used for debug visualization of the brick structure subdivision.</summary>
        public Color[] subdivisionDebugColors { get; } = new Color[ProbeBrickIndex.kMaxSubdivisionLevels];

        DebugUI.Widget[] m_DebugItems;
        Mesh m_DebugMesh;
        Material m_DebugMaterial;
        Mesh m_DebugOffsetMesh;
        Material m_DebugOffsetMaterial;
        Plane[] m_DebugFrustumPlanes = new Plane[6];

        // Scenario blending debug data
        GUIContent[] m_DebugScenarioNames = new GUIContent[0];
        int[] m_DebugScenarioValues = new int[0];
        string m_DebugActiveSceneGUID, m_DebugActiveScenario;
        DebugUI.EnumField m_DebugScenarioField;

        internal ProbeVolumeBakingProcessSettings bakingProcessSettings; /* DEFAULTS would be better but is implemented in PR#6174 = ProbeVolumeBakingProcessSettings.Defaults; */

        // Field used for the realtime subdivision preview
        internal Dictionary<Bounds, ProbeBrickIndex.Brick[]> realtimeSubdivisionInfo = new ();

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
            subdivContainer.children.Add(new DebugUI.BoolField { displayName = "Display Cells", getter = () => probeVolumeDebug.drawCells, setter = value => probeVolumeDebug.drawCells = value, onValueChanged = RefreshDebug });
            subdivContainer.children.Add(new DebugUI.BoolField { displayName = "Display Bricks", getter = () => probeVolumeDebug.drawBricks, setter = value => probeVolumeDebug.drawBricks = value, onValueChanged = RefreshDebug });
#if UNITY_EDITOR
            subdivContainer.children.Add(new DebugUI.BoolField { displayName = "Realtime Update", getter = () => probeVolumeDebug.realtimeSubdivision, setter = value => probeVolumeDebug.realtimeSubdivision = value, onValueChanged = RefreshDebug });
            if (probeVolumeDebug.realtimeSubdivision)
            {
                var cellUpdatePerFrame = new DebugUI.IntField { displayName = "Number Of Cell Update Per Frame", getter = () => probeVolumeDebug.subdivisionCellUpdatePerFrame, setter = value => probeVolumeDebug.subdivisionCellUpdatePerFrame = value, min = () => 1, max = () => 100 };
                var delayBetweenUpdates = new DebugUI.FloatField { displayName = "Delay Between Two Updates In Seconds", getter = () => probeVolumeDebug.subdivisionDelayInSeconds, setter = value => probeVolumeDebug.subdivisionDelayInSeconds = value, min = () => 0.1f, max = () => 10 };
                subdivContainer.children.Add(new DebugUI.Container { children = { cellUpdatePerFrame, delayBetweenUpdates } });
            }
#endif

            subdivContainer.children.Add(new DebugUI.FloatField { displayName = "Culling Distance", getter = () => probeVolumeDebug.subdivisionViewCullingDistance, setter = value => probeVolumeDebug.subdivisionViewCullingDistance = value, min = () => 0.0f });

            var probeContainer = new DebugUI.Container() { displayName = "Probe Visualization" };
            probeContainer.children.Add(new DebugUI.BoolField { displayName = "Display Probes", getter = () => probeVolumeDebug.drawProbes, setter = value => probeVolumeDebug.drawProbes = value, onValueChanged = RefreshDebug });
            if (probeVolumeDebug.drawProbes)
            {
                var probeContainerChildren = new DebugUI.Container();
                probeContainerChildren.children.Add(new DebugUI.EnumField
                {
                    displayName = "Probe Shading Mode",
                    getter = () => (int)probeVolumeDebug.probeShading,
                    setter = value => probeVolumeDebug.probeShading = (DebugProbeShadingMode)value,
                    autoEnum = typeof(DebugProbeShadingMode),
                    getIndex = () => (int)probeVolumeDebug.probeShading,
                    setIndex = value => probeVolumeDebug.probeShading = (DebugProbeShadingMode)value,
                    onValueChanged = RefreshDebug
                });
                probeContainerChildren.children.Add(new DebugUI.FloatField { displayName = "Probe Size", getter = () => probeVolumeDebug.probeSize, setter = value => probeVolumeDebug.probeSize = value, min = () => kProbeSizeMin, max = () => kProbeSizeMax });
                if (probeVolumeDebug.probeShading == DebugProbeShadingMode.SH || probeVolumeDebug.probeShading == DebugProbeShadingMode.SHL0 || probeVolumeDebug.probeShading == DebugProbeShadingMode.SHL0L1)
                    probeContainerChildren.children.Add(new DebugUI.FloatField { displayName = "Probe Exposure Compensation", getter = () => probeVolumeDebug.exposureCompensation, setter = value => probeVolumeDebug.exposureCompensation = value });

                probeContainerChildren.children.Add(new DebugUI.IntField
                {
                    displayName = "Max subdivision displayed",
                    getter = () => probeVolumeDebug.maxSubdivToVisualize,
                    setter = (v) => probeVolumeDebug.maxSubdivToVisualize = Mathf.Min(v, ProbeReferenceVolume.instance.GetMaxSubdivision() - 1),
                    min = () => 0,
                    max = () => ProbeReferenceVolume.instance.GetMaxSubdivision()-1,
                });

                probeContainerChildren.children.Add(new DebugUI.IntField
                {
                    displayName = "Min subdivision displayed",
                    getter = () => probeVolumeDebug.minSubdivToVisualize,
                    setter = (v) => probeVolumeDebug.minSubdivToVisualize = Mathf.Max(v, 0),
                    min = () => 0,
                    max = () => ProbeReferenceVolume.instance.GetMaxSubdivision()-1,
                });


                probeContainer.children.Add(probeContainerChildren);
            }

            probeContainer.children.Add(new DebugUI.BoolField
            {
                displayName = "Virtual Offset",
                getter = () => probeVolumeDebug.drawVirtualOffsetPush,
                setter = value =>
                {
                    probeVolumeDebug.drawVirtualOffsetPush = value;

                    if (probeVolumeDebug.drawVirtualOffsetPush && probeVolumeDebug.drawProbes)
                    {
                        // If probes are being drawn when enabling offset, automatically scale them down to a reasonable size so the arrows aren't obscured by the probes.
                        var searchDistance = CellSize(0) * MinBrickSize() / ProbeBrickPool.kBrickCellCount * bakingProcessSettings.virtualOffsetSettings.searchMultiplier + bakingProcessSettings.virtualOffsetSettings.outOfGeoOffset;
                        probeVolumeDebug.probeSize = Mathf.Min(probeVolumeDebug.probeSize, Mathf.Clamp(searchDistance, kProbeSizeMin, kProbeSizeMax));
                    }
                },
                onValueChanged = RefreshDebug
            });
            if (probeVolumeDebug.drawVirtualOffsetPush)
            {
                var voOffset = new DebugUI.FloatField { displayName = "Offset Size", getter = () => probeVolumeDebug.offsetSize, setter = value => probeVolumeDebug.offsetSize = value, min = () => kOffsetSizeMin, max = () => kOffsetSizeMax };
                probeContainer.children.Add(new DebugUI.Container { children = { voOffset } });
            }

            probeContainer.children.Add(new DebugUI.FloatField { displayName = "Culling Distance", getter = () => probeVolumeDebug.probeCullingDistance, setter = value => probeVolumeDebug.probeCullingDistance = value, min = () => 0.0f });

            var streamingContainer = new DebugUI.Container() { displayName = "Streaming" };
            streamingContainer.children.Add(new DebugUI.BoolField { displayName = "Freeze Streaming", getter = () => probeVolumeDebug.freezeStreaming, setter = value => probeVolumeDebug.freezeStreaming = value });

            streamingContainer.children.Add(new DebugUI.IntField { displayName = "Number Of Cells Loaded Per Frame", getter = () => instance.numberOfCellsLoadedPerFrame, setter = value => instance.SetNumberOfCellsLoadedPerFrame(value), min = () => 0 });

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

            if (parameters.scenarioBlendingShader != null && parameters.blendingMemoryBudget != 0)
            {
                var blendingContainer = new DebugUI.Container() { displayName = "Scenario Blending" };
                blendingContainer.children.Add(new DebugUI.IntField { displayName = "Number Of Cells Blended Per Frame", getter = () => instance.numberOfCellsBlendedPerFrame, setter = value => instance.numberOfCellsBlendedPerFrame = value, min = () => 0 });
                blendingContainer.children.Add(new DebugUI.FloatField { displayName = "Turnover Rate", getter = () => instance.turnoverRate, setter = value => instance.turnoverRate = value, min = () => 0, max = () => 1 });

                void RefreshScenarioNames(string guid)
                {
                    HashSet<string> allScenarios = new();
                    foreach (var set in parameters.sceneData.bakingSets)
                    {
                        if (!set.sceneGUIDs.Contains(guid))
                            continue;
                        foreach (var scenario in set.lightingScenarios)
                            allScenarios.Add(scenario);
                    }

                    allScenarios.Remove(sceneData.lightingScenario);
                    if (m_DebugActiveSceneGUID == guid && allScenarios.Count + 1 == m_DebugScenarioNames.Length && m_DebugActiveScenario == sceneData.lightingScenario)
                        return;

                    int i = 0;
                    ArrayExtensions.ResizeArray(ref m_DebugScenarioNames, allScenarios.Count + 1);
                    ArrayExtensions.ResizeArray(ref m_DebugScenarioValues, allScenarios.Count + 1);
                    m_DebugScenarioNames[0] = new GUIContent("None");
                    m_DebugScenarioValues[0] = 0;
                    foreach (var scenario in allScenarios)
                    {
                        i++;
                        m_DebugScenarioNames[i] = new GUIContent(scenario);
                        m_DebugScenarioValues[i] = i;
                    }

                    m_DebugActiveSceneGUID = guid;
                    m_DebugActiveScenario = sceneData.lightingScenario;
                    m_DebugScenarioField.enumNames = m_DebugScenarioNames;
                    m_DebugScenarioField.enumValues = m_DebugScenarioValues;
                    if (probeVolumeDebug.otherStateIndex >= m_DebugScenarioNames.Length)
                        probeVolumeDebug.otherStateIndex = 0;
                }

                m_DebugScenarioField = new DebugUI.EnumField
                {
                    displayName = "Scenario To Blend With",
                    enumNames = m_DebugScenarioNames,
                    enumValues = m_DebugScenarioValues,
                    getIndex = () =>
                    {
                        RefreshScenarioNames(ProbeVolumeSceneData.GetSceneGUID(SceneManagement.SceneManager.GetActiveScene()));

                        probeVolumeDebug.otherStateIndex = 0;
                        if (!string.IsNullOrEmpty(sceneData.otherScenario))
                        {
                            for (int i = 1; i < m_DebugScenarioNames.Length; i++)
                            {
                                if (m_DebugScenarioNames[i].text == sceneData.otherScenario)
                                {
                                    probeVolumeDebug.otherStateIndex = i;
                                    break;
                                }
                            }
                        }
                        return probeVolumeDebug.otherStateIndex;
                    },
                    setIndex = value =>
                    {
                        string other = value == 0 ? null : m_DebugScenarioNames[value].text;
                        sceneData.BlendLightingScenario(other, sceneData.scenarioBlendingFactor);
                        probeVolumeDebug.otherStateIndex = value;
                    },
                    getter = () => probeVolumeDebug.otherStateIndex,
                    setter = (value) => probeVolumeDebug.otherStateIndex = value,
                };

                blendingContainer.children.Add(m_DebugScenarioField);
                blendingContainer.children.Add(new DebugUI.FloatField { displayName = "Scenario Blending Factor", getter = () => instance.scenarioBlendingFactor, setter = value => instance.scenarioBlendingFactor = value, min = () => 0.0f, max = () => 1.0f });

                widgetList.Add(blendingContainer);
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
            float distanceRoundedUpWithCellSize = Mathf.CeilToInt(probeVolumeDebug.probeCullingDistance / cellSize) * cellSize;

            if (Vector3.Distance(cameraTransform.position, cellCenterWS) > distanceRoundedUpWithCellSize)
                return true;

            var volumeAABB = new Bounds(cellCenterWS, cellSize * Vector3.one);
            return !GeometryUtility.TestPlanesAABB(frustumPlanes, volumeAABB);
        }

        void DrawProbeDebug(Camera camera)
        {
            if (!enabledBySRP || !isInitialized)
                return;

            if (!probeVolumeDebug.drawProbes && !probeVolumeDebug.drawVirtualOffsetPush)
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
            m_DebugOffsetMaterial.renderQueue = (int)RenderQueue.Transparent;

            // Sanitize the min max subdiv levels with what is available
            int minAvailableSubdiv = ProbeReferenceVolume.instance.cells.Count > 0 ? ProbeReferenceVolume.instance.GetMaxSubdivision()-1 : 0;
            foreach (var cellInfo in ProbeReferenceVolume.instance.cells.Values)
            {
                minAvailableSubdiv = Mathf.Min(minAvailableSubdiv, cellInfo.cell.minSubdiv);
            }

            probeVolumeDebug.maxSubdivToVisualize = Mathf.Min(probeVolumeDebug.maxSubdivToVisualize, ProbeReferenceVolume.instance.GetMaxSubdivision() - 1);
            probeVolumeDebug.minSubdivToVisualize = Mathf.Clamp(probeVolumeDebug.minSubdivToVisualize, minAvailableSubdiv, probeVolumeDebug.maxSubdivToVisualize);


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
                    props.SetInt("_ShadingMode", (int)probeVolumeDebug.probeShading);
                    props.SetFloat("_ExposureCompensation", probeVolumeDebug.exposureCompensation);
                    props.SetFloat("_ProbeSize", probeVolumeDebug.probeSize);
                    props.SetFloat("_CullDistance", probeVolumeDebug.probeCullingDistance);
                    props.SetInt("_MaxAllowedSubdiv", probeVolumeDebug.maxSubdivToVisualize);
                    props.SetInt("_MinAllowedSubdiv", probeVolumeDebug.minSubdivToVisualize);
                    props.SetFloat("_ValidityThreshold", bakingProcessSettings.dilationSettings.dilationValidityThreshold);
                    props.SetFloat("_OffsetSize", probeVolumeDebug.offsetSize);

                    if (probeVolumeDebug.drawProbes)
                    {
                        var probeBuffer = debug.probeBuffers[i];
                        Graphics.DrawMeshInstanced(m_DebugMesh, 0, m_DebugMaterial, probeBuffer, probeBuffer.Length, props, ShadowCastingMode.Off, false, 0, camera, LightProbeUsage.Off, null);
                    }

                    if (probeVolumeDebug.drawVirtualOffsetPush)
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

            if (!cell.bricks.IsCreated || cell.bricks.Length == 0 || !cellInfo.loaded)
                return null;

            List<Matrix4x4[]> probeBuffers = new List<Matrix4x4[]>();
            List<Matrix4x4[]> offsetBuffers = new List<Matrix4x4[]>();
            List<MaterialPropertyBlock> props = new List<MaterialPropertyBlock>();
            var chunks = cellInfo.chunkList;

            Vector4[] texels = new Vector4[kProbesPerBatch];
            float[] validity = new float[kProbesPerBatch];
            float[] relativeSize = new float[kProbesPerBatch];
            float[] touchupUpVolumeAction = cell.touchupVolumeInteraction.Length > 0 ? new float[kProbesPerBatch] : null;
            Vector4[] offsets = cell.offsetVectors.Length > 0 ? new Vector4[kProbesPerBatch] : null;

            List<Matrix4x4> probeBuffer = new List<Matrix4x4>();
            List<Matrix4x4> offsetBuffer = new List<Matrix4x4>();

            var debugData = new CellInstancedDebugProbes();
            debugData.probeBuffers = probeBuffers;
            debugData.offsetBuffers = offsetBuffers;
            debugData.props = props;

            var chunkSizeInProbes = m_CurrentProbeVolumeChunkSizeInBricks * ProbeBrickPool.kBrickProbeCountTotal;

            var loc = ProbeBrickPool.ProbeCountToDataLocSize(chunkSizeInProbes);

            int idxInBatch = 0;
            int globalIndex = 0;
            int brickCount = cell.probeCount / ProbeBrickPool.kBrickProbeCountTotal;
            int bx = 0, by = 0, bz = 0;
            for (int brickIndex = 0; brickIndex < brickCount; ++brickIndex)
            {
                Debug.Assert(bz < loc.z);

                int brickSize = cell.bricks[brickIndex].subdivisionLevel;
                int chunkIndex = brickIndex / m_CurrentProbeVolumeChunkSizeInBricks;
                var chunk = chunks[chunkIndex];
                Vector3Int brickStart = new Vector3Int(chunk.x + bx, chunk.y + by, chunk.z + bz);

                for (int z = 0; z < ProbeBrickPool.kBrickProbeCountPerDim; ++z)
                {
                    for (int y = 0; y < ProbeBrickPool.kBrickProbeCountPerDim; ++y)
                    {
                        for (int x = 0; x < ProbeBrickPool.kBrickProbeCountPerDim; ++x)
                        {
                            Vector3Int texelLoc = new Vector3Int(brickStart.x + x, brickStart.y + y, brickStart.z + z);

                            int probeFlatIndex = chunkIndex * chunkSizeInProbes + (bx + x) + loc.x * ((by + y) + loc.y * (bz + z));

                            probeBuffer.Add(Matrix4x4.TRS(cell.probePositions[probeFlatIndex], Quaternion.identity, Vector3.one * (0.3f * (brickSize + 1))));
                            validity[idxInBatch] = cell.validity[probeFlatIndex];
                            texels[idxInBatch] = new Vector4(texelLoc.x, texelLoc.y, texelLoc.z, brickSize);
                            relativeSize[idxInBatch] = (float)brickSize / (float)maxSubdiv;

                            if (touchupUpVolumeAction != null)
                            {
                                touchupUpVolumeAction[idxInBatch] = cell.touchupVolumeInteraction[probeFlatIndex];
                            }

                            if (offsets != null)
                            {
                                const float kOffsetThresholdSqr = 1e-6f;

                                var offset = cell.offsetVectors[probeFlatIndex];
                                offsets[idxInBatch] = offset;

                                if (offset.sqrMagnitude < kOffsetThresholdSqr)
                                {
                                    offsetBuffer.Add(Matrix4x4.identity);
                                }
                                else
                                {
                                    var position = cell.probePositions[probeFlatIndex] + offset;
                                    var orientation = Quaternion.LookRotation(-offset);
                                    var scale = new Vector3(0.5f, 0.5f, offset.magnitude);
                                    offsetBuffer.Add(Matrix4x4.TRS(position, orientation, scale));
                                }
                            }
                            idxInBatch++;

                            if (probeBuffer.Count >= kProbesPerBatch || globalIndex == cell.probeCount - 1)
                            {
                                idxInBatch = 0;
                                MaterialPropertyBlock prop = new MaterialPropertyBlock();

                                prop.SetFloatArray("_Validity", validity);
                                prop.SetFloatArray("_TouchupedByVolume", touchupUpVolumeAction);
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

                            globalIndex++;
                        }
                    }
                }

                bx += ProbeBrickPool.kBrickProbeCountPerDim;
                if (bx >= loc.x)
                {
                    bx = 0;
                    by += ProbeBrickPool.kBrickProbeCountPerDim;
                    if (by >= loc.y)
                    {
                        by = 0;
                        bz += ProbeBrickPool.kBrickProbeCountPerDim;
                        if (bz >= loc.z)
                        {
                            bx = 0;
                            by = 0;
                            bz = 0;
                        }
                    }
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
