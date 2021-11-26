using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.VFX
{
    internal static class VFXSortingUtility
    {
        public enum SortCriteria
        {
            DistanceToCamera,
            YoungestInFront,
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
                    yield return VFXAttribute.Age;
                    break;
            }
        }

        public static bool IsPerCamera(SortCriteria criteria)
        {
            return criteria is SortCriteria.CameraDepth or SortCriteria.DistanceToCamera;
        }
        public static KeyValuePair<TResult, int> MajorityVote<TResult, TVoter>(IEnumerable<TVoter> voterContainer, Func<TVoter, TResult> getVoteFunc, IEqualityComparer<TResult> comparer = null)
        {
            Dictionary<TResult, int> voteCounts = comparer == null ? new Dictionary<TResult, int>() : new Dictionary<TResult, int>(comparer);
            foreach (var voter in voterContainer)
            {
                TResult vote = getVoteFunc(voter);
                voteCounts.TryGetValue(vote, out var currentCount);
                voteCounts[vote] = currentCount + 1;
            }
            //Return result with the most votes
            var voteResult = voteCounts.Aggregate((l, r) => l.Value > r.Value ? l : r);
            return voteResult;
        }

        private static readonly SortingCriteriaComparer s_SortingCriteriaComparer = new SortingCriteriaComparer();
        public static bool OutputNeedsOwnSort(VFXAbstractParticleOutput abstractParticleOutput,
            SortingCriterion globalSortCriterion, bool hasMainUpdate)
        {
            var outputSortingCriteria = new SortingCriterion(abstractParticleOutput);
            return (!hasMainUpdate && abstractParticleOutput.HasSorting()) ||
                   abstractParticleOutput.HasSorting() &&
                   !s_SortingCriteriaComparer.Equals(outputSortingCriteria, globalSortCriterion);
        }
        public static void SetContextSortCriteria(ref VFXGlobalSort globalSort, SortingCriterion globalSortCriterion)
        {
            globalSort.sortCriterion = globalSortCriterion.sortCriterion;
            globalSort.revertSorting = globalSortCriterion.revertSorting;
            if (globalSort.sortCriterion == SortCriteria.Custom)
            {
                globalSort.customSortingSlot = globalSortCriterion.sortKeySlot;
            }
        }

        public class SortingCriterion
        {
            public SortCriteria sortCriterion;
            public VFXSlot sortKeySlot = null;
            public bool revertSorting = false;

            public SortingCriterion(SortCriteria sortCriterion, VFXSlot sortKeySlot, bool revertSorting)
            {
                this.sortCriterion = sortCriterion;
                this.revertSorting = revertSorting;
                if (sortCriterion == SortCriteria.Custom)
                {
                    this.sortKeySlot = sortKeySlot;
                }
            }

            public SortingCriterion(VFXAbstractParticleOutput output)
            {
                sortCriterion = output.GetSortCriterion();
                revertSorting = output.revertSorting;
                if (sortCriterion == SortCriteria.Custom)
                {
                    sortKeySlot = output.inputSlots.FirstOrDefault(o => o.name == "sortKey");
                }
            }

            public SortingCriterion()
            {
                sortCriterion = SortCriteria.DistanceToCamera;
                sortKeySlot = null;
            }
        }

        public static SortingCriterion GetVoteFunc(VFXAbstractParticleOutput output)
        {
            return new SortingCriterion(output.GetSortCriterion(), output.inputSlots.FirstOrDefault(s => s.name == "sortKey"), output.revertSorting);
        }

        public class SortingCriteriaComparer : EqualityComparer<SortingCriterion>
        {
            public override bool Equals(SortingCriterion x, SortingCriterion y)
            {
                if (x == null || y == null)
                    return false;
                if (x.sortCriterion != y.sortCriterion)
                    return false;
                if (x.revertSorting != y.revertSorting)
                    return false;
                if (x.sortCriterion == SortCriteria.Custom)
                {
                    return x.sortKeySlot.GetExpression().Equals(y.sortKeySlot.GetExpression());
                }
                return true;
            }


            public override int GetHashCode(SortingCriterion obj)
            {
                int hash = obj.sortCriterion.GetHashCode();
                hash ^= obj.revertSorting.GetHashCode();
                if (obj.sortCriterion == SortCriteria.Custom)
                    hash ^= obj.sortKeySlot.GetExpression().GetHashCode();
                return hash;
            }
        }
    }


}
