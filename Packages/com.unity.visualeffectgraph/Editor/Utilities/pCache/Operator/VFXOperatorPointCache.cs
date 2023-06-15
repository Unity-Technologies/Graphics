using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor.Experimental.VFX.Utility;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Utility")]
    class VFXOperatorPointCache : VFXOperator
    {
        private bool m_IsPointCacheAssetMissing;

        public override string name { get { return "Point Cache"; } }

        [VFXSetting]
        public PointCacheAsset Asset;

        public class OutputProperties
        {
            public uint Count = 0;
        }

        public override void GetImportDependentAssets(HashSet<int> dependencies)
        {
            base.GetImportDependentAssets(dependencies);
            if (!object.ReferenceEquals(Asset, null))
            {
                dependencies.Add(Asset.GetInstanceID());
            }
        }

        internal override void GenerateErrors(VFXInvalidateErrorReporter manager)
        {
            base.GenerateErrors(manager);

            var asset = GetOrRefreshPointCacheAsset(false);
            if (m_IsPointCacheAssetMissing)
            {
                var missingPointCachePath = AssetDatabase.GetAssetPath(asset.GetInstanceID());
                var message = $"The VFX Graph cannot be compiled because a PointCacheAsset located here '{missingPointCachePath}' is missing.";
                manager.RegisterError("ErrorMissingPointCache", VFXErrorType.Error, message);
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                var asset = GetOrRefreshPointCacheAsset(true);
                if (asset != null)
                {
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(uint), "Point Count"));
                    foreach (var surface in asset.surfaces)
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(Texture2D), "AttributeMap : " + surface.name));
                }
            }
        }

        // Do not resync slots when point cache asset is missing to keep potential links to output slots
        public override bool ResyncSlots(bool notify) => !m_IsPointCacheAssetMissing && base.ResyncSlots(notify);

        protected sealed override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression[] expressions = null;
            var asset = GetOrRefreshPointCacheAsset();
            if (asset != null)
            {
                expressions = new VFXExpression[asset.surfaces.Length + 1];
                expressions[0] = VFXValue.Constant((uint)asset.PointCount);

                for (int i = 0; i < asset.surfaces.Length; i++)
                    expressions[i + 1] = VFXValue.Constant(asset.surfaces[i]);

                return expressions;
            }
            if (m_IsPointCacheAssetMissing)
            {
                expressions = new VFXExpression[outputSlots.Count];
                expressions[0] = VFXValue.Constant((uint)0);
                for (int i = 1; i < outputSlots.Count; i++)
                    expressions[i] = VFXValue.Constant<Texture2D>();

                return expressions;
            }

            return Array.Empty<VFXExpression>();
        }

        private PointCacheAsset GetOrRefreshPointCacheAsset(bool refreshErrors = true)
        {
            var wasPointCacheAssetMissing = m_IsPointCacheAssetMissing;
            //This is the only place where point cache property is updated or read
            if (Asset == null && !object.ReferenceEquals(Asset, null))
            {
                var assetPath = AssetDatabase.GetAssetPath(Asset.GetInstanceID());

                var newPointCacheAsset = AssetDatabase.LoadAssetAtPath<PointCacheAsset>(assetPath);
                m_IsPointCacheAssetMissing = newPointCacheAsset == null;

                if (!m_IsPointCacheAssetMissing)
                {
                    Asset = newPointCacheAsset;
                }
            }
            else
            {
                m_IsPointCacheAssetMissing = false;
            }

            if (refreshErrors && wasPointCacheAssetMissing != m_IsPointCacheAssetMissing)
            {
                RefreshErrors();
            }

            return Asset;
        }
    }
}
