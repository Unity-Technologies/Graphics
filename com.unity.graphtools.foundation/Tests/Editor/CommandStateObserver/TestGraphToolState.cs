using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEngine.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    class FooBarStateComponent : ViewStateComponent<FooBarStateComponent.StateUpdater>
    {
        internal class StateUpdater : BaseUpdater<FooBarStateComponent>
        {
            public int Foo { get => m_State.Foo; set => m_State.Foo = value; }
            public int Bar { get => m_State.Bar; set => m_State.Bar = value; }
        }

        public int Foo { get; private set; }
        public int Bar { get; private set; }

        public FooBarStateComponent(int init)
        {
            Foo = init;
            Bar = init;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
        }

        /// <inheritdoc />
        protected override void Move(IStateComponent other)
        {
            base.Move(other);

            if (other is FooBarStateComponent fooBarStateComponent)
            {
                Foo = fooBarStateComponent.Foo;
                Bar = fooBarStateComponent.Bar;
            }
        }
    }

    class FewBawStateComponent : ViewStateComponent<FewBawStateComponent.StateUpdater>
    {
        internal class StateUpdater : BaseUpdater<FewBawStateComponent>
        {
            public int Few { get => m_State.Few; set => m_State.Few = value; }
            public int Baw { get => m_State.Few; set => m_State.Few = value; }
        }

        public int Few { get; private set; }
        public int Baw { get; private set; }

        public FewBawStateComponent(int init)
        {
            Few = init;
            Baw = init;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
        }

        /// <inheritdoc />
        protected override void Move(IStateComponent other)
        {
            base.Move(other);

            if (other is FewBawStateComponent fewBawStateComponent)
            {
                Few = fewBawStateComponent.Few;
                Baw = fewBawStateComponent.Baw;
            }
        }
    }

    class TestGraphToolState : State
    {
        public FooBarStateComponent FooBarStateComponent { get; }
        public FewBawStateComponent FewBawStateComponent { get; }

        public TestGraphToolState(int init)
        {
            FooBarStateComponent = new FooBarStateComponent(init);
            FewBawStateComponent = new FewBawStateComponent(init);
        }

        ~TestGraphToolState() => Dispose(false);

        /// <inheritdoc />
        public override IEnumerable<IStateComponent> AllStateComponents
        {
            get
            {
                foreach (var s in base.AllStateComponents)
                {
                    yield return s;
                }

                yield return FooBarStateComponent;
                yield return FewBawStateComponent;
            }
        }
    }
}
