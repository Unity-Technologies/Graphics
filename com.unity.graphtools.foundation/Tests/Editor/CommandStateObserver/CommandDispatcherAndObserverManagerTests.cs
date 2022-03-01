using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;
using UnityEngine.TestTools;
using Dispatcher = UnityEngine.GraphToolsFoundation.CommandStateObserver.Dispatcher;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEngine.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    [Category("CommandSystem")]
    class CommandDispatcherAndObserverManagerTests
    {
        const int k_StateDefaultValue = 1;

        Dispatcher m_CommandDispatcher;
        ObserverManager m_ObserverManager;
        TestGraphToolState m_State;

        [SetUp]
        public void SetUp()
        {
            m_CommandDispatcher = new Dispatcher();
            m_ObserverManager = new ObserverManager();
            m_State = new TestGraphToolState(k_StateDefaultValue);
        }

        [TearDown]
        public void TearDown()
        {
            m_CommandDispatcher = null;
            m_ObserverManager = null;
        }

        [Test]
        public void GetStateShouldReturnInitialState()
        {
            Assert.That(m_State.FooBarStateComponent.Foo, Is.EqualTo(k_StateDefaultValue));
            Assert.That(m_State.FooBarStateComponent.Bar, Is.EqualTo(k_StateDefaultValue));
        }

        [Test]
        public void RegisteringCommandObserverDoesNotChangeState()
        {
            var observer = new TestCommandObserver();
            m_CommandDispatcher.RegisterCommandPreDispatchCallback(observer.Observe);
            Assert.That(m_State.FooBarStateComponent.Foo, Is.EqualTo(k_StateDefaultValue));
        }

        [Test]
        public void RegisteringCommandObserverTwiceThrows()
        {
            var observer = new TestCommandObserver();

            m_CommandDispatcher.RegisterCommandHandler<FooBarStateComponent, ChangeFooCommand>(ChangeFooCommand.DefaultHandler, m_State.FooBarStateComponent);
            m_CommandDispatcher.RegisterCommandPreDispatchCallback(observer.Observe);

            Assert.Throws<InvalidOperationException>(() => m_CommandDispatcher.RegisterCommandPreDispatchCallback(observer.Observe));
        }

        [Test]
        public void UnregisteringCommandObserverTwiceDoesNotThrow()
        {
            var observer = new TestCommandObserver();

            m_CommandDispatcher.RegisterCommandPreDispatchCallback(observer.Observe);

            m_CommandDispatcher.UnregisterCommandPreDispatchCallback(observer.Observe);
            m_CommandDispatcher.UnregisterCommandPreDispatchCallback(observer.Observe);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RegisteringStateObserverDoesNotChangeState()
        {
            var observer = new ObserverThatObservesFooBar(m_State.FooBarStateComponent);
            m_ObserverManager.RegisterObserver(observer);
            Assert.That(m_State.FooBarStateComponent.Foo, Is.EqualTo(k_StateDefaultValue));
        }

        [Test]
        public void RegisteringStateObserverTwiceThrows()
        {
            var observer = new ObserverThatObservesFooBar(m_State.FooBarStateComponent);

            m_ObserverManager.RegisterObserver(observer);

            Assert.Throws<InvalidOperationException>(() => m_ObserverManager.RegisterObserver(observer));
        }

        [Test]
        public void UnregisteringStateObserverTwiceDoesNotThrow()
        {
            var observer = new ObserverThatObservesFooBar(m_State.FooBarStateComponent);

            m_ObserverManager.RegisterObserver(observer);

            m_ObserverManager.UnregisterObserver(observer);
            m_ObserverManager.UnregisterObserver(observer);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void DispatchCommandWorks()
        {
            m_CommandDispatcher.RegisterCommandHandler<PassThroughCommand>(PassThroughCommand.PassThrough);
            m_CommandDispatcher.RegisterCommandHandler<FooBarStateComponent, ChangeFooCommand>(
                ChangeFooCommand.DefaultHandler, m_State.FooBarStateComponent);
            m_CommandDispatcher.RegisterCommandHandler<FooBarStateComponent, ChangeBarCommand>(
                ChangeBarCommand.DefaultHandler, m_State.FooBarStateComponent);

            m_CommandDispatcher.Dispatch(new ChangeFooCommand(10));
            Assert.That(m_State.FooBarStateComponent.Foo, Is.EqualTo(10));
            Assert.That(m_State.FooBarStateComponent.Bar, Is.EqualTo(k_StateDefaultValue));

            m_CommandDispatcher.Dispatch(new ChangeFooCommand(20));
            Assert.That(m_State.FooBarStateComponent.Foo, Is.EqualTo(20));
            Assert.That(m_State.FooBarStateComponent.Bar, Is.EqualTo(k_StateDefaultValue));

            m_CommandDispatcher.Dispatch(new ChangeBarCommand(15));
            Assert.That(m_State.FooBarStateComponent.Foo, Is.EqualTo(20));
            Assert.That(m_State.FooBarStateComponent.Bar, Is.EqualTo(15));

            m_CommandDispatcher.Dispatch(new ChangeBarCommand(30));
            Assert.That(m_State.FooBarStateComponent.Foo, Is.EqualTo(20));
            Assert.That(m_State.FooBarStateComponent.Bar, Is.EqualTo(30));

            m_CommandDispatcher.Dispatch(new PassThroughCommand());
            Assert.That(m_State.FooBarStateComponent.Foo, Is.EqualTo(20));
            Assert.That(m_State.FooBarStateComponent.Bar, Is.EqualTo(30));
        }

        [Test]
        public void DispatchedCommandShouldIncrementStateVersion()
        {
            m_CommandDispatcher.RegisterCommandHandler<FooBarStateComponent, ChangeFooCommand>(
                ChangeFooCommand.DefaultHandler, m_State.FooBarStateComponent);
            m_CommandDispatcher.RegisterCommandHandler<FooBarStateComponent, ChangeBarCommand>(
                ChangeBarCommand.DefaultHandler, m_State.FooBarStateComponent);

            Assert.IsNotNull(m_State);
            var version = m_State.FooBarStateComponent.CurrentVersion;

            m_CommandDispatcher.Dispatch(new ChangeFooCommand(10));
            Assert.That(m_State.FooBarStateComponent.CurrentVersion, Is.GreaterThan(version));

            version = m_State.FooBarStateComponent.CurrentVersion;
            m_CommandDispatcher.Dispatch(new ChangeBarCommand(20));
            Assert.That(m_State.FooBarStateComponent.CurrentVersion, Is.GreaterThan(version));
        }

        [Test]
        public void RegisteredCommandObserverShouldBeCalledForEachCommandDispatched()
        {
            var observer = new TestCommandObserver();
            m_CommandDispatcher.RegisterCommandPreDispatchCallback(observer.Observe);

            m_CommandDispatcher.RegisterCommandHandler<PassThroughCommand>(PassThroughCommand.PassThrough);
            m_CommandDispatcher.RegisterCommandHandler<FooBarStateComponent, ChangeFooCommand>(
                ChangeFooCommand.DefaultHandler, m_State.FooBarStateComponent);
            m_CommandDispatcher.RegisterCommandHandler<FooBarStateComponent, ChangeBarCommand>(
                ChangeBarCommand.DefaultHandler, m_State.FooBarStateComponent);

            Assert.That(observer.CommandObserved, Is.EqualTo(0));

            m_CommandDispatcher.Dispatch(new ChangeFooCommand(20));
            Assert.That(observer.CommandObserved, Is.EqualTo(1));

            m_CommandDispatcher.Dispatch(new ChangeBarCommand(10));
            Assert.That(observer.CommandObserved, Is.EqualTo(2));

            m_CommandDispatcher.Dispatch(new PassThroughCommand());
            Assert.That(observer.CommandObserved, Is.EqualTo(3));

            // Unregistered observer should not be notified anymore
            m_CommandDispatcher.UnregisterCommandPreDispatchCallback(observer.Observe);

            m_CommandDispatcher.Dispatch(new PassThroughCommand());
            Assert.That(observer.CommandObserved, Is.EqualTo(3));
        }

        [Test]
        public void AllRegisteredCommandObserverShouldBeCalledForEachCommandDispatched()
        {
            var observer1 = new TestCommandObserver();
            var observer2 = new TestCommandObserver();
            m_CommandDispatcher.RegisterCommandPreDispatchCallback(observer1.Observe);
            m_CommandDispatcher.RegisterCommandPreDispatchCallback(observer2.Observe);

            m_CommandDispatcher.RegisterCommandHandler<PassThroughCommand>(PassThroughCommand.PassThrough);
            m_CommandDispatcher.RegisterCommandHandler<FooBarStateComponent, ChangeFooCommand>(
                ChangeFooCommand.DefaultHandler, m_State.FooBarStateComponent);
            m_CommandDispatcher.RegisterCommandHandler<FooBarStateComponent, ChangeBarCommand>(
                ChangeBarCommand.DefaultHandler, m_State.FooBarStateComponent);

            Assert.That(observer1.CommandObserved, Is.EqualTo(0));
            Assert.That(observer2.CommandObserved, Is.EqualTo(0));

            m_CommandDispatcher.Dispatch(new ChangeFooCommand(10));
            Assert.That(observer1.CommandObserved, Is.EqualTo(1));
            Assert.That(observer2.CommandObserved, Is.EqualTo(1));

            m_CommandDispatcher.Dispatch(new PassThroughCommand());
            Assert.That(observer1.CommandObserved, Is.EqualTo(2));
            Assert.That(observer2.CommandObserved, Is.EqualTo(2));
        }

        [Test]
        public void CommandObserverShouldNotBeCalledAfterUnregistering()
        {
            var observer1 = new TestCommandObserver();
            m_CommandDispatcher.RegisterCommandPreDispatchCallback(observer1.Observe);

            m_CommandDispatcher.RegisterCommandHandler<FooBarStateComponent, ChangeFooCommand>(
                ChangeFooCommand.DefaultHandler, m_State.FooBarStateComponent);

            Assert.That(observer1.CommandObserved, Is.EqualTo(0));

            m_CommandDispatcher.Dispatch(new ChangeFooCommand(10));
            Assert.That(observer1.CommandObserved, Is.EqualTo(1));

            m_CommandDispatcher.UnregisterCommandPreDispatchCallback(observer1.Observe);
            m_CommandDispatcher.Dispatch(new ChangeFooCommand(10));
            Assert.That(observer1.CommandObserved, Is.EqualTo(1));
        }

        [Test]
        public void StateObserverIsNotifiedWhenObservedStateIsModified()
        {
            Assert.IsNotNull(m_State);

            var observer = new ObserverThatObservesFooBar(m_State.FooBarStateComponent);

            m_ObserverManager.RegisterObserver(observer);
            m_CommandDispatcher.RegisterCommandHandler<FooBarStateComponent, ChangeFooCommand>(
                ChangeFooCommand.DefaultHandler, m_State.FooBarStateComponent);

            Assert.IsTrue(observer.ObservedStateComponents.Contains(m_State.FooBarStateComponent));

            var internalObserver = (IInternalStateObserver)observer;
            var initialObserverVersion = internalObserver.GetLastObservedComponentVersion(m_State.FooBarStateComponent);

            m_CommandDispatcher.Dispatch(new ChangeFooCommand(10));
            var currentStateVersion = m_State.FooBarStateComponent.CurrentVersion;

            m_ObserverManager.NotifyObservers(m_State);

            // Observer version has changed
            Assert.AreNotEqual(initialObserverVersion,
                internalObserver.GetLastObservedComponentVersion(m_State.FooBarStateComponent));
            // and is equal to current state version.
            Assert.AreEqual(currentStateVersion,
                internalObserver.GetLastObservedComponentVersion(m_State.FooBarStateComponent).Version);
        }

        [Test]
        public void StateObserverIsNotifiedWhenObservedStateIsModifiedByOtherObserver()
        {
            Assert.IsNotNull(m_State);

            var observer1 = new ObserverThatObservesFooBar(m_State.FooBarStateComponent);
            var observer2 = new ObserverThatObservesFewBawAndModifiesFooBar(m_State.FewBawStateComponent, m_State.FooBarStateComponent);

            m_ObserverManager.RegisterObserver(observer1);
            m_ObserverManager.RegisterObserver(observer2);
            m_CommandDispatcher.RegisterCommandHandler<FewBawStateComponent, ChangeFewCommand>(
                ChangeFewCommand.DefaultHandler, m_State.FewBawStateComponent);

            var internalObserver = (IInternalStateObserver)observer1;
            var initialObserverVersion = internalObserver.GetLastObservedComponentVersion(m_State.FooBarStateComponent);

            m_CommandDispatcher.Dispatch(new ChangeFewCommand(10));
            var beforeNotification = m_State.FooBarStateComponent.CurrentVersion;

            m_ObserverManager.NotifyObservers(m_State);
            var afterNotification = m_State.FooBarStateComponent.CurrentVersion;

            // Observer version has changed since initial observation.
            Assert.AreNotEqual(initialObserverVersion, internalObserver.GetLastObservedComponentVersion(m_State.FooBarStateComponent));

            // Observer version has changed after notifying observers.
            Assert.AreNotEqual(beforeNotification, internalObserver.GetLastObservedComponentVersion(m_State.FooBarStateComponent));

            // and is equal to current state version.
            Assert.AreEqual(afterNotification, internalObserver.GetLastObservedComponentVersion(m_State.FooBarStateComponent).Version);
        }

        [Test]
        public void StateObserverIsNotNotifiedAfterUnregistering()
        {
            Assert.IsNotNull(m_State);

            var observer = new ObserverThatObservesFooBar(m_State.FooBarStateComponent);

            m_ObserverManager.RegisterObserver(observer);
            m_CommandDispatcher.RegisterCommandHandler<FooBarStateComponent, ChangeFooCommand>(
                ChangeFooCommand.DefaultHandler, m_State.FooBarStateComponent);

            Assert.IsTrue(observer.ObservedStateComponents.Contains(m_State.FooBarStateComponent));

            var internalObserver = (IInternalStateObserver)observer;
            var initialObserverVersion = internalObserver.GetLastObservedComponentVersion(m_State.FooBarStateComponent);

            m_CommandDispatcher.Dispatch(new ChangeFooCommand(10));

            m_ObserverManager.UnregisterObserver(observer);
            m_ObserverManager.NotifyObservers(m_State);

            // Observer version did not change
            Assert.AreEqual(initialObserverVersion, internalObserver.GetLastObservedComponentVersion(m_State.FooBarStateComponent));
        }

        class OrderTestStateObserver : IStateObserver
        {
            static Dictionary<string, IStateComponent> s_Components = new Dictionary<string, IStateComponent>();

            static IStateComponent GetStateComponent(string name)
            {
                if (!s_Components.TryGetValue(name, out var stateComponent))
                {
                    stateComponent = new FooBarStateComponent(0);
                    s_Components[name] = stateComponent;
                }

                return stateComponent;
            }

            readonly IStateComponent[] m_ModifiedStateComponents;
            readonly IStateComponent[] m_ObservedStateComponents;

            /// <inheritdoc />
            public IEnumerable<IStateComponent> ObservedStateComponents => m_ObservedStateComponents;

            /// <inheritdoc />
            public IEnumerable<IStateComponent> ModifiedStateComponents => m_ModifiedStateComponents;

            public OrderTestStateObserver(string[] observed, string[] updated)
            {
                m_ObservedStateComponents = observed.Select(GetStateComponent).ToArray();
                m_ModifiedStateComponents = updated.Select(GetStateComponent).ToArray();
            }

            /// <inheritdoc />
            public void Observe()
            {
                throw new NotImplementedException();
            }
        }

        static List<IStateObserver> MakeObserverSet(IEnumerable<(string[] observed, string[] updated)> desc)
        {
            return desc.Select(d => new OrderTestStateObserver(d.observed, d.updated) as IStateObserver).ToList();
        }

        static IEnumerable<object[]> GetTestStateObservers()
        {
            yield return new object[]
            {
                MakeObserverSet(
                    new[]
                    {
                        (new[] { "a" }, new string[] {}),
                    }),
                new[] { 0 },
                false
            };
            yield return new object[]
            {
                MakeObserverSet(
                    new[]
                    {
                        (new[] { "a" }, new string[] {}),
                        (new[] { "b" }, new[] { "a" }),
                    }),
                new[] { 1, 0 },
                false
            };
            yield return new object[]
            {
                MakeObserverSet(
                    new[]
                    {
                        (new[] { "b" }, new[] { "a" }),
                        (new[] { "a" }, new string[] {}),
                    }),
                new[] { 0, 1 },
                false
            };
            yield return new object[]
            {
                MakeObserverSet(
                    new[]
                    {
                        (new[] { "a" }, new string[] {}),
                        (new[] { "b" }, new[] { "a" }),
                        (new[] { "d" }, new[] { "c" }),
                        (new[] { "c" }, new[] { "b" })
                    }),
                new[] { 2, 3, 1, 0 },
                false
            };
            yield return new object[]
            {
                MakeObserverSet(
                    new[]
                    {
                        (new[] { "a", "b" }, new[] { "c", "d", "e"}),
                        (new[] { "c", "d" }, new[] { "e" }),
                        (new[] { "a", "c" }, new[] { "d" }),
                        (new[] { "d" }, new[] { "e" })
                    }),
                new[] { 0, 2, 1, 3 },
                false
            };
            yield return new object[]
            {
                MakeObserverSet(
                    new[]
                    {
                        (new[] { "a" }, new[] { "b" }),
                        (new[] { "b" }, new[] { "a" }),
                    }),
                new[] { 0, 1 },
                true
            };
            yield return new object[]
            {
                MakeObserverSet(
                    new[]
                    {
                        (new[] { "a", "b" }, new[] { "c", "d", "e"}),
                        (new[] { "c", "d" }, new[] { "e" }),
                        (new[] { "a", "c" }, new[] { "d" }),
                        (new[] { "d" }, new[] { "e" }),
                        (new[] { "e" }, new[] { "a" }),
                    }),
                new[] { 0, 1, 2, 3, 4 },
                true
            };
            yield return new object[]
            {
                MakeObserverSet(
                    new[]
                    {
                        (new[] { "a", "b" }, new[] { "c", "d", "e"}),
                        (new[] { "c", "d" }, new[] { "e" }),
                        (new[] { "a", "c" }, new[] { "d" }),
                        (new[] { "d" }, new[] { "e" }),
                        (new[] { "e" }, new[] { "c" }),
                    }),
                new[] { 0, 1, 2, 3, 4 },
                true
            };
        }

        [Test, TestCaseSource(nameof(GetTestStateObservers))]
        public void StateObserversAreSortedAccordingToObservationsAndUpdates(List<IStateObserver> observers, int[] expectedOrder, bool expectedHasCycle)
        {
            if (expectedHasCycle)
                LogAssert.Expect(LogType.Warning, "Dependency cycle detected in observers.");

            ObserverManager.SortObservers(observers.ToList(), out var sortedObservers);
            var indices = sortedObservers.Select(so => observers.IndexOf(so)).ToArray();
            Assert.AreEqual(expectedOrder, indices);
        }
    }
}
