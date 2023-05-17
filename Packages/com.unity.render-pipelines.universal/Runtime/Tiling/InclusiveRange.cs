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

        public void Clamp(short min, short max)
        {
            start = Math.Max(min, start);
            end = Math.Min(max, end);
        }

        public bool isEmpty => end < start;

        public bool Contains(short index)
        {
            return index >= start && index <= end;
        }

        public static InclusiveRange Merge(InclusiveRange a, InclusiveRange b) => new(Math.Min(a.start, b.start), Math.Max(a.end, b.end));

        public static InclusiveRange empty => new InclusiveRange(short.MaxValue, short.MinValue);

        public override string ToString()
        {
            return $"[{start}, {end}]";
        }
    }
}
