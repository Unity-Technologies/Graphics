using System;

namespace UnityEngine.Rendering.Universal
{
    struct InclusiveRange
    {
        public short start;
        public short end;

        public InclusiveRange(short startEnd)
        {
            this.start = startEnd;
            this.end = startEnd;
        }

        public InclusiveRange(short start, short end)
        {
            this.start = start;
            this.end = end;
        }

        public void Expand(short index)
        {
            start = Math.Min(start, index);
            end = Math.Max(end, index);
        }
    }
}
