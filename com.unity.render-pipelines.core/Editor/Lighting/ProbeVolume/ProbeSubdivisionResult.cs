using System.Collections.Generic;
using Unity.Collections;
using System;
using UnityEditor;
using Brick = UnityEngine.Experimental.Rendering.ProbeBrickIndex.Brick;
using UnityEngine.SceneManagement;

namespace UnityEngine.Experimental.Rendering
{
    class ProbeSubdivisionResult
    {
        public List<Vector3Int> cellPositions = new List<Vector3Int>();
        public Dictionary<Vector3Int, List<Brick>> bricksPerCells = new Dictionary<Vector3Int, List<Brick>>();
        public Dictionary<Vector3Int, HashSet<Scene>> scenesPerCells = new Dictionary<Vector3Int, HashSet<Scene>>();
    }
}
