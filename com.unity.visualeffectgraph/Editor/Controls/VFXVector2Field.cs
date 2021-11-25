using UnityEngine;
using UnityEngine.UIElements;

using System.Collections.Generic;

using FloatField = UnityEditor.VFX.UI.VFXLabeledField<UnityEngine.UIElements.FloatField, float>;

namespace UnityEditor.VFX.UI
{
    class VFXVector2Field : VFXVectorNField<Vector2>
    {
        protected override int componentCount { get { return 2; } }
        protected override void SetValueComponent(ref Vector2 value, int i, float componentValue)
        {
            switch (i)
            {
                case 0:
                    value.x = componentValue;
                    break;
                default:
                    value.y = componentValue;
                    break;
            }
        }

        protected override float GetValueComponent(ref Vector2 value, int i)
        {
            switch (i)
            {
                case 0:
                    return value.x;
                default:
                    return value.y;
            }
        }
    }
}
