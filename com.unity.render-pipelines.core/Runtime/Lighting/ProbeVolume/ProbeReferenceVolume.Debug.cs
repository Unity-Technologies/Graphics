using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    [GenerateHLSL]
    public enum DebugProbeShadingMode
    {
        SH,
        Validity,
        Size
    }

    class ProbeVolumeDebug
    {
        public bool drawProbes;
        public bool drawBricks;
        public bool drawCells;
        public DebugProbeShadingMode probeShading;
        public float probeSize = 1.0f;
        public float cullingDistance = 500;
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

        internal ProbeVolumeDebug       debugDisplay { get; } = new ProbeVolumeDebug();

        DebugUI.Widget[]                m_DebugItems;
        Mesh                            m_DebugMesh;
        Material                        m_DebugMaterial;
        List<CellInstancedDebugProbes>  m_CellDebugData = new List<CellInstancedDebugProbes>();
        Plane[]                         m_DebugFrustumPlanes = new Plane[6];

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

            RegisterDebug();
        }

        void CleanupDebug()
        {
            UnregisterDebug(true);
            CoreUtils.Destroy(m_DebugMaterial);
        }

        void RefreshDebug<T>(DebugUI.Field<T> field, T value)
        {
            UnregisterDebug(false);
            RegisterDebug();
        }

        void RegisterDebug()
        {
            var widgetList = new List<DebugUI.Widget>();
            widgetList.Add(new DebugUI.BoolField { displayName = "Display Cells", getter = () => debugDisplay.drawCells, setter = value => debugDisplay.drawCells = value });
            widgetList.Add(new DebugUI.BoolField { displayName = "Display Bricks", getter = () => debugDisplay.drawBricks, setter = value => debugDisplay.drawBricks = value });
            widgetList.Add(new DebugUI.BoolField { displayName = "Display Probes", getter = () => debugDisplay.drawProbes, setter = value => debugDisplay.drawProbes = value, onValueChanged = RefreshDebug });
            if (debugDisplay.drawProbes)
            {
                var probeContainer = new DebugUI.Container();

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

                widgetList.Add(probeContainer);
            }
            widgetList.Add(new DebugUI.FloatField { displayName = "Culling Distance", getter = () => debugDisplay.cullingDistance, setter = value => debugDisplay.cullingDistance = value, min = () => 0.0f });

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

        bool ShouldCull(Vector3 cellPosition, Vector3 cameraPosition, Plane[] frustumPlanes)
        {
            var cellSize = MaxBrickSize();
            var originWS = GetTransform().posWS;
            Vector3 cellCenterWS = cellPosition * cellSize + originWS + Vector3.one * (cellSize / 2.0f);

            if (Vector3.Distance(cameraPosition, cellCenterWS) > debugDisplay.cullingDistance)
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

                foreach (var debug in m_CellDebugData)
                {
                    if (ShouldCull(debug.cellPosition, camera.transform.position, m_DebugFrustumPlanes))
                        continue;

                    for (int i = 0; i < debug.probeBuffers.Count; ++i)
                    {
                        var probeBuffer = debug.probeBuffers[i];
                        var props = debug.props[i];
                        props.SetInt("_ShadingMode", (int)debugDisplay.probeShading);
                        props.SetFloat("_ExposureCompensation", -debugDisplay.exposureCompensation);
                        props.SetFloat("_ProbeSize", debugDisplay.probeSize);

                        Graphics.DrawMeshInstanced(m_DebugMesh, 0, m_DebugMaterial, probeBuffer, probeBuffer.Length, props, ShadowCastingMode.Off, false, 0, camera, LightProbeUsage.Off, null);
                    }
                }
            }
        }

        void ClearDebugData()
        {
            m_CellDebugData.Clear();
        }

        void CreateInstancedProbes()
        {
            foreach (var cell in ProbeReferenceVolume.instance.cells.Values)
            {
                if (cell.sh == null || cell.sh.Length == 0)
                    continue;

                float largestBrickSize = cell.bricks.Count == 0 ? 0 : cell.bricks[0].subdivisionLevel;

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
                        if (cell.bricks[i / 64].subdivisionLevel == brickSize)
                        {
                            probeBuffer.Add(Matrix4x4.TRS(cell.probePositions[i], Quaternion.identity, Vector3.one * (0.3f * (brickSize + 1))));
                            probeMap.Add(i);
                        }

                        // Batch limit reached or out of probes
                        if (probeBuffer.Count >= kProbesPerBatch || i == cell.probePositions.Length - 1)
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
                debugData.cellPosition = cell.position;

                Vector4[] positionBuffer = new Vector4[kProbesPerBatch];
                Vector4[] validityColors = new Vector4[kProbesPerBatch];

                for (int batchIndex = 0; batchIndex < probeMaps.Count; batchIndex++)
                {
                    for (int indexInBatch = 0; indexInBatch < probeMaps[batchIndex].Length; indexInBatch++)
                    {
                        int probeIdx = probeMaps[batchIndex][indexInBatch];

                        var pos = cell.probePositions[probeIdx];
                        positionBuffer[indexInBatch] = new Vector4(pos.x, pos.y, pos.z, 0.0f);
                        validityColors[indexInBatch] = Color.Lerp(Color.green, Color.red, cell.validity[probeIdx]);
                    }

                    debugData.props[batchIndex].SetVectorArray("_Position", positionBuffer);
                    debugData.props[batchIndex].SetVectorArray("_Validity", validityColors);
                }

                m_CellDebugData.Add(debugData);
            }
        }
    }
}
