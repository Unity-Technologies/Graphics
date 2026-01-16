using UnityEngine;

namespace SpriteRendererTests
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class UUM127937: MonoBehaviour
    {
        private static readonly int Saturation = Shader.PropertyToID("_Saturation");
        private static readonly int AnotherProp = Shader.PropertyToID("_AnotherProp");

        public static readonly float sDefault = 0.2f;

        private void Start()
        {
            SpriteRenderer ren = this.GetComponent<SpriteRenderer>();
            MaterialPropertyBlock propertyBlock = new();
            ren.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(Saturation, sDefault);
            propertyBlock.SetFloat(AnotherProp, sDefault);
            ren.SetPropertyBlock(propertyBlock);
        }

        public float GetSaturation()
        {
            SpriteRenderer ren = this.GetComponent<SpriteRenderer>();
            MaterialPropertyBlock propertyBlock = new();
            ren.GetPropertyBlock(propertyBlock);
            var saturation = propertyBlock.GetFloat(Saturation);
            return saturation;
        }
    }
}
