using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.VFX.Utility;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Utility")]
    class VFXOperatorPointCache : VFXOperator
    {
        override public string name { get { return "Point Cache"; } }

        [VFXSetting]
        public PointCacheAsset Asset;

        public class OutputProperties
        {
            public uint Count = 0;
        }
        void RefreshPointCacheObject()
        {
            if (Asset == null && !object.ReferenceEquals(Asset, null))
            {
                string assetPath = AssetDatabase.GetAssetPath(Asset.GetInstanceID());
                
                var newPointCache = AssetDatabase.LoadAssetAtPath<PointCacheAsset>(assetPath);
                if( newPointCache != null )
                {
                    Asset = newPointCache;
                }
            }
        }

        public override void GetImportDependentAssets(HashSet<string> dependencies)
        {
            base.GetImportDependentAssets(dependencies);

            dependencies.Add(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(Asset.GetInstanceID())));
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                RefreshPointCacheObject();
                if (Asset != null)
                {
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(uint), "Point Count"));
                    foreach (var surface in Asset.surfaces)
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(Texture2D), "AttributeMap : " + surface.name));
                }
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            RefreshPointCacheObject();
            VFXExpression[] expressions = new VFXExpression[Asset.surfaces.Length + 1];
            expressions[0] = VFXValue.Constant((uint)Asset.PointCount);

            for (int i = 0; i < Asset.surfaces.Length; i++)
                expressions[i + 1] = VFXValue.Constant(Asset.surfaces[i]);

            return expressions;
        }
    }
}
