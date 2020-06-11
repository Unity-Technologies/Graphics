namespace UnityEngine.Experimental.Rendering.Universal
{
    internal struct OutlineSearchNode // might want to check speed if packed into a int4 instead of a structure
    {
        public int x;
        public int y;
        public int shapeIndex;
        public int contourIndex;
    }
}
