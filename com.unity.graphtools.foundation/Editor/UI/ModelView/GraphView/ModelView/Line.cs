using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    struct Line
    {
        public Vector2 Start;
        public Vector2 End;
        public Line(Vector2 start, Vector2 end)
        {
            Start = start;
            End = end;
        }
    }
}
