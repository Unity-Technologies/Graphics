using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class TypeHandleUtil
    {
        public static GraphType.Height GetGraphTypeHeight(TypeHandle th)
        {
            if (th == ShaderGraphExampleTypes.Matrix4) return GraphType.Height.Four;
            if (th == ShaderGraphExampleTypes.Matrix3) return GraphType.Height.Three;
            if (th == ShaderGraphExampleTypes.Matrix2) return GraphType.Height.Two;

            return GraphType.Height.One;
        }

        public static GraphType.Length GetGraphTypeLength(TypeHandle th)
        {
            if (th == ShaderGraphExampleTypes.Matrix4 || th == TypeHandle.Vector4 || th == ShaderGraphExampleTypes.Color) return GraphType.Length.Four;
            if (th == ShaderGraphExampleTypes.Matrix3 || th == TypeHandle.Vector3) return GraphType.Length.Three;
            if (th == ShaderGraphExampleTypes.Matrix2 || th == TypeHandle.Vector2) return GraphType.Length.Two;

            return GraphType.Length.One;
        }

        public static GraphType.Primitive GetGraphTypePrimitive(TypeHandle th)
        {
            if (th == TypeHandle.Bool) return GraphType.Primitive.Bool;
            if (th == TypeHandle.Int) return GraphType.Primitive.Int;

            return GraphType.Primitive.Float;
        }
    }
}
