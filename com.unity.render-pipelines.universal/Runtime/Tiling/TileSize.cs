namespace UnityEngine.Rendering.Universal
{
    internal enum TileSize
    {
        _8 = 8,
        _16 = 16,
        _32 = 32,
        _64 = 64
    }

    static class TileSizeExtensions
    {
        public static bool IsValid(this TileSize tileSize)
        {
            return tileSize == TileSize._8 || tileSize == TileSize._16 || tileSize == TileSize._32 || tileSize == TileSize._64;
        }
    }
}
