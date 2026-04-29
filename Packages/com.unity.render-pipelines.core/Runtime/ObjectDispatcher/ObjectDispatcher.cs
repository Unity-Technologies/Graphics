using System;
using Unity.Collections;

namespace UnityEngine.InternalBridge
{
    internal struct TypeDispatchData : IDisposable
    {
        UnityEngine.TypeDispatchData _inner;

        public Object[] changed => _inner.changed;
        public NativeArray<EntityId> changedID => _inner.changedID;
        public NativeArray<EntityId> destroyedID => _inner.destroyedID;

        public TypeDispatchData(UnityEngine.TypeDispatchData inner)
        {
            _inner = inner;
        }

        public void Dispose()
        {
            _inner.Dispose();
        }
    }

    internal class ObjectDispatcher : IDisposable
    {
        internal enum TransformTrackingType
        {
            GlobalTRS,
            LocalTRS,
            Hierarchy
        }

        [Flags]
        internal enum TypeTrackingFlags
        {
            SceneObjects = 1,
            Assets = 2,
            EditorOnlyObjects = 4,

            Default = SceneObjects | Assets,
            All = SceneObjects | Assets | EditorOnlyObjects
        }

        UnityEngine.ObjectDispatcher _inner;

        public ObjectDispatcher()
        {
            _inner = new UnityEngine.ObjectDispatcher();
        }

        public void Dispose()
        {
            _inner.Dispose();
        }

        public void EnableTypeTracking<T>(TypeTrackingFlags typeTrackingMask = TypeTrackingFlags.Default) where T : UnityEngine.Object
        {
            _inner.EnableTypeTracking<T>((UnityEngine.ObjectDispatcher.TypeTrackingFlags)typeTrackingMask);
        }

        public void EnableTransformTracking<T>(TransformTrackingType trackingType) where T : Object
        {
            _inner.EnableTransformTracking(MapPublicToInternal(trackingType), typeof(T));
        }

        public TypeDispatchData GetTypeChangesAndClear<T>(Allocator allocator, bool sortByInstanceID = false, bool noScriptingArray = false) where T : Object
        {
            var innerResult = _inner.GetTypeChangesAndClear(typeof(T), allocator, sortByInstanceID, noScriptingArray);
            return new TypeDispatchData(innerResult);
        }

        public Component[] GetTransformChangesAndClear<T>(TransformTrackingType trackingType, bool sortByInstanceID = false) where T : Object
        {
            return _inner.GetTransformChangesAndClear(typeof(T), MapPublicToInternal(trackingType), sortByInstanceID);
        }

        public int maxDispatchHistoryFramesCount
        {
            get => _inner.maxDispatchHistoryFramesCount;
            set => _inner.maxDispatchHistoryFramesCount = value;
        }

        static UnityEngine.ObjectDispatcher.TransformTrackingType MapPublicToInternal(TransformTrackingType t)
        {
            switch (t)
            {
                case TransformTrackingType.GlobalTRS:
                    return UnityEngine.ObjectDispatcher.TransformTrackingType.GlobalTRS;
                case TransformTrackingType.LocalTRS:
                    return UnityEngine.ObjectDispatcher.TransformTrackingType.LocalTRS;
                case TransformTrackingType.Hierarchy:
                    return UnityEngine.ObjectDispatcher.TransformTrackingType.Hierarchy;
                default:
                    throw new ArgumentException();
            }
        }
    }
}
