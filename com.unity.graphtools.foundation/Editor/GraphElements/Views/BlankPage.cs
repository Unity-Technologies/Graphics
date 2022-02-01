using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The view to show the available <see cref="OnboardingProviders"/>. Displayed in a new window, when no asset is selected.
    /// </summary>
    public class BlankPage : VisualElement
    {
        public static readonly string ussClassName = "ge-blank-page";

        readonly Dispatcher m_CommandDispatcher;

        public IEnumerable<OnboardingProvider> OnboardingProviders { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlankPage"/> class.
        /// </summary>
        /// <param name="commandDispatcher">The command dispatcher.</param>
        /// <param name="onboardingProviders">The list of <see cref="OnboardingProviders"/> to display.</param>
        public BlankPage(Dispatcher commandDispatcher, IEnumerable<OnboardingProvider> onboardingProviders)
        {
            m_CommandDispatcher = commandDispatcher;
            OnboardingProviders = onboardingProviders;
        }

        public virtual void CreateUI()
        {
            Clear();

            AddToClassList(ussClassName);
            foreach (var provider in OnboardingProviders)
            {
                Add(provider.CreateOnboardingElements(m_CommandDispatcher));
            }
        }

        public virtual void UpdateUI()
        {
        }
    }
}
