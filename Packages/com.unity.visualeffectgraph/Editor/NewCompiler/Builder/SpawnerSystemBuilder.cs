using System.Collections.Generic;
using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX
{
    class SpawnerSystemBuilder : SystemBuilder
    {
        static UniqueDataKey EventListDataBindingKey { get; } = new UniqueDataKey(nameof(EventListDataBindingKey));

        List<VFXBasicSpawner> m_Spawners = new List<VFXBasicSpawner>();

        public override bool ProcessContext(VFXContext context)
        {
            var processed = base.ProcessContext(context);
            if (context is VFXBasicSpawner spawner)
            {
                m_Spawners.Add(spawner);
                processed = true;
            }
            return processed;
        }

        public override void Build()
        {
            foreach (var spawner in m_Spawners)
            {
                BuildSpawnerSystem(spawner);
            }
        }

        public override void EndBuilding()
        {
            m_Spawners.Clear();
            base.EndBuilding();
        }

        void BuildSpawnerSystem(VFXBasicSpawner spawner)
        {
            var eventDataDescription = new EventData(SystemNames.GetUniqueSystemName(spawner.GetData()));
            var eventData = TaskGraphBuilder.AddData(eventDataDescription.Name, eventDataDescription);
            var eventAttributeData = TaskGraphBuilder.GetSubdata(eventData, EventData.AttributeDataKey);

            var cpuMapper = ExpressionGraph.BuildCPUMapper(spawner);

            int taskIndex = 0;
            foreach (var block in spawner.activeFlattenedChildrenWithImplicit)
            {
                if (block is VFXAbstractSpawner spawnerBlock)
                {
                    SpawnerTask subTask = null;
                    if (spawnerBlock is Block.VFXSpawnerSetAttribute spawnerSetAttribute)
                    {
                        var attribute = VFXAttributesManager.ConvertToNewCompiler(spawnerSetAttribute.currentAttribute); ;
                        TaskGraphBuilder.GetSubdata(eventAttributeData, new AttributeKey(attribute));
                        subTask = new SpawnerTask(spawnerBlock.spawnerType, EventListDataBindingKey, attribute);
                    }
                    else
                    {
                        subTask = new SpawnerTask(spawnerBlock.spawnerType, EventListDataBindingKey);
                    }

                    var spawnerTaskNodeId = TaskGraphBuilder.AddTask(subTask, block.name);
                    TaskGraphBuilder.BindData(spawnerTaskNodeId, EventListDataBindingKey, eventData);
                    BindSpawnerExpressions(cpuMapper, spawnerTaskNodeId, taskIndex);

                    taskIndex++;
                }
            }

            // Register event data so other builders can retrieve it
            RegisterDataView(spawner, eventData);
        }

        void BindSpawnerExpressions(VFXExpressionMapper cpuMapper, TaskNodeId spawnerTaskNodeId, int taskIndex)
        {
            // For spawner, we only bind CPU expressions and we do not use the full name for the binding.
            foreach (var namedExpression in cpuMapper.CollectExpression(taskIndex, false))
            {
                TryGetDataView(namedExpression.exp, out var expressionDataViewId);
                string bindingName = namedExpression.name;
                TaskGraphBuilder.BindData(spawnerTaskNodeId, new NameDataKey(bindingName), expressionDataViewId, BindingUsage.Read);
            }
        }
    }
}
