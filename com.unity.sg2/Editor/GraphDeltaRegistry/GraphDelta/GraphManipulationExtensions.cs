using System;
using System.Collections.Generic;
using UnityEditor.ContextLayeredDataStorage;
using static UnityEditor.ShaderGraph.GraphDelta.GraphStorage;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public static class GraphManipulationExtensions
    {

        public static bool TryGetData<T>(this PortHandler port, string dataName, out T dataValue)
        {
            dataValue = default;
            var field = port.GetField<T>(dataName);
            if (field != null)
            {
                dataValue = field.GetData();
                return true;
            }
            return false;
        }

    }
}
