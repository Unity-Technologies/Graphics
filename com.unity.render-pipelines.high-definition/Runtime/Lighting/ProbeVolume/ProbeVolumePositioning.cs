using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition
{
    internal class ProbeVolumePositioning
    {
        // Grid info
        public float MinCellSize { get; set; } = 5f;
        public Vector3Int GridResolution { get; private set; }
        public Bounds ReferenceBounds { get; private set; }

        // Current subdivision level
        private List<ProbeVolumeBrick> level;

        // Output: Bricks in grid space, an index buffer and probe positions in world space
        public List<ProbeVolumeBrick> Bricks { get; private set; }
        public int[] IndexBuffer { get; private set; }
        public Vector3[] ProbePositions { get; private set; }

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
            this.Bricks = new List<ProbeVolumeBrick>();
            this.IndexBuffer = new int[GridResolution.x * GridResolution.y * GridResolution.z];

            // Build indirection
            SubDivideGrid();
            CalculateProbePositions();

            // Dirty debug drawing to force redraw
            ProbeVolumeDebugDrawing.drawing.Dirty();
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

        private void CalculateProbePositions()
        {
            ProbePositions = new Vector3[Bricks.Count * 64];

            for (int i = 0; i < Bricks.Count; i++)
            {
                Vector3Int origin = Bricks[i].Position;

                for (int j = 0; j < 64; j++)
                {
                    Vector3 offset = (Vector3)Position3D(4, 4, j) / 3f * Bricks[i].size * MinCellSize;
                    ProbePositions[i * 64 + j] = GridToWorld(origin) + offset;
                }
            }
        }

        private void SubDivideGrid()
        {
            level = new List<ProbeVolumeBrick>();

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
                            level.Add(new ProbeVolumeBrick(pos, size));
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
            foreach (ProbeVolumeBrick brick in level)
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
            List<ProbeVolumeBrick> result = new List<ProbeVolumeBrick>();

            // Subdivide into new level
            for (int i = 0; i < level.Count; i++)
            {
                ProbeVolumeBrick brick = level[i];
                if (brick.size >= 3)
                {
                    int thirdSize = brick.size / 3;

                    for (int b = 0; b < 27; b++)
                    {
                        Vector3Int offset = Position3D(3, 3, b) * thirdSize;

                        result.Add(new ProbeVolumeBrick(brick.Position + offset, thirdSize));
                    }
                }
            }
            
            // Only keep some bricks
            if (result.Count > 0)
            {
                for (int i = result.Count-1; i >= 0; i--)
                {
                    if (!ShouldKeepBrick(result[i]))
                    {
                        result.RemoveAt(i);
                    }
                }
            }

            level = result;
        }

        // TODO: Add subdivision criteria here,
        // currently just keeps subdividing inside probe volumes
        private bool ShouldKeepBrick(ProbeVolumeBrick brick)
        {
            return IntersectsProbeVolume(brick);
        }

        private bool IsDoneSubDividing()
        {
            return level.Count == 0;
        }

        // TODO: This should probably go somewhere else
        private Bounds GetProbeVolumeBounds(ProbeVolume volume)
        {
            var OBB = new OrientedBBox(Matrix4x4.TRS(volume.transform.position, volume.transform.rotation, volume.parameters.size));

            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (int i = 0; i < 8; i++)
            {
                Vector3 dir = (Position3D(2, 2, i) - new Vector3(0.5f, 0.5f, 0.5f)) * 2f;

                Vector3 pt = OBB.center
                    + OBB.right * OBB.extentX * dir.x
                    + OBB.up * OBB.extentY * dir.y
                    + OBB.forward * OBB.extentZ * dir.z;

                min = Vector3.Min(min, pt);
                max = Vector3.Max(max, pt);
            }

            return new Bounds(
                (min + max) / 2,
                max - min);
        }

        // TODO: Full OBB-OBB collision, perhaps using SAT
        private bool IntersectsProbeVolume(ProbeVolumeBrick brick)
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
    public struct ProbeVolumeBrick
    {
        public ushort x;
        public ushort y;
        public ushort z;
        public ushort size;

        public Vector3Int Position
        {
            get { return new Vector3Int(x, y, z); }
        }

        public ProbeVolumeBrick(Vector3Int pos, int size)
        {
            this.x = (ushort)pos.x;
            this.y = (ushort)pos.y;
            this.z = (ushort)pos.z;
            this.size = (ushort)size;
        }
    }
}
