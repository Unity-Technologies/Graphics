using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition
{
    internal class ProbeVolumePositioning
    {
        // Grid info
        public const float MinCellSize = 0.5f;
        public Vector3Int GridResolution { get; private set; }
        public Bounds ReferenceBounds { get; private set; }

        // Current subdivision level
        private List<Brick> level;

        // Output: Bricks in grid space, and an index buffer
        public List<Brick> Bricks { get; private set; }
        public int[] IndexBuffer { get; private set; }

        public bool DebugDraw { get; set; } = false;

        internal ProbeVolumePositioning()
        {
            //TODO: Debug elements should probably be rendered elsewhere
            SceneView.duringSceneGui += delegate
            {
                if (ReferenceBounds != null && Bricks != null && DebugDraw)
                {
                    Handles.color = Color.red;
                    Handles.DrawWireCube(ReferenceBounds.center, ReferenceBounds.size);

                    Handles.color = Color.blue;
                    foreach (Brick b in Bricks)
                    {
                        // Don't draw all bricks - without instanced rendering that would be way too many.
                        if (b.size < 9)
                            continue;

                        Vector3 scaledSize = new Vector3(b.size, b.size, b.size) * MinCellSize;
                        Vector3 scaledPos = GridToWorld(b.Position) + scaledSize / 2;

                        Handles.DrawWireCube(scaledPos, scaledSize);
                    }
                }
            };
        }

        public void BuildBrickStructure(Bounds referenceBounds)
        {
            this.ReferenceBounds = referenceBounds;

            // Calculate resolution
            Vector3 resolutionFloat = referenceBounds.size / MinCellSize;
            this.GridResolution = new Vector3Int(
                Mathf.CeilToInt(resolutionFloat.x),
                Mathf.CeilToInt(resolutionFloat.y),
                Mathf.CeilToInt(resolutionFloat.z));

            // Set up output
            this.Bricks = new List<Brick>();
            this.IndexBuffer = new int[GridResolution.x * GridResolution.y * GridResolution.z];

            // Build indirection
            SubDivideGrid();
        }

        public void BuildBrickStructure()
        {
            if (ProbeVolumeManager.manager.volumes.Count > 0)
            {
                // Union volumes into bounding box
                Bounds bounds = GetProbeVolumeBounds(ProbeVolumeManager.manager.volumes[0]);
                for (int i = 1; i < ProbeVolumeManager.manager.volumes.Count; i++)
                {
                    bounds.Encapsulate(GetProbeVolumeBounds(ProbeVolumeManager.manager.volumes[i]));
                }

                BuildBrickStructure(bounds);
            }
            else
            {
                BuildBrickStructure(new Bounds());
            }
        }

        private void SubDivideGrid()
        {
            level = new List<Brick>();

            int minSize = Mathf.Min(GridResolution.x, Mathf.Min(GridResolution.y, GridResolution.z));
            int size = (int)Mathf.Pow(3, Mathf.Ceil(Mathf.Log(minSize, 3)));
            Vector3 logicalBrickRes = (Vector3)GridResolution / size;

            for (int x = 0; x < Mathf.CeilToInt(logicalBrickRes.x); x++)
            {
                for (int y = 0; y < Mathf.CeilToInt(logicalBrickRes.y); y++)
                {
                    for (int z = 0; z < Mathf.CeilToInt(logicalBrickRes.z); z++)
                    {
                        Vector3Int pos = new Vector3Int(x, y, z) * size;
                    
                        if (!OutOfBounds(pos))
                        {
                            level.Add(new Brick(pos, size));
                        }
                    }
                }
            }

            // Subdivide until finished
            do
            {
                SubDivideLevel();
                UpdateIndexBuffer();
            }
            while (!IsDoneSubDividing());
        }

        private void UpdateIndexBuffer()
        {
            foreach (Brick brick in level)
            {
                if (OutOfBounds(brick.Position)) continue;

                Bricks.Add(brick);

                for (int x = 0; x < brick.size; x++)
                {
                    for (int y = 0; y < brick.size; y++)
                    {
                        for (int z = 0; z < brick.size; z++)
                        {
                            Vector3Int posInBrick = brick.Position + new Vector3Int(x, y, z);

                            if (OutOfBounds(posInBrick)) continue;

                            IndexBuffer[Index3D(GridResolution.x, GridResolution.y, posInBrick)] = Bricks.Count - 1;
                        }
                    }
                }
            }
        }

        private void SubDivideLevel()
        {
            List<Brick> result = new List<Brick>();

            // Subdivide into new level
            for (int i = 0; i < level.Count; i++)
            {
                Brick brick = level[i];
                if (brick.size >= 3)
                {
                    int thirdSize = brick.size / 3;

                    for (int b = 0; b < 27; b++)
                    {
                        Vector3Int offset = Position3D(3, 3, b) * thirdSize;

                        result.Add(new Brick(brick.Position + offset, thirdSize));
                    }
                }
            }

            // Only keep some bricks
            if (result.Count > 0)
            {
                for (int i = result.Count-1; i >= 0; i--)
                {
                    // TODO: Add subdivision criteria here,
                    // just keeps subdividing inside probe volumes
                    if (!IntersectsProbeVolume(result[i]))
                    {
                        result.RemoveAt(i);
                    }
                }
            }

            level = result;
        }

        private bool IsDoneSubDividing()
        {
            return level.Count == 0;
        }

        // TODO: This should probably go somewhere else
        private Bounds GetProbeVolumeBounds(ProbeVolume volume)
        {
            return new Bounds(
                volume.transform.position,
                volume.GetComponent<ProbeVolume>().parameters.size);
        }

        private bool IntersectsProbeVolume(Brick brick)
        {
            Vector3 scaledSize = new Vector3(brick.size, brick.size, brick.size) * MinCellSize;
            Vector3 scaledPos = GridToWorld(brick.Position) + scaledSize / 2;
            Bounds bounds = new Bounds(scaledPos, scaledSize);

            bool result = false;
            foreach (ProbeVolume v in ProbeVolumeManager.manager.volumes)
            {
                if (bounds.Intersects(GetProbeVolumeBounds(v)))
                {
                    result = true;
                }
            }
            return result;
        }

        public Vector3 GridToWorld(Vector3 gridPos)
        {
            return gridPos * MinCellSize - (ReferenceBounds.size / 2) + ReferenceBounds.center;
        }

        private Vector3Int Position3D(int width, int height, int idx)
        {
            int x = idx % width;
            int y = (idx / width) % height;
            int z = idx / (width * height);

            return new Vector3Int(x, y, z);
        }

        private int Index3D(int width, int height, Vector3Int pos)
        {
            return pos.x + width * (pos.y + height * pos.z);
        }

        private bool OutOfBounds(Vector3Int gridPos)
        {
            return (gridPos.x < 0 || gridPos.y < 0 || gridPos.z < 0 ||
                    gridPos.x >= GridResolution.x || gridPos.y >= GridResolution.y || gridPos.z >= GridResolution.z);
        }
    }

    // Brick struct - currently 8 bytes
    // Bricks are defined by a corner position and a size
    public struct Brick
    {
        public ushort x;
        public ushort y;
        public ushort z;
        public ushort size;

        public Vector3Int Position
        {
            get { return new Vector3Int(x, y, z); }
        }

        public Brick(Vector3Int pos, int size)
        {
            this.x = (ushort)pos.x;
            this.y = (ushort)pos.y;
            this.z = (ushort)pos.z;
            this.size = (ushort)size;
        }
    }
}
