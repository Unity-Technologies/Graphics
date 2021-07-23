using System;
using UnityEngine.Jobs;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.HighDefinition
{
    internal struct HDLightEntity
    {
        public int index;
        public int version;

        public static readonly HDLightEntity Invalid = new HDLightEntity() { index = -1, version = -1 };

        public bool valid { get { return index != -1; } }
    }

    internal class HDLightSoAComponents
    {
        public const int ArrayCapacity = 100;
        private int m_Capacity = ArrayCapacity;
        private HDLightEntity[]  m_LightEntities    = null;
        private LightAccessArray m_LightAccessArray;

        public void Dispose()
        {
            m_LightAccessArray.Dispose();
        }

        public int Allocate(Light light)
        {
            if (!m_LightAccessArray.isCreated)
            {
                m_LightAccessArray = new LightAccessArray(ArrayCapacity);
                m_LightEntities = new HDLightEntity[ArrayCapacity];
            }
            else if (m_LightAccessArray.length == m_Capacity)
            {
                m_Capacity *= 2;
                m_LightAccessArray.capacity = m_Capacity;
                Array.Resize(ref m_LightEntities, m_Capacity);
            }

            int nextIndex = m_LightAccessArray.length;
            m_LightEntities[nextIndex] = HDLightEntity.Invalid;
            m_LightAccessArray.Add(light);
            return nextIndex;
        }

        public void DeleteArrays()
        {
            if (!m_LightAccessArray.isCreated)
                return;

            m_LightAccessArray.Dispose();
            m_LightEntities = null;
            m_Capacity = ArrayCapacity;
        }

        public void SetEntity(int arrayIndex, HDLightEntity entity)
        {
            m_LightEntities[arrayIndex] = entity;
        }

        public HDLightEntity RemoveAtSwapBack(int removeIndexAt)
        {
            int lastIndex = m_LightAccessArray.length - 1;
            m_LightAccessArray.RemoveAtSwapBack(removeIndexAt);

            var entity = m_LightEntities[lastIndex];
            m_LightEntities[removeIndexAt] = entity;
            if (m_LightAccessArray.length == 0)
            {
                DeleteArrays();
                return HDLightEntity.Invalid;
            }

            return entity;
        }

        public ref LightAccessArray GetLightAccessArray()
        {
            return ref m_LightAccessArray;
        }

        ~HDLightSoAComponents()
        {
            DeleteArrays();
        }
    }

    internal class HDLightEntityCollection
    {
        private static HDLightEntityCollection s_Instance = null;

        static public HDLightEntityCollection instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new HDLightEntityCollection();
                return s_Instance;
            }
        }

        private List<EntityItem> m_Entities = new List<EntityItem>();
        private Queue<int> m_FreeIndices = new Queue<int>();
        private HDLightSoAComponents m_LightComponents = new HDLightSoAComponents();

        private struct EntityItem
        {
            public int arrayIndex;
            public int version;
        }

        public bool IsValid(HDLightEntity entity)
        {
            if (entity.index >= m_Entities.Count || !entity.valid)
                return false;

            return m_Entities[entity.index].version == entity.version;
        }

        public HDLightEntity CreateEntity(Light light)
        {
            HDLightEntity entity;
            int entityIndex = 0;
            int newVersion = 0;
            int arrayIndex = m_LightComponents.Allocate(light);
            if (m_FreeIndices.Count != 0)
            {
                // Reuse
                entityIndex = m_FreeIndices.Dequeue();
                newVersion = m_Entities[entityIndex].version + 1;
            }
            else
            {
                // Create new one
                entityIndex = m_Entities.Count;
                newVersion = 1;
            }

            var entityItem = new EntityItem()
            {
                arrayIndex = arrayIndex,
                version = newVersion
            };

            entity = new HDLightEntity()
            {
                index = entityIndex,
                version = newVersion,
            };

            if (entityIndex == m_Entities.Count)
                m_Entities.Add(entityItem);
            else
                m_Entities[entityIndex] = entityItem;

            m_LightComponents.SetEntity(arrayIndex, entity);
            return entity;
        }

        public void DestroyEntity(HDLightEntity entity)
        {
            Assert.IsTrue(IsValid(entity));
            var entityData = m_Entities[entity.index];

            //erase component data by swaping with back. Get the last entity on the list.
            HDLightEntity entityToUpdate = m_LightComponents.RemoveAtSwapBack(entityData.arrayIndex);

            //rebind the indices of the entity to update, to the index passed as deletion.
            if (entityToUpdate.valid)
                UpdateIndex(entityToUpdate, entityData.arrayIndex);

            m_FreeIndices.Enqueue(entity.index);
            var item = m_Entities[entity.index];
            item.version++;
            m_Entities[entity.index] = item;
        }

        public void UpdateIndex(HDLightEntity entity, int newArrayIndex)
        {
            Assert.IsTrue(IsValid(entity));
            var item = m_Entities[entity.index];
            item.arrayIndex = newArrayIndex;
            m_Entities[entity.index] = item;
        }

        public void Cleanup()
        {
            //Do not dispose of the arrays, since lights could outlive the pipeline.
            //Cleanup here any shaders / resources specific to the render pipeline.
        }

        private struct TestJob : IJobParallelForLight
        {
            public void Execute(int index, LightAccess light)
            {
                Color c = light.color;
            }
        }

        public void Test()
        {
            if (!m_LightComponents.GetLightAccessArray().isCreated)
                return;

            var job = new TestJob();
            var jobHandle = job.ScheduleReadOnly(m_LightComponents.GetLightAccessArray());
            jobHandle.Complete();
        }
    }
}
