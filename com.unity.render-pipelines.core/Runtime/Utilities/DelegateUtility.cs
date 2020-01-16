using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Delegate utility class.
    /// </summary>
    public static class DelegateUtility
    {
        /// <summary>
        /// Cast a delegate.
        /// </summary>
        /// <param name="source">Source delegate.</param>
        /// <param name="type">Type of the delegate.</param>
        /// <returns>Cast delegate.</returns>
        public static Delegate Cast(Delegate source, Type type)
        {
            if (source == null)
                return null;
            Delegate[] delegates = source.GetInvocationList();
            if (delegates.Length == 1)
                return Delegate.CreateDelegate(type,
                    delegates[0].Target, delegates[0].Method);
            Delegate[] delegatesDest = new Delegate[delegates.Length];
            for (int nDelegate = 0; nDelegate < delegates.Length; nDelegate++)
                delegatesDest[nDelegate] = Delegate.CreateDelegate(type,
                        delegates[nDelegate].Target, delegates[nDelegate].Method);
            return Delegate.Combine(delegatesDest);
        }
    }
}
