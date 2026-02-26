using System;
using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Expression that, when evaluated, returns the current delta time.
    /// </summary>
    /*public*/ class DeltaTimeExpression : ValueExpression<float>
    {
        /// <inheritdoc cref="ValueExpression{T}"/>
        public override float Value => Time.deltaTime;
    }

    /// <summary>
    /// Expression that, when evaluated, returns the total time since the start of the effect.
    /// </summary>
    /*public*/ class TotalTimeExpression : ValueExpression<float>
    {
        /// <inheritdoc cref="ValueExpression{T}"/>
        public override float Value => Time.time;
    }
}
