using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    public struct NeededTransform
    {
        public static NeededTransform None => new NeededTransform(NeededCoordinateSpace.None, NeededCoordinateSpace.None);
        public static NeededTransform ObjectToWorld => new NeededTransform(NeededCoordinateSpace.Object, NeededCoordinateSpace.World);
        public static NeededTransform WorldToObject => new NeededTransform(NeededCoordinateSpace.Object, NeededCoordinateSpace.World);

        public NeededTransform(NeededCoordinateSpace from, NeededCoordinateSpace to)
        {
            this.from = from;
            this.to = to;
        }

        public NeededCoordinateSpace from;
        public NeededCoordinateSpace to;
    }

    interface IMayRequireTransform
    {
        NeededTransform RequiresTransform(ShaderStageCapability stageCapability = ShaderStageCapability.All);
    }

    static class MayRequireTransformExtensions
    {
        public static NeededTransform RequiresTransform(this MaterialSlot slot)
        {
            var mayRequireTransform = slot as IMayRequireTransform;
            return mayRequireTransform != null ? mayRequireTransform.RequiresTransform() : NeededTransform.None;
        }
    }
}
