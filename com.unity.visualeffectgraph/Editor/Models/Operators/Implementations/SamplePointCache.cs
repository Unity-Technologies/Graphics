using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;
using UnityEditor.Experimental.VFX.Utility;
using UnityEngine.Experimental.Rendering;


namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Sampling", experimental = true)]
    class SamplePointCache : VFXOperator
    {
        override public string name { get { return "Sample Point Cache"; } }

        [VFXSetting, Tooltip("Specifies the Point Cache Asset to sample from.")]
        public PointCacheAsset asset;

        public class InputProperties
        {
            [Tooltip("Sets the index of the point to sample.")]
            public uint index = 0u;
        }

        [VFXSetting, SerializeField, Tooltip("Specifies how Unity handles the sample when the particleId is out of the point cache bounds.")]
        private VFXOperatorUtility.SequentialAddressingMode mode = VFXOperatorUtility.SequentialAddressingMode.Wrap;
        
        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                if (!object.ReferenceEquals(asset, null))
                {
                    if (asset == null)
                        asset = EditorUtility.InstanceIDToObject(asset.GetInstanceID()) as PointCacheAsset;
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(uint), "Point Count"));

                    if (asset.surfaces.Length != asset.types.Length)
                        throw new InvalidOperationException("Unexpected pCache format: " + AssetDatabase.GetAssetPath(asset));

                    for (int i = 0; i < asset.surfaces.Length; ++i)
                    {
                        var surface = asset.surfaces[i];
                        var type = asset.types[i];
                        yield return new VFXPropertyWithValue(new VFXProperty(VFXExpression.TypeToType(type), surface.name));
                    }
                }
            }
        }

        protected sealed override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression[] expressions = new VFXExpression[asset.surfaces.Length + 1];
            expressions[0] = VFXValue.Constant((uint)asset.PointCount);

            for (int i = 0; i < asset.surfaces.Length; i++)
            {
                var surfaceExpr = VFXValue.Constant(asset.surfaces[i]);
                VFXExpression height = new VFXExpressionTextureHeight(surfaceExpr);
                VFXExpression width = new VFXExpressionTextureWidth(surfaceExpr);
                VFXExpression u_index = VFXOperatorUtility.ApplyAddressingMode(inputExpression[0], new VFXExpressionMin(height * width, expressions[0]), mode);
                VFXExpression y = u_index / width;
                VFXExpression x = u_index - (y * width);

                Type outputType = VFXExpression.TypeToType(asset.types[i]);
                var type = typeof(VFXExpressionSampleAttributeMap<>).MakeGenericType(outputType);
                var outputExpr = Activator.CreateInstance(type, surfaceExpr, x, y);

                expressions[i + 1] = (VFXExpression)outputExpr;
            }

            return expressions;
        }
    }
}
