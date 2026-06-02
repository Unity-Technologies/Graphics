using System.Collections.Generic;

namespace UnityEditor.VFX
{
    class CompositeSystemBuilder : SystemBuilder
    {
        protected List<SystemBuilder> m_Builders;

        public CompositeSystemBuilder()
        {
            m_Builders = new List<SystemBuilder>();
        }

        public CompositeSystemBuilder(params SystemBuilder[] builders)
        {
            m_Builders = new List<SystemBuilder>(builders);
        }

        public CompositeSystemBuilder(IEnumerable<SystemBuilder> builders)
        {
            m_Builders = new List<SystemBuilder>(builders);
        }

        public override void BeginBuilding(BuildContext context)
        {
            base.BeginBuilding(context);
            foreach (var builder in m_Builders)
            {
                builder.BeginBuilding(context);
            }
        }

        public override bool ProcessContext(VFXContext context)
        {
            bool processed = base.ProcessContext(context);
            foreach (var builder in m_Builders)
            {
                processed |= builder.ProcessContext(context);
            }
            return processed;
        }

        public override void Build()
        {
            foreach (var builder in m_Builders)
            {
                builder.Build();
            }
        }

        public override void EndBuilding()
        {
            foreach (var builder in m_Builders)
            {
                builder.EndBuilding();
            }
            base.EndBuilding();
        }
    }
}
