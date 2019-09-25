namespace UnityEditor.ShaderGraph.Internal
{
    public enum SubscriptOptions
    {
        Static = 0,
        Optional = 1 << 0, 
        Generated = 1 << 1
    }
    public class SubscriptDescriptor : IField
    {
        public string tag { get; }
        public string name { get; }
        public string define { get; }
        public string type { get; }
        public int vectorCount { get; }
        public string semantic { get; }
        public string preprocessor { get; }
        public SubscriptOptions subscriptOptions { get; }

        public SubscriptDescriptor(string tag, string name, string define, ShaderValueType type,
                string semantic = "", string preprocessor = "", SubscriptOptions subscriptOptions = SubscriptOptions.Static)
        {
            this.tag = tag;
            this.name = name;
            this.define = define;
            this.type = type.ToShaderString();
            this.vectorCount = type.GetVectorCount();
            this.semantic = semantic;
            this.preprocessor = preprocessor;
            this.subscriptOptions = subscriptOptions;
        }

        public SubscriptDescriptor(string tag, string name, string define, string type,
                string semantic = "", string preprocessor = "", SubscriptOptions subscriptOptions = SubscriptOptions.Static)
        {
            this.tag = tag;
            this.name = name;
            this.define = define;
            this.type = type;
            this.vectorCount = 0;
            this.semantic = semantic;            
            this.preprocessor = preprocessor;
            this.subscriptOptions = subscriptOptions;
        }

        public bool hasPreprocessor()
        {
            return (this.preprocessor.Length > 0);
        }

        public bool hasSemantic()
        {
            return (this.semantic.Length > 0);
        }

        public bool hasFlag(SubscriptOptions options)
        {
            return (this.subscriptOptions & options) == options;
        }
    }
}
