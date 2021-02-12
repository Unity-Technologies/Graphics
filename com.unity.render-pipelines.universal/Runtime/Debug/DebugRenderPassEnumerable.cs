
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal
{
    public class DebugRenderPassEnumerable : IEnumerable<DebugRenderPass>
    {
        private class Enumerator : IEnumerator<DebugRenderPass>
        {
            private readonly DebugHandler m_DebugHandler;
            private readonly ScriptableRenderContext m_Context;
            private readonly CommandBuffer m_CommandBuffer;
            private readonly int m_NumPasses;

            private int m_Index;

            public DebugRenderPass Current { get; private set; }
            object IEnumerator.Current => Current;

            public Enumerator(DebugHandler debugHandler, ScriptableRenderContext context, CommandBuffer commandBuffer)
            {
                m_DebugHandler = debugHandler;
                m_Context = context;
                m_CommandBuffer = commandBuffer;
                m_NumPasses = DebugRenderPass.GetNumPasses(debugHandler);

                m_Index = -1;
            }

            #region IEnumerator<DebugRenderPass>
            public bool MoveNext()
            {
                Current?.Dispose();

                if(++m_Index >= m_NumPasses)
                {
                    return false;
                }
                else
                {
                    Current = new DebugRenderPass(m_DebugHandler, m_Context, m_CommandBuffer, m_Index);
                    return true;
                }
            }

            public void Reset()
            {
                if(Current != null)
                {
                    Current.Dispose();
                    Current = null;
                }
                m_Index = -1;
            }

            public void Dispose()
            {
                Current?.Dispose();
            }
            #endregion
        }

        private readonly DebugHandler m_DebugHandler;
        private readonly ScriptableRenderContext m_Context;
        private readonly CommandBuffer m_CommandBuffer;

        public DebugRenderPassEnumerable(DebugHandler debugHandler, ScriptableRenderContext context, CommandBuffer commandBuffer)
        {
            m_DebugHandler = debugHandler;
            m_Context = context;
            m_CommandBuffer = commandBuffer;
        }

        #region IEnumerable<DebugRenderPass>
        public IEnumerator<DebugRenderPass> GetEnumerator()
        {
            return new Enumerator(m_DebugHandler, m_Context, m_CommandBuffer);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion
    }
}
