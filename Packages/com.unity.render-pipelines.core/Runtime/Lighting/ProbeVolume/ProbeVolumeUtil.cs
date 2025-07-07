namespace UnityEngine.Rendering
{
    static class ProbeVolumeUtil
    {
        internal static int CellSize(int subdivisionLevel) => (int)Mathf.Pow(ProbeBrickPool.kBrickCellCount, subdivisionLevel);
        internal static float BrickSize(float minBrickSize, int subdivisionLevel) => minBrickSize * CellSize(subdivisionLevel);
        internal static float MaxBrickSize(float minBrickSize, int maxSubDivision) => BrickSize(minBrickSize, maxSubDivision - 1);
    }
}
