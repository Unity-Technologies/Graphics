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

    internal class HDLightSoAComponents : IDisposable
    {
        public const int ArrayCapacity = 100;
        private int m_Capacity = ArrayCapacity;
        private HDLightEntity[]  m_LightEntities    = new HDLightEntity[ArrayCapacity];
        private LightAccessArray m_LightAccessArray = new LightAccessArray(ArrayCapacity);

        public void Dispose()
        {
            m_LightAccessArray.Dispose();
        }

        public int Allocate(Light light)
        {
            int nextIndex = m_LightAccessArray.length;

            if (m_LightAccessArray.length == m_Capacity)
            {
                m_Capacity *= 2;
                m_LightAccessArray.capacity = m_Capacity;
                Array.Resize(ref m_LightEntities, m_Capacity);
            }

            m_LightEntities[nextIndex] = HDLightEntity.Invalid;
            m_LightAccessArray.Add(light);
            return nextIndex;
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
            return m_LightAccessArray.length == 0 ? HDLightEntity.Invalid : entity;
        }
        public ref LightAccessArray GetLightAccessArray()
        {
            return ref m_LightAccessArray;
        }
    }

    internal class HDLightEntityCollection : IDisposable
    {
        private static HDLightEntityCollection s_Instance = new HDLightEntityCollection();

        static public HDLightEntityCollection instance
        {
            get
            {
                return s_Instance;
            }
        }

        public static void Cleanup()
        {
            s_Instance.Dispose();
        }

        private bool m_Disposed = false;
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
            if (m_Disposed)
                return;

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

        public void Dispose()
        {
            if (m_Disposed)
                return;

            m_Disposed = true;
            m_LightComponents.Dispose();
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
            var job = new TestJob();
            var jobHandle = job.ScheduleReadOnly(m_LightComponents.GetLightAccessArray(), 32);
            //var jobHandle = job.Schedule(m_LightComponents.GetLightAccessArray());
            jobHandle.Complete();
        }
    }

}
