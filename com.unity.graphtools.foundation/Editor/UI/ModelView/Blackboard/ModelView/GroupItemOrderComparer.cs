using System;
using System.Collections.Generic;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class GroupItemOrderComparer : IComparer<IGroupItemModel>
    {
        public static GroupItemOrderComparer Default = new GroupItemOrderComparer();
        public int Compare(IGroupItemModel a, IGroupItemModel b)
        {
            if (a == null && b == null)
                return 0;
            if (a == null)
                return 1;
            if (b == null)
                return -1;
            var aDepth = 0;
            var current = a;
            while (current.ParentGroup != null)
            {
                ++aDepth;
                current = current.ParentGroup;
            }

            var bDepth = 0;
            current = b;
            while (current.ParentGroup != null)
            {
                ++bDepth;
                current = current.ParentGroup;
            }

            if (bDepth > aDepth)
            {
                for (var i = aDepth; i < bDepth; ++i)
                    b = b.ParentGroup;
            }
            else if (aDepth > bDepth)
            {
                for (var i = bDepth; i < aDepth; ++i)
                    a = a.ParentGroup;
            }

            // a and b are at the same depth find the base container they share

            while (a.ParentGroup != b.ParentGroup && a.ParentGroup != null)
            {
                a = a.ParentGroup;
                b = b.ParentGroup;
            }

            if (a.ParentGroup != null)
            {
                return a.ParentGroup.Items.IndexOfInternal(a) - a.ParentGroup.Items.IndexOfInternal(b);
            }

            return a.GraphModel.SectionModels.IndexOfInternal(a) - a.GraphModel.SectionModels.IndexOfInternal(b);
        }
    }
}
