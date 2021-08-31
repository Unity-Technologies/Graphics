using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Search;
using static UnityEditor.VFX.VFXSortingUtility;

namespace UnityEditor.VFX
{
    internal static class VFXSortingUtility
    {
        public enum SortCriteria
        {
            DistanceToCamera,
            YoungestInFront,
            OldestInFront,
            CameraDepth,
            Custom,
        }

        public static IEnumerable<string> GetSortingAdditionalDefines(SortCriteria sortCriteria)
        {
            switch (sortCriteria)
            {
                case SortCriteria.Custom:
                    yield return "VFX_CUSTOM_SORT_KEY";
                    break;
                case SortCriteria.CameraDepth:
                    yield return "VFX_DEPTH_SORT_KEY";
                    break;
                case SortCriteria.DistanceToCamera:
                    yield return "VFX_DISTANCE_SORT_KEY";
                    break;
                case SortCriteria.YoungestInFront:
                    yield return "VFX_YOUNGEST_SORT_KEY";
                    break;
                case SortCriteria.OldestInFront:
                    yield return "VFX_OLDEST_SORT_KEY";
                    break;
                default:
                    throw new NotImplementedException("This Sorting criteria is missing an Additional Define");
            }
        }

        public static IEnumerable<VFXAttribute> GetSortingDependantAttributes(SortCriteria sortCriteria)
        {
            switch (sortCriteria)
            {
                case SortCriteria.Custom:
                    break;
                case SortCriteria.CameraDepth:
                case SortCriteria.DistanceToCamera:
                    yield return VFXAttribute.Position;
                    break;
                case SortCriteria.YoungestInFront:
                case SortCriteria.OldestInFront:
                    yield return VFXAttribute.Age;
                    break;
            }
        }

        public static bool IsPerCamera(SortCriteria criteria)
        {
            return criteria is SortCriteria.CameraDepth or SortCriteria.DistanceToCamera;
        }
        public class SortKeySlotComparer : EqualityComparer<VFXSlot>
        {
            public override bool Equals(VFXSlot x, VFXSlot y)
            {
                return x.GetExpression().Equals(y.GetExpression());
            }

            public override int GetHashCode(VFXSlot obj)
            {
                return obj.GetExpression().GetHashCode();
            }
        }

        public static TResult MajorityVote<TResult, TVoter>(IEnumerable<TVoter> voterContainer, Func<TVoter, TResult> getVoteFunc, IEqualityComparer<TResult> comparer = null)
        {
            Dictionary<TResult, int> voteCounts = comparer == null ? new Dictionary<TResult, int>() : new Dictionary<TResult, int>(comparer) ;
            foreach (var voter in voterContainer)
            {
                TResult vote = getVoteFunc(voter);
                voteCounts.TryGetValue(vote, out var currentCount);
                voteCounts[vote] = currentCount + 1;
            }
            //Return result with the most votes
            return voteCounts.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
        }

        public static bool OutputNeedsOwnSort(VFXAbstractParticleOutput abstractParticleOutput, bool needsGlobalSort,
            SortCriteria globalSortCriterion, VFXSlot globalSortKeySlot, SortKeySlotComparer comparer)
        {
            return abstractParticleOutput.HasSorting() && needsGlobalSort &&
                   (abstractParticleOutput.GetSortCriterion() != globalSortCriterion
                    || abstractParticleOutput.GetSortCriterion() == SortCriteria.Custom
                    && !comparer.Equals(abstractParticleOutput.inputSlots.First(o => o.name == "sortKey"), globalSortKeySlot));
        }


        public class BaseSortingCriterion
        {
            public SortCriteria sortCriterion;
        }

        public class BuiltInSortingCriterion : BaseSortingCriterion
        {
            public BuiltInSortingCriterion(SortCriteria sortCriterion)
            {
                if (sortCriterion == SortCriteria.Custom)
                    throw new ArgumentException("Built-in sorting criterion excludes Custom");
                this.sortCriterion = sortCriterion;
            }
        }

        public class CustomSortingCriterion : BaseSortingCriterion
        {
            public readonly VFXSlot sortKeySlot;

            public CustomSortingCriterion(VFXSlot sortKeySlot)
            {
                this.sortCriterion = SortCriteria.Custom;
                this.sortKeySlot = sortKeySlot;
            }
        }
        public static BaseSortingCriterion GetVoteFunc(VFXAbstractParticleOutput output)
        {
            SortCriteria sortCriterion = output.GetSortCriterion();
            return sortCriterion != SortCriteria.Custom
                ? new BuiltInSortingCriterion(sortCriterion)
                : new CustomSortingCriterion(output.inputSlots.First(s => s.name == "sortKey"));
        }

        public class SortingCriteriaComparer : EqualityComparer<BaseSortingCriterion>
        {
            public override bool Equals(BaseSortingCriterion x, BaseSortingCriterion y)
            {
                if (x?.GetType() != y?.GetType())
                    return false;
                switch (x)
                {
                    case BuiltInSortingCriterion sortingCriteriaX:
                        return sortingCriteriaX.sortCriterion.Equals(((BuiltInSortingCriterion) y).sortCriterion);
                    case CustomSortingCriterion slotCriteriaX:
                        return slotCriteriaX.sortKeySlot.GetExpression()
                            .Equals(((CustomSortingCriterion) y).sortKeySlot.GetExpression());
                    default:
                        return false;
                }
            }


            public override int GetHashCode(BaseSortingCriterion obj)
            {
                switch (obj)
                {
                    case BuiltInSortingCriterion sortingCriteriaObj:
                        return sortingCriteriaObj.sortCriterion.GetHashCode();
                    case CustomSortingCriterion slotCriteriaObj:
                        return slotCriteriaObj.sortKeySlot.GetExpression().GetHashCode();
                    default:
                        return 0;
                }
            }
        }
    }


}
