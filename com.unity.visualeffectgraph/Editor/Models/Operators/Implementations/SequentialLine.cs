using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math")]
    class SequentialLine : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Element index used to loop over the sequence")]
            public uint Index = 0u;
            [Tooltip("Element count used to loop over the sequence")]
            public uint Count = 64;
            [Tooltip("Start Position")]
            public Position Start = Position.defaultValue;
            [Tooltip("End Position")]
            public Position End = new Position() { position = new Vector3(1, 0, 0) };
        }

        public class OutputProperties
        {
            public Position r = Position.defaultValue;
        }

        public override string name
        {
            get
            {
                return "Sequential Line";
            }
        }

        [SerializeField, VFXSetting]
        private VFXOperatorUtility.SequentialAddressingMode mode = VFXOperatorUtility.SequentialAddressingMode.Clamp;

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var index = inputExpression[0];
            var count = inputExpression[1];
            var start = inputExpression[2];
            var end = inputExpression[3];

            return new[] { VFXOperatorUtility.SequentialLine(start, end, index, count, mode) };
        }

        public override void Sanitize(int version)
        {
            var oldLineSlot = inputSlots.FirstOrDefault(o => o.name == "line");

            if (oldLineSlot != null)
            {
                RemoveSlot(oldLineSlot); //Avoid unlink
            }

            base.Sanitize(version);
            if (oldLineSlot != null)
            {
                var start = inputSlots.FirstOrDefault(o => o.name == "Start");
                var end = inputSlots.FirstOrDefault(o => o.name == "End");
                VFXSlot.CopyLinksAndValue(start, oldLineSlot.children.ElementAt(0), true);
                VFXSlot.CopyLinksAndValue(end, oldLineSlot.children.ElementAt(1), true);
                oldLineSlot.UnlinkAll();
            }
        }
    }
}
