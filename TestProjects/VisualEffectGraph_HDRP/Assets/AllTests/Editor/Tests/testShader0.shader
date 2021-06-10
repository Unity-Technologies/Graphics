Shader "Hidden/MaterialPropertiesTest"
{
    Properties
    {
        [HideInInspector]
        floatProperty1("property1", Float) = 1.0
        [HideInInspector]
        floatProperty2("property2", Float) = 2.0
        floatProperty3_Visible("property3", Float) = 3.0
        [HideInInspector, PerRendererData]
        floatProperty4_PerRenderer("property4", Float) = 4.0
        vectorProperty("property5", Vector) = (5.1,5.2,5.3,5.4)
    }
  
    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 vert() : SV_POSITION
            {
                return (float4)0;
            }

            float4 frag(float4 i : SV_POSITION) : SV_Target
            {
                return (float4)0;
            }
            ENDHLSL
        }
    }
}
