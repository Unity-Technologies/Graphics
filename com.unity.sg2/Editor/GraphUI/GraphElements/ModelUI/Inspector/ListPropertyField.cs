using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class SGListViewController : ListViewController
    {
        public override void AddItems(int itemCount)
        {
            RaiseItemsAdded(new List<int>());
        }
    }

    /// <summary>
    /// ListPropertyField is a thin property field wrapper around a ListView.
    /// </summary>
    class ListPropertyField : BaseModelPropertyField
    {
        /// <summary>
        /// This field's underlying ListView. Directly access this for customization beyond what is exposed in the
        /// constructor.
        /// </summary>
        public ListView listView { get; }

        public ListPropertyField(
            ICommandTarget commandTarget,
            IList itemsSource,
            ListViewController controller = null,
            Func<VisualElement> makeItem = null,
            Action<VisualElement, int> bindItem = null
        )
            : base(commandTarget)
        {
            listView = new ListView();

            // Essential configuration

            listView.SetViewController(controller ?? new SGListViewController());
            listView.itemsSource = itemsSource;
            listView.makeItem = makeItem;
            listView.bindItem = bindItem;

            // Tool defaults

            listView.showAddRemoveFooter = true;
            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;

            Add(listView);
        }

        public override bool UpdateDisplayedValue()
        {
            listView.RefreshItems();
            return true;
        }
    }
}
