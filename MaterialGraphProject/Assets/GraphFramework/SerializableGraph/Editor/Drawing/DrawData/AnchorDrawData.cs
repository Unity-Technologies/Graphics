using System;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    internal static class MyNodeAdapters
    {
        internal static bool Adapt(this NodeAdapter value, PortSource<int> a, PortSource<int> b)
        {
            // run adapt code for int to int connections
            return true;
        }

        internal static bool Adapt(this NodeAdapter value, PortSource<float> a, PortSource<float> b)
        {
            // run adapt code for float to float connections
            return true;
        }

        internal static bool Adapt(this NodeAdapter value, PortSource<int> a, PortSource<float> b)
        {
            // run adapt code for int to float connections, perhaps by insertion a conversion node
            return true;
        }

        internal static bool Adapt(this NodeAdapter value, PortSource<Vector3> a, PortSource<Vector3> b)
        {
            // run adapt code for vec3 to vec3 connections
            return true;
        }

        internal static bool Adapt(this NodeAdapter value, PortSource<Color> a, PortSource<Color> b)
        {
            // run adapt code for Color to Color connections
            return true;
        }

        internal static bool Adapt(this NodeAdapter value, PortSource<Vector3> a, PortSource<Color> b)
        {
            // run adapt code for vec3 to Color connections
            return true;
        }

        internal static bool Adapt(this NodeAdapter value, PortSource<Color> a, PortSource<Vector3> b)
        {
            // run adapt code for Color to vec3 connections
            return true;
        }
        internal static bool Adapt(this NodeAdapter value, PortSource<Vector4> a, PortSource<Vector4> b)
        {
            return true;
        }
    }

    [Serializable]
    public class AnchorDrawData : NodeAnchorData
    {
        protected AnchorDrawData()
        {}

        public ISlot slot { get; private set; }

        public void Initialize(ISlot slot)
        {
            this.slot = slot;
            name = slot.displayName;
            type = typeof(Vector4);
            direction = slot.isInputSlot ? Direction.Input : Direction.Output;
        }
    }
}
