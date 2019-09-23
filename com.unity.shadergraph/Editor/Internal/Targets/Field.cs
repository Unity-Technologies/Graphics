using System.Diagnostics;

namespace UnityEditor.ShaderGraph.Internal
{
    public class Field
    {
        public string containerName { get; }
        public string fieldName { get; }
        public string define { get; }

        public Field(string define, [System.Runtime.CompilerServices.CallerMemberName] string fieldName = "")
        {
            var method = new StackTrace().GetFrame(1).GetMethod();
            string containerName = method.DeclaringType.Name;

            this.containerName = containerName;
            this.fieldName = fieldName;
            this.define = define;
        }
    }
}
