using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// A marker to determine what area of the scene is considered by the Probe Volumes system
    /// </summary>
    [CoreRPHelpURL("probevolumes-settings#probe-volume-properties", "com.unity.render-pipelines.high-definition")]
    [ExecuteAlways]
    [AddComponentMenu("Rendering/Probe Volume")]
    public partial class ProbeVolume : MonoBehaviour
    {
        /// <summary>Indicates which renderers should be considerer for the Probe Volume bounds when baking</summary>
        public enum Mode
        {
            /// <summary>Encapsulate all renderers in the baking set.</summary>
            Global,
            /// <summary>Encapsulate all renderers in the scene.</summary>
            Scene,
            /// <summary>Encapsulate all renderers in the bounding box.</summary>
            Local
        }

        /// <summary>
        /// If is a global bolume
        /// </summary>
        [Tooltip("When set to Global this Probe Volume considers all renderers with Contribute Global Illumination enabled. Local only considers renderers in the scene.\nThis list updates every time the Scene is saved or the lighting is baked.")]
        public Mode mode = Mode.Local;

        /// <summary>
        /// The size
        /// </summary>
        public Vector3 size = new Vector3(10, 10, 10);

        /// <summary>
        /// Override the renderer filters.
        /// </summary>
        [HideInInspector, Min(0)]
        public bool overrideRendererFilters = false;

        /// <summary>
        /// The minimum renderer bounding box volume size. This value is used to discard small renderers when the overrideMinRendererVolumeSize is enabled.
        /// </summary>
        [HideInInspector, Min(0)]
        public float minRendererVolumeSize = 0.1f;

        /// <summary>
        /// The <see cref="LayerMask"/>
        /// </summary>
        public LayerMask objectLayerMask = -1;

        /// <summary>
        /// The lowest subdivision level override
        /// </summary>
        [HideInInspector]
        public int lowestSubdivLevelOverride = 0;

        /// <summary>
        /// The highest subdivision level override
        /// </summary>
        [HideInInspector]
        public int highestSubdivLevelOverride = -1;

        /// <summary>
        /// If the subdivision levels need to be overriden
        /// </summary>
        [HideInInspector]
        public bool overridesSubdivLevels = false;

        [SerializeField] internal bool mightNeedRebaking = false;

        [SerializeField] internal Matrix4x4 cachedTransform;
        [SerializeField] internal int cachedHashCode;

        /// <summary>Whether spaces with no renderers need to be filled with bricks at lowest subdivision level.</summary>
        [HideInInspector]
        [Tooltip("Whether Unity should fill empty space between renderers with bricks at the lowest subdivision level.")]
        public bool fillEmptySpaces = false;

#if UNITY_EDITOR
        /// <summary>
        /// Returns the extents of the volume.
        /// </summary>
        /// <returns>The extents of the ProbeVolume.</returns>
        public Vector3 GetExtents()
        {
            return size;
        }

        internal Bounds ComputeBounds(GIContributors.ContributorFilter filter, Scene? scene = null)
        {
            Bounds bounds = new Bounds();
            bool foundABound = false;

            void ExpandBounds(Bounds bound)
            {
                if (!foundABound)
                {
                    bounds = bound;
                    foundABound = true;
                }
                else
                {
                    bounds.Encapsulate(bound);
                }
            }

            var contributors = GIContributors.Find(filter, scene);
            foreach (var renderer in contributors.renderers)
                ExpandBounds(renderer.component.bounds);
            foreach (var terrain in contributors.terrains)
                ExpandBounds(terrain.boundsWithTrees);

            return bounds;
        }

        internal void UpdateGlobalVolume(GIContributors.ContributorFilter filter)
        {
            float minBrickSize = ProbeReferenceVolume.instance.MinBrickSize();
            var bounds = ComputeBounds(filter, gameObject.scene);
            transform.position = bounds.center;
            size = Vector3.Max(bounds.size + new Vector3(minBrickSize, minBrickSize, minBrickSize), Vector3.zero);
        }

        internal void OnLightingDataAssetCleared()
        {
            mightNeedRebaking = true;
        }

        internal void OnBakeCompleted()
        {
            // We cache the data of last bake completed.
            cachedTransform = gameObject.transform.worldToLocalMatrix;
            cachedHashCode = GetHashCode();
            mightNeedRebaking = false;
        }

        public override int GetHashCode()
        {
            int hash = 17;

            unchecked
            {
                hash = hash * 23 + size.GetHashCode();
                hash = hash * 23 + gameObject.transform.worldToLocalMatrix.GetHashCode();
                hash = hash * 23 + overridesSubdivLevels.GetHashCode();
                hash = hash * 23 + highestSubdivLevelOverride.GetHashCode();
                hash = hash * 23 + lowestSubdivLevelOverride.GetHashCode();
                hash = hash * 23 + overrideRendererFilters.GetHashCode();
                if (overrideRendererFilters)
                {
                    hash = hash * 23 + minRendererVolumeSize.GetHashCode();
                    hash = hash * 23 + objectLayerMask.value.GetHashCode();
                }
                hash = hash * 23 + fillEmptySpaces.GetHashCode();
            }

            return hash;
        }

        internal float GetMinSubdivMultiplier()
        {
            float maxSubdiv = ProbeReferenceVolume.instance.GetMaxSubdivision() - 1;
            return overridesSubdivLevels ? Mathf.Clamp(lowestSubdivLevelOverride / maxSubdiv, 0.0f, 1.0f) : 0.0f;
        }

        internal float GetMaxSubdivMultiplier()
        {
            float maxSubdiv = ProbeReferenceVolume.instance.GetMaxSubdivision() - 1;
            return overridesSubdivLevels ? Mathf.Clamp(highestSubdivLevelOverride / maxSubdiv, 0.0f, 1.0f) : 1.0f;
        }

        // Momentarily moving the gizmo rendering for bricks and cells to Probe Volume itself,
        // only the first probe volume in the scene will render them. The reason is that we dont have any
        // other non-hidden component related to APV.
        #region APVGizmo

        internal static List<ProbeVolume> instances = new();

        MeshGizmo brickGizmos;
        MeshGizmo cellGizmo;

        void DisposeGizmos()
        {
            brickGizmos?.Dispose();
            brickGizmos = null;
            cellGizmo?.Dispose();
            cellGizmo = null;
        }

        void OnEnable()
        {
            instances.Add(this);
        }

        void OnDisable()
        {
            instances.Remove(this);
            DisposeGizmos();
        }

        // Only the first PV of the available ones will draw gizmos.
        bool IsResponsibleToDrawGizmo() => instances.Count > 0 && instances[0] == this;

        internal bool ShouldCullCell(Vector3 cellPosition, Vector3 originWS = default(Vector3))
        {
            var cellSizeInMeters = ProbeReferenceVolume.instance.MaxBrickSize();
            var debugDisplay = ProbeReferenceVolume.instance.probeVolumeDebug;
            if (debugDisplay.realtimeSubdivision)
            {
                var profile = ProbeReferenceVolume.instance.sceneData.GetBakingSetForScene(gameObject.scene);
                if (profile == null)
                    return true;
                cellSizeInMeters = profile.cellSizeInMeters;
            }
            Camera activeCamera = Camera.current;
#if UNITY_EDITOR
            if (activeCamera == null)
                activeCamera = UnityEditor.SceneView.lastActiveSceneView.camera;
#endif

            if (activeCamera == null)
                return true;

            var cameraTransform = activeCamera.transform;

            Vector3 cellCenterWS = cellPosition * cellSizeInMeters + originWS + Vector3.one * (cellSizeInMeters / 2.0f);

            // Round down to cell size distance
            float roundedDownDist = Mathf.Floor(Vector3.Distance(cameraTransform.position, cellCenterWS) / cellSizeInMeters) * cellSizeInMeters;

            if (roundedDownDist > ProbeReferenceVolume.instance.probeVolumeDebug.subdivisionViewCullingDistance)
                return true;

            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(activeCamera);
            var volumeAABB = new Bounds(cellCenterWS, cellSizeInMeters * Vector3.one);

            return !GeometryUtility.TestPlanesAABB(frustumPlanes, volumeAABB);
        }


        struct CellDebugData
        {
            public Vector4  center;
            public Color    color;
        }

        // Path to the gizmos location
        internal readonly static string s_gizmosLocationPath = "Packages/com.unity.render-pipelines.core/Editor/Resources/Gizmos/";

        // TODO: We need to get rid of Handles.DrawWireCube to be able to have those at runtime as well.
        void OnDrawGizmos()
        {
            Gizmos.DrawIcon(transform.position, s_gizmosLocationPath + "ProbeVolume.png", true);

            if (!ProbeReferenceVolume.instance.isInitialized || !IsResponsibleToDrawGizmo() || ProbeReferenceVolume.instance.sceneData == null)
                return;

            var debugDisplay = ProbeReferenceVolume.instance.probeVolumeDebug;

            float minBrickSize = ProbeReferenceVolume.instance.MinBrickSize();
            var cellSizeInMeters = ProbeReferenceVolume.instance.MaxBrickSize();
            if (debugDisplay.realtimeSubdivision)
            {
                var bakingSet = ProbeReferenceVolume.instance.sceneData.GetBakingSetForScene(gameObject.scene);
                if (bakingSet == null)
                    return;

                // Overwrite settings with data from profile
                minBrickSize = ProbeVolumeBakingSet.GetMinBrickSize(bakingSet.minDistanceBetweenProbes);
                cellSizeInMeters = ProbeVolumeBakingSet.GetCellSizeInBricks(bakingSet.simplificationLevels) * minBrickSize;
            }

            if (debugDisplay.drawBricks)
            {
                var subdivColors = ProbeReferenceVolume.instance.subdivisionDebugColors;

                IEnumerable<ProbeBrickIndex.Brick> GetVisibleBricks()
                {
                    if (debugDisplay.realtimeSubdivision)
                    {
                        // realtime subdiv cells are already culled
                        foreach (var kp in ProbeReferenceVolume.instance.realtimeSubdivisionInfo)
                        {
                            var cellVolume = kp.Key;

                            foreach (var brick in kp.Value)
                            {
                                yield return brick;
                            }
                        }
                    }
                    else
                    {
                        foreach (var cell in ProbeReferenceVolume.instance.cells.Values)
                        {
                            if (!cell.loaded)
                                continue;

                            if (ShouldCullCell(cell.desc.position, ProbeReferenceVolume.instance.GetTransform().posWS))
                                continue;

                            if (cell.data.bricks == null)
                                continue;

                            foreach (var brick in cell.data.bricks)
                                yield return brick;
                        }
                    }
                }

                if (brickGizmos == null)
                    brickGizmos = new MeshGizmo((int)(Mathf.Pow(3, ProbeBrickIndex.kMaxSubdivisionLevels) * MeshGizmo.vertexCountPerCube));

                brickGizmos.Clear();
                foreach (var brick in GetVisibleBricks())
                {
                    if (brick.subdivisionLevel < 0)
                        continue;

                    float brickSize = minBrickSize * ProbeReferenceVolume.CellSize(brick.subdivisionLevel);
                    Vector3 scaledSize = new Vector3(brickSize, brickSize, brickSize);
                    Vector3 scaledPos = new Vector3(brick.position.x * minBrickSize, brick.position.y * minBrickSize, brick.position.z * minBrickSize) + scaledSize / 2;
                    brickGizmos.AddWireCube(scaledPos, scaledSize, subdivColors[brick.subdivisionLevel]);
                }

                brickGizmos.RenderWireframe(Matrix4x4.identity, gizmoName: "Brick Gizmo Rendering");
            }

            if (debugDisplay.drawCells)
            {
                IEnumerable<CellDebugData> GetVisibleCellDebugData()
                {
                    Color s_LoadedColor = new Color(0, 1, 0.5f, 0.2f);
                    Color s_UnloadedColor = new Color(1, 0.0f, 0.0f, 0.2f);
                    Color s_StreamingColor = new Color(0.0f, 0.0f, 1.0f, 0.2f);
                    Color s_LowScoreColor = new Color(0, 0, 0, 0.2f);
                    Color s_HighScoreColor = new Color(1, 1, 0, 0.2f);

                    var prv = ProbeReferenceVolume.instance;

                    float minStreamingScore = prv.minStreamingScore;
                    float streamingScoreRange = prv.maxStreamingScore - prv.minStreamingScore;

                    if (debugDisplay.realtimeSubdivision)
                    {
                        foreach (var kp in prv.realtimeSubdivisionInfo)
                        {
                            var center = kp.Key.center;
                            yield return new CellDebugData { center = center, color = s_LoadedColor };
                        }
                    }
                    else
                    {
                        foreach (var cell in ProbeReferenceVolume.instance.cells.Values)
                        {
                            if (ShouldCullCell(cell.desc.position, prv.GetTransform().posWS))
                                continue;

                            var positionF = new Vector4(cell.desc.position.x, cell.desc.position.y, cell.desc.position.z, 0.0f);
                            var output = new CellDebugData();
                            output.center = positionF * cellSizeInMeters + cellSizeInMeters * 0.5f * Vector4.one;
                            if (debugDisplay.displayCellStreamingScore)
                            {
                                float lerpFactor = (cell.streamingInfo.streamingScore - minStreamingScore) / streamingScoreRange;
                                output.color = Color.Lerp(s_HighScoreColor, s_LowScoreColor, lerpFactor);
                            }
                            else
                            {
                                if (cell.streamingInfo.IsStreaming())
                                    output.color = s_StreamingColor;
                                else
                                    output.color = cell.loaded ? s_LoadedColor : s_UnloadedColor;
                            }
                            yield return output;
                        }
                    }
                }

                Matrix4x4 trs = Matrix4x4.TRS(ProbeReferenceVolume.instance.GetTransform().posWS, ProbeReferenceVolume.instance.GetTransform().rot, Vector3.one);
                var oldGizmoMatrix = Gizmos.matrix;

                if (cellGizmo == null)
                    cellGizmo = new MeshGizmo();
                cellGizmo.Clear();
                foreach (var cell in GetVisibleCellDebugData())
                {
                    Gizmos.color = cell.color;
                    Gizmos.matrix = trs;

                    Gizmos.DrawCube(cell.center, Vector3.one * cellSizeInMeters);
                    var wireColor = cell.color;
                    wireColor.a = 1.0f;
                    cellGizmo.AddWireCube(cell.center, Vector3.one * cellSizeInMeters, wireColor);
                }
                cellGizmo.RenderWireframe(Gizmos.matrix, gizmoName: "Brick Gizmo Rendering");
                Gizmos.matrix = oldGizmoMatrix;
            }
        }
        #endregion

#endif // UNITY_EDITOR
    }
} // UnityEngine.Rendering.HDPipeline
