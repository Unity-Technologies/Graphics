using UnityEngine;

namespace UnityEditor.VFX.UI
{
    class VFXVector4Field : VFXVectorNField<Vector4>
    {
        protected override int componentCount => 4;

        protected override void SetValueComponent(ref Vector4 value, int i, float componentValue)
        {
            switch (i)
            {
                case 0:
                    value.x = componentValue;
                    break;
                case 1:
                    value.y = componentValue;
                    break;
                case 2:
                    value.z = componentValue;
                    break;
                default:
                    value.w = componentValue;
                    break;
            }
        }

        protected override float GetValueComponent(ref Vector4 value, int i)
        {
            switch (i)
            {
                case 0:
                    return value.x;
                case 1:
                    return value.y;
                case 2:
                    return value.z;
                default:
                    return value.w;
            }
        }
    }
}
