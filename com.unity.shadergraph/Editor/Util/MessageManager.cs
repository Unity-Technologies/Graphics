using System.Collections.Generic;
using System.Text;
using UnityEditor.ShaderGraph;
using UnityEngine;

namespace UnityEditor.Graphing.Util
{
    class MessageManager
    {
        Dictionary<object, Dictionary<Identifier, List<ShaderMessage>>> m_Messages =
            new Dictionary<object, Dictionary<Identifier, List<ShaderMessage>>>();

        Dictionary<Identifier, List<ShaderMessage>> m_Combined = new Dictionary<Identifier, List<ShaderMessage>>();

        public bool nodeMessagesChanged { get; private set; }

        public void AddOrAppendError(object errorProvider, Identifier nodeId, ShaderMessage error)
        {
            Dictionary<Identifier, List<ShaderMessage>> messages;
            if (!m_Messages.TryGetValue(errorProvider, out messages))
            {
                messages = new Dictionary<Identifier, List<ShaderMessage>>();
                m_Messages[errorProvider] = messages;
            }

            List<ShaderMessage> messageList;
            if (messages.TryGetValue(nodeId, out messageList))
            {
                messageList.Add(error);
            }
            else
            {
                messages[nodeId] = new List<ShaderMessage>() {error};
            }

            nodeMessagesChanged = true;
        }

        public IEnumerable<KeyValuePair<Identifier, List<ShaderMessage>>> GetNodeMessages()
        {
            var fixedNodes = new List<Identifier>();
            m_Combined.Clear();
            foreach (var messageMap in m_Messages)
            {
                foreach (var messageList in messageMap.Value)
                {
                    List<ShaderMessage> foundList;
                    if (m_Combined.TryGetValue(messageList.Key, out foundList))
                    {
                        foundList.AddRange(messageList.Value);
                    }
                    else
                    {
                        m_Combined[messageList.Key] = messageList.Value;
                    }

                    if (messageList.Value.Count == 0)
                    {
                        fixedNodes.Add(messageList.Key);
                    }
                }

                // If all the messages from a provider for a node are gone,
                // we can now remove it from the list since that will be reported in m_Combined
                fixedNodes.ForEach(nodeId => messageMap.Value.Remove(nodeId));
            }

            nodeMessagesChanged = false;
            return m_Combined;
        }

        public void RemoveNode(Identifier nodeId)
        {
            foreach (var messageMap in m_Messages)
            {
                nodeMessagesChanged |= messageMap.Value.Remove(nodeId);
            }
        }

        public void ClearAllFromProvider(object messageProvider)
        {
            Dictionary<Identifier, List<ShaderMessage>> messageMap;
            if (m_Messages.TryGetValue(messageProvider, out messageMap))
            {
                foreach (var messageList in messageMap)
                {
                    nodeMessagesChanged |= messageList.Value.Count > 0;
                    messageList.Value.Clear();
                }
            }
        }

        public void ClearNodesFromProvider(object messageProvider, IEnumerable<AbstractMaterialNode> nodes)
        {
            Dictionary<Identifier,List<ShaderMessage>> messageMap;
            if (m_Messages.TryGetValue(messageProvider, out messageMap))
            {
                foreach (var node in nodes)
                {
                    List<ShaderMessage> messages;
                    if (messageMap.TryGetValue(node.tempId, out messages))
                    {
                        nodeMessagesChanged |= messages.Count > 0;
                        messages.Clear();
                    }
                }
            }
        }

        public void ClearAll()
        {
            m_Messages.Clear();
            m_Combined.Clear();
            nodeMessagesChanged = false;
        }

        void DebugPrint()
        {
            StringBuilder output = new StringBuilder("MessageMap:\n");
            foreach (var messageMap in m_Messages)
            {
                output.AppendFormat("\tFrom Provider {0}:\n", messageMap.Key.GetType());
                foreach (var messageList in messageMap.Value)
                {
                    output.AppendFormat("\t\tNode {0} has {1} messages:\n", messageList.Key.index, messageList.Value.Count);
                    foreach (var message in messageList.Value)
                    {
                        output.AppendFormat("\t\t\t{0}\n", message.message);
                    }
                }
            }
            Debug.Log(output.ToString());
        }
    }
}
