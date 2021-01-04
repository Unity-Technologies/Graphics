using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Rendering;
using UnityEditor.ShaderGraph;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Graphing.Util
{
    class MessageManager
    {
        protected Dictionary<object, Dictionary<string, List<ShaderMessage>>> m_Messages =
            new Dictionary<object, Dictionary<string, List<ShaderMessage>>>();

        Dictionary<string, List<ShaderMessage>> m_Combined = new Dictionary<string, List<ShaderMessage>>();

        public bool nodeMessagesChanged { get; private set; }

        Dictionary<string, List<ShaderMessage>> m_FoundMessages;

        public void AddOrAppendError(object errorProvider, string nodeId, ShaderMessage error)
        {
            if (!m_Messages.TryGetValue(errorProvider, out var messages))
            {
                messages = new Dictionary<string, List<ShaderMessage>>();
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

        // Sort messages so errors come before warnings in the list
        static int CompareMessages(ShaderMessage m1, ShaderMessage m2)
        {
            return m1.severity > m2.severity ? 1 : m2.severity > m1.severity ? -1 : 0;
        }

        public IEnumerable<KeyValuePair<string, List<ShaderMessage>>> GetNodeMessages()
        {
            var fixedNodes = new List<string>();
            m_Combined.Clear();
            foreach (var messageMap in m_Messages)
            {
                foreach (var messageList in messageMap.Value)
                {
                    if (!m_Combined.TryGetValue(messageList.Key, out var foundList))
                    {
                        foundList = new List<ShaderMessage>();
                        m_Combined.Add(messageList.Key, foundList);
                    }
                    foundList.AddRange(messageList.Value);

                    if (messageList.Value.Count == 0)
                    {
                        fixedNodes.Add(messageList.Key);
                    }
                }

                // If all the messages from a provider for a node are gone,
                // we can now remove it from the list since that will be reported in m_Combined
                fixedNodes.ForEach(nodeId => messageMap.Value.Remove(nodeId));
            }

            foreach (var nodeList in m_Combined)
            {
                nodeList.Value.Sort(CompareMessages);
            }

            nodeMessagesChanged = false;
            return m_Combined;
        }

        public void RemoveNode(string nodeId)
        {
            foreach (var messageMap in m_Messages)
            {
                nodeMessagesChanged |= messageMap.Value.Remove(nodeId);
            }
        }

        public void ClearAllFromProvider(object messageProvider)
        {
            if (m_Messages.TryGetValue(messageProvider, out m_FoundMessages))
            {
                foreach (var messageList in m_FoundMessages)
                {
                    nodeMessagesChanged |= messageList.Value.Count > 0;
                    messageList.Value.Clear();
                }

                m_FoundMessages = null;
            }
        }

        public void ClearNodesFromProvider(object messageProvider, IEnumerable<AbstractMaterialNode> nodes)
        {
            if (m_Messages.TryGetValue(messageProvider, out m_FoundMessages))
            {
                foreach (var node in nodes)
                {
                    if (m_FoundMessages.TryGetValue(node.objectId, out var messages))
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
                    output.AppendFormat("\t\tNode {0} has {1} messages:\n", messageList.Key, messageList.Value.Count);
                    foreach (var message in messageList.Value)
                    {
                        output.AppendFormat("\t\t\t{0}\n", message.message);
                    }
                }
            }
            Debug.Log(output.ToString());
        }

        public static void Log(AbstractMaterialNode node, string path, ShaderMessage message, Object context)
        {
            var errString = $"{message.severity} in Graph at {path} at node {node.name}: {message.message}";
            if (message.severity == ShaderCompilerMessageSeverity.Error)
            {
                Debug.LogError(errString, context);
            }
            else
            {
                Debug.LogWarning(errString, context);
            }
        }

        public bool AnyError()
        {
            foreach (var messages in m_Messages.Values)
            {
                foreach (List<ShaderMessage> messageList in messages.Values)
                {
                    foreach (var message in messageList)
                    {
                        if (message.severity == ShaderCompilerMessageSeverity.Error)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
