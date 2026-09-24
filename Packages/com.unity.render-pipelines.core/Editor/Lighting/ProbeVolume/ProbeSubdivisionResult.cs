using System.Collections.Generic;

using Brick = UnityEngine.Rendering.ProbeBrickIndex.Brick;

namespace UnityEngine.Rendering
{
    class ProbeSubdivisionResult
    {
        public List<(Vector3Int position, Bounds bounds, Brick[] bricks)> cells = new();
        public Dictionary<Vector3Int, HashSet<GUID>> scenesPerCells = new Dictionary<Vector3Int, HashSet<GUID>>();
        public GIContributors? contributors = null;
    }
}
