using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Bridge
{
    static class MovedFromAttributeExtensions
    {
        public static void GetData(this MovedFromAttribute @this, out bool autoUpdateAPI, out string sourceNamespace, out string sourceAssembly, out string sourceClassName)
        {
            autoUpdateAPI = @this.data.autoUdpateAPI;
            sourceNamespace = @this.data.nameSpace;
            sourceAssembly = @this.data.assembly;
            sourceClassName = @this.data.className;
        }
    }
}
