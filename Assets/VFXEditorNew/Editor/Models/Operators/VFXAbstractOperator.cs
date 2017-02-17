using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    abstract class VFXOperatorUnaryFloatOperation : VFXOperator
    {
        public class Properties
        {
            public float input = 0.0f;
        }
    }

    abstract class VFXOperatorBinaryFloatOperation : VFXOperator
    {
        protected override void OnInvalidate(InvalidationCause cause)
        {
            var newInputSlots = InputSlots.ToList();

            //Remove useless unplugged slot (ensuring there is at least 2 slots)
            for (int slotIndex = newInputSlots.Count - 1; slotIndex >= 2; --slotIndex)
            {
                var currentSlot = newInputSlots[slotIndex];
                if (currentSlot.parent == null)
                {
                    newInputSlots.RemoveAt(slotIndex);
                }
            }

            if (newInputSlots.All(s => s.parent != null))
            {
                var lastElement = newInputSlots.Last();
                //Add new available slot element
                newInputSlots.Add(new VFXMitoSlotInput()
                {
                    name = lastElement.name,
                    type = lastElement.type
                });
            }
            InputSlots = newInputSlots.ToArray();

            IEnumerable<VFXExpression> inputExpression = GetInputExpressions();

            //Process aggregate two by two element until result
            var outputExpression = new Stack<VFXExpression>(inputExpression.Reverse());
            while (outputExpression.Count > 1)
            {
                var a = outputExpression.Pop();
                var b = outputExpression.Pop();
                var compose = BuildExpression(new[] { a, b })[0];
                outputExpression.Push(compose);
            }
            OutputSlots = BuildOuputSlot(outputExpression).ToArray();
        }
    }

    abstract class VFXOperatorBinaryFloatOperationOne : VFXOperatorBinaryFloatOperation
    {
        public class Properties
        {
            public float right = 1.0f;
            public float left = 1.0f;
        }
    }

    abstract class VFXOperatorBinaryFloatOperationZero : VFXOperatorBinaryFloatOperation
    {
        public class Properties
        {
            public float right = 0.0f;
            public float left = 0.0f;
        }
    }
}