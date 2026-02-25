
namespace Unity.GraphCommon.LowLevel.Editor
{
    class Task : ITask
    {
        public string DebugName { get; set; } = "";

        public Task(string name)
        {
            DebugName = name;
        }
    }
}
