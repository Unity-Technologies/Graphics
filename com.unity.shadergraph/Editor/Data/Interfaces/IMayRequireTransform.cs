using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    public struct NeededTransform
    {
        static Dictionary<UnityMatrixType, NeededTransform> s_TransformMap = new Dictionary<UnityMatrixType, NeededTransform>
        {
            {UnityMatrixType.Model, ObjectToWorld},
            {UnityMatrixType.InverseModel, WorldToObject},

            // TODO: Define the rest.
            {UnityMatrixType.View, None},
            {UnityMatrixType.InverseView, None},
            {UnityMatrixType.Projection, None},
            {UnityMatrixType.InverseProjection, None},
            {UnityMatrixType.ViewProjection, None},
            {UnityMatrixType.InverseViewProjection, None},
        };

        public static NeededTransform None => new NeededTransform(NeededCoordinateSpace.None, NeededCoordinateSpace.None);
        public static NeededTransform ObjectToWorld => new NeededTransform(NeededCoordinateSpace.Object, NeededCoordinateSpace.World);
        public static NeededTransform WorldToObject => new NeededTransform(NeededCoordinateSpace.World, NeededCoordinateSpace.Object);

        public NeededTransform(NeededCoordinateSpace from, NeededCoordinateSpace to)
        {
            this.from = from;
            this.to = to;
        }

        // Secondary constructor for certain nodes like TransformationMatrix.
        internal NeededTransform(UnityMatrixType matrix)
        {
            if (s_TransformMap.TryGetValue(matrix, out var transform))
            {
                from = transform.from;
                to = transform.to;
            }
            else
            {
                from = NeededCoordinateSpace.None;
                to = NeededCoordinateSpace.None;
            }
        }

        public NeededCoordinateSpace from;
        public NeededCoordinateSpace to;
    }

    interface IMayRequireTransform
    {
        NeededTransform[] RequiresTransform(ShaderStageCapability stageCapability = ShaderStageCapability.All);
    }
}
