using System;
using NUnit.Framework;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEngine.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    class PassThroughCommand : UndoableCommand
    {
        public static void PassThrough(PassThroughCommand command)
        {
            Assert.That(command, Is.Not.Null);
        }
    }

    class ChangeFooCommand : UndoableCommand
    {
        public int Value { get; }

        public ChangeFooCommand(int value)
        {
            Value = value;
        }

        public static void DefaultHandler(FooBarStateComponent fooBarState, ChangeFooCommand command)
        {
            Assert.That(command, Is.Not.Null);
            using (var updater = fooBarState.UpdateScope)
                updater.Foo = command.Value;
        }
    }

    class ChangeBarCommand : UndoableCommand
    {
        public int Value { get; }

        public ChangeBarCommand(int value)
        {
            Value = value;
        }

        public static void DefaultHandler(FooBarStateComponent fooBarState, ChangeBarCommand command)
        {
            Assert.That(command, Is.Not.Null);
            using (var updater = fooBarState.UpdateScope)
                updater.Bar = command.Value;
        }
    }

    class ChangeFewCommand : UndoableCommand
    {
        public int Value { get; }

        public ChangeFewCommand(int value)
        {
            Value = value;
        }

        public static void DefaultHandler(FewBawStateComponent fewBawState, ChangeFewCommand command)
        {
            Assert.That(command, Is.Not.Null);
            using (var updater = fewBawState.UpdateScope)
                updater.Few = command.Value;
        }
    }

    class UnregisteredCommand : UndoableCommand
    {}
}
