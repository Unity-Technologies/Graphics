namespace UnityEngine.Rendering
{
    /// <summary>Utility for tiles layout</summary>
    public static class TileLayoutUtils
    {
        /// <summary>Try decompose the givent rect into tiles given the parameter</summary>
        /// <param name="src">The rect to split</param>
        /// <param name="tileSize">The size of the tiles</param>
        /// <param name="main">Computed main area</param>
        /// <param name="topRow">Computed top row area</param>
        /// <param name="rightCol">Computed right column area</param>
        /// <param name="topRight">Computed top right corner area</param>
        /// <returns>If true, the tiles decomposition is a success</returns>
        public static bool TryLayoutByTiles(
            RectInt src,
            uint tileSize,
            out RectInt main,
            out RectInt topRow,
            out RectInt rightCol,
            out RectInt topRight)
        {
            if (src.width < tileSize || src.height < tileSize)
            {
                main = new RectInt(0, 0, 0, 0);
                topRow = new RectInt(0, 0, 0, 0);
                rightCol = new RectInt(0, 0, 0, 0);
                topRight = new RectInt(0, 0, 0, 0);
                return false;
            }

            int mainRows = src.height / (int)tileSize;
            int mainCols = src.width / (int)tileSize;
            int mainWidth = mainCols * (int)tileSize;
            int mainHeight = mainRows * (int)tileSize;

            main = new RectInt
            {
                x = src.x,
                y = src.y,
                width = mainWidth,
                height = mainHeight,
            };
            topRow = new RectInt
            {
                x = src.x,
                y = src.y + mainHeight,
                width = mainWidth,
                height = src.height - mainHeight
            };
            rightCol = new RectInt
            {
                x = src.x + mainWidth,
                y = src.y,
                width = src.width - mainWidth,
                height = mainHeight
            };
            topRight = new RectInt
            {
                x = src.x + mainWidth,
                y = src.y + mainHeight,
                width = src.width - mainWidth,
                height = src.height - mainHeight
            };

            return true;
        }

        /// <summary>Try decompose the givent rect into rows given the parameter</summary>
        /// <param name="src">The rect to split</param>
        /// <param name="tileSize">The size of the tiles</param>
        /// <param name="main">Computed main area</param>
        /// <param name="other">Computed other area</param>
        /// <returns>If true, the tiles decomposition is a success</returns>
        public static bool TryLayoutByRow(
            RectInt src,
            uint tileSize,
            out RectInt main,
            out RectInt other)
        {
            if (src.height < tileSize)
            {
                main = new RectInt(0, 0, 0, 0);
                other = new RectInt(0, 0, 0, 0);
                return false;
            }

            int mainRows = src.height / (int)tileSize;
            int mainHeight = mainRows * (int)tileSize;

            main = new RectInt
            {
                x = src.x,
                y = src.y,
                width = src.width,
                height = mainHeight,
            };
            other = new RectInt
            {
                x = src.x,
                y = src.y + mainHeight,
                width = src.width,
                height = src.height - mainHeight
            };

            return true;
        }

        /// <summary>Try decompose the givent rect into columns given the parameter</summary>
        /// <param name="src">The rect to split</param>
        /// <param name="tileSize">The size of the tiles</param>
        /// <param name="main">Computed main area</param>
        /// <param name="other">Computed other area</param>
        /// <returns>If true, the tiles decomposition is a success</returns>
        public static bool TryLayoutByCol(
            RectInt src,
            uint tileSize,
            out RectInt main,
            out RectInt other)
        {
            if (src.width < tileSize)
            {
                main = new RectInt(0, 0, 0, 0);
                other = new RectInt(0, 0, 0, 0);
                return false;
            }

            int mainCols = src.width / (int)tileSize;
            int mainWidth = mainCols * (int)tileSize;

            main = new RectInt
            {
                x = src.x,
                y = src.y,
                width = mainWidth,
                height = src.height,
            };
            other = new RectInt
            {
                x = src.x + mainWidth,
                y = src.y,
                width = src.width - mainWidth,
                height = src.height
            };

            return true;
        }
    }
}
