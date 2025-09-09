
namespace UnityEngine.PathTracing.Core
{
    internal static class ShaderProperties
    {
        public static readonly int LightGrid = Shader.PropertyToID("g_LightGrid");
        public static readonly int LightGridCellsData = Shader.PropertyToID("g_LightGridCellsData");
        public static readonly int GridDimX = Shader.PropertyToID("g_GridDimX");
        public static readonly int GridDimY = Shader.PropertyToID("g_GridDimY");
        public static readonly int GridDimZ = Shader.PropertyToID("g_GridDimZ");
        public static readonly int NumLights = Shader.PropertyToID("g_NumLights");
        public static readonly int GridMin = Shader.PropertyToID("g_GridMin");
        public static readonly int GridSize = Shader.PropertyToID("g_GridSize");
        public static readonly int CellSize = Shader.PropertyToID("g_CellSize");
        public static readonly int InvCellSize = Shader.PropertyToID("g_InvCellSize");
        public static readonly int TotalReservoirCount = Shader.PropertyToID("g_TotalLightsInGridCount");
        public static readonly int BuildPass = Shader.PropertyToID("g_BuildPass");
        public static readonly int NumCandidates = Shader.PropertyToID("g_NumCandidates");
        public static readonly int NumReservoirs = Shader.PropertyToID("g_NumReservoirs");
        public static readonly int MaxLightsPerCell = Shader.PropertyToID("g_MaxLightsPerCell");
        public static readonly int LightList = Shader.PropertyToID("g_LightList");
        public static readonly int NumEmissiveMeshes = Shader.PropertyToID("g_NumEmissiveMeshes");
    }
}
