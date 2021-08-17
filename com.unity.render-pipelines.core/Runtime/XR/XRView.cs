namespace UnityEngine.Experimental.Rendering
{
    internal readonly struct XRView
    {
        internal readonly Matrix4x4 projMatrix;
        internal readonly Matrix4x4 viewMatrix;
        internal readonly Rect viewport;
        internal readonly Mesh occlusionMesh;
        internal readonly int textureArraySlice;

        internal XRView(Matrix4x4 projMatrix, Matrix4x4 viewMatrix, Rect viewport, Mesh occlusionMesh, int textureArraySlice)
        {
            this.projMatrix = projMatrix;
            this.viewMatrix = viewMatrix;
            this.viewport = viewport;
            this.occlusionMesh = occlusionMesh;
            this.textureArraySlice = textureArraySlice;
        }
    }
}
