using System.Collections.Generic;
using Unity.Collections;
using System;
using UnityEditor;
using Brick = UnityEngine.Rendering.ProbeBrickIndex.Brick;
using UnityEngine.SceneManagement;

namespace UnityEngine.Rendering
{
    class ProbeSubdivisionResult
    {
        public List<(Vector3Int position, Bounds bounds)> cellPositionsAndBounds = new List<(Vector3Int, Bounds)>();
        public Dictionary<Vector3Int, List<Brick>> bricksPerCells = new Dictionary<Vector3Int, List<Brick>>();
        public Dictionary<Vector3Int, HashSet<Scene>> scenesPerCells = new Dictionary<Vector3Int, HashSet<Scene>>();
    }
}
