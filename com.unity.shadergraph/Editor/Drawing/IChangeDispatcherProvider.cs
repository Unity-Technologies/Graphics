using System;
using UnityEditor.ShaderGraph.Serialization;

namespace UnityEditor.ShaderGraph.Drawing
{
    interface IChangeDispatcherProvider
    {
        ChangeDispatcher changeDispatcher { get; }
    }
}
