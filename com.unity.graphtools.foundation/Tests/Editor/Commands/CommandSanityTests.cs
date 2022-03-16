using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Commands
{
    public class CommandSanityTests
    {
        static string[] s_IgnoredCommandTypes =
        {
            // Not undoable
            nameof(LoadGraphAssetCommand)
        };

        static IEnumerable<Type> AllCommands()
        {
            return TypeCache.GetTypesDerivedFrom<UndoableCommand>()
                .Where(a => !a.IsAbstract && !s_IgnoredCommandTypes.Contains(a.Name) && !a.Namespace.Contains(".Tests"))
                .OrderBy(t => t.Namespace).ThenBy(t => t.Name);
        }

        [TestCaseSource(nameof(AllCommands))]
        public void CommandHaveAnUndoString(Type t)
        {
            foreach (var constructor in t.GetConstructors())
            {
                var command = constructor.Invoke(constructor.GetParameters().Select(
                    parameterInfo => parameterInfo.ParameterType.IsValueType ?
                    Activator.CreateInstance(parameterInfo.ParameterType) : null).ToArray()) as UndoableCommand;

                Assert.IsNotNull(command);
                Assert.IsNotNull(command.UndoString);
                Assert.AreNotEqual("", command.UndoString);
            }
        }
    }
}
