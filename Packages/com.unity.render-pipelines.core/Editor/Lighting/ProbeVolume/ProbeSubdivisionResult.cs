using System.Collections.Generic;
using UnityEngine.SceneManagement;

using Brick = UnityEngine.Rendering.ProbeBrickIndex.Brick;

namespace UnityEngine.Rendering
{
    class ProbeSubdivisionResult
    {
        public List<(Vector3Int position, Bounds bounds, Brick[] bricks)> cells = new ();
        public Dictionary<Vector3Int, HashSet<string>> scenesPerCells = new Dictionary<Vector3Int, HashSet<string>>();
    }
}
