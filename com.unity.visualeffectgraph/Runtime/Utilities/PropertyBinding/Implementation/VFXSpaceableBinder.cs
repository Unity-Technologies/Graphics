using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    abstract class VFXSpaceableBinder : VFXBinderBase
    {
        public enum BinderSpace
        {
            Automatic,
            World,
            Local
        }

        [SerializeField]
        public BinderSpace Space;

        private VFXSpace GetTargetSpace(VisualEffect component, ExposedProperty targetProperty)
        {
            var targetSpace = VFXSpace.None;
            switch (Space)
            {
                case BinderSpace.Automatic: targetSpace = component.visualEffectAsset.GetExposedSpace(targetProperty); break;
                case BinderSpace.World: targetSpace = VFXSpace.World; break;
                case BinderSpace.Local: targetSpace = VFXSpace.Local; break;
            }
            return targetSpace;
        }

        protected void ApplySpacePositionNormal(VisualEffect component, ExposedProperty targetProperty, Transform sourceTransform, out Vector3 position, out Vector3 normal)
        {
            var targetSpace = GetTargetSpace(component, targetProperty);
            if (targetSpace == VFXSpace.Local)
            {
                var transformInComponentSpace = component.transform.worldToLocalMatrix * sourceTransform.localToWorldMatrix;
                position = transformInComponentSpace.GetPosition();
                normal = transformInComponentSpace.MultiplyVector(Vector3.up);
            }
            else
            {
                position = sourceTransform.position;
                normal = sourceTransform.up;
            }
        }

        protected void ApplySpaceTS(VisualEffect component, ExposedProperty targetProperty, Transform sourceTransform, out Vector3 position, out Vector3 scale)
        {
            var targetSpace = GetTargetSpace(component, targetProperty);
            if (targetSpace == VFXSpace.Local)
            {
                var transformInComponentSpace = component.transform.worldToLocalMatrix * sourceTransform.localToWorldMatrix;
                position = transformInComponentSpace.GetPosition();
                scale = transformInComponentSpace.lossyScale;
            }
            else
            {
                position = sourceTransform.position;
                scale = sourceTransform.lossyScale;
            }
        }

        protected void ApplySpaceTRS(VisualEffect component, ExposedProperty targetProperty, Transform sourceTransform, out Vector3 position, out Vector3 eulerAngles, out Vector3 scale)
        {
            var targetSpace = GetTargetSpace(component, targetProperty);
            if (targetSpace == VFXSpace.Local)
            {
                var transformInComponentSpace = component.transform.worldToLocalMatrix * sourceTransform.localToWorldMatrix;
                position = transformInComponentSpace.GetPosition();
                eulerAngles = transformInComponentSpace.rotation.eulerAngles;
                scale = transformInComponentSpace.lossyScale;
            }
            else
            {
                position = sourceTransform.position;
                eulerAngles = sourceTransform.eulerAngles;
                scale = sourceTransform.lossyScale;
            }
        }

        protected Vector3 ApplySpacePosition(VisualEffect component, ExposedProperty targetProperty, Vector3 sourceWorldPosition)
        {
            var targetSpace = GetTargetSpace(component, targetProperty);
            if (targetSpace == VFXSpace.Local)
            {
                var sourceLocalPosition = component.transform.worldToLocalMatrix.MultiplyPoint(sourceWorldPosition);
                return sourceLocalPosition;
            }
            return sourceWorldPosition;
        }
    }
}
