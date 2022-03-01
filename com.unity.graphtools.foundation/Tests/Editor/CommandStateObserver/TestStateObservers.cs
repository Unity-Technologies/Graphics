using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;

namespace UnityEngine.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    class ObserverThatObservesFooBar : StateObserver
    {
        FooBarStateComponent m_FooBarStateComponent;

        /// <inheritdoc />
        public ObserverThatObservesFooBar(FooBarStateComponent fooBarStateComponent)
            : base(fooBarStateComponent)
        {
            m_FooBarStateComponent = fooBarStateComponent;
        }

        /// <inheritdoc />
        public override void Observe()
        {
            using (this.ObserveState(m_FooBarStateComponent))
            {
            }
        }
    }

    class ObserverThatObservesFewBawAndModifiesFooBar : StateObserver
    {
        FewBawStateComponent m_FewBawStateComponent;
        FooBarStateComponent m_FooBarStateComponent;

        /// <inheritdoc />
        public ObserverThatObservesFewBawAndModifiesFooBar(FewBawStateComponent fewBawStateComponent,
            FooBarStateComponent fooBarStateComponent)
            : base(new[] { fewBawStateComponent },
                new[] { fooBarStateComponent })
        {
            m_FewBawStateComponent = fewBawStateComponent;
            m_FooBarStateComponent = fooBarStateComponent;
        }

        /// <inheritdoc />
        public override void Observe()
        {
            using (this.ObserveState(m_FewBawStateComponent))
            {
                using (var updater = m_FooBarStateComponent.UpdateScope)
                    updater.Foo = 42;
            }
        }
    }
}
