Shader "Hidden/DistordTest"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

#define CENTER 0.15

            float4 frag(v2f_customrendertexture IN) : SV_Target
            {
                float4 col = float4(0,0,0,1);

                fixed2 centeredUV = IN.localTexcoord.xy * 2 - 1;

                // x distortion (r)
                if (centeredUV.x > CENTER)
                {
                    if (abs(centeredUV.y) < CENTER)
                        col.r = 1;
                    else
                        col.r = (centeredUV.x - CENTER) / (1-CENTER);
                }
                if (centeredUV.x < -CENTER)
                {
                    if (abs(centeredUV.y) < CENTER)
                        col.r = -1;
                    else
                        col.r = (centeredUV.x + CENTER) / (1 - CENTER);
                }

                // y distortion (g)
                if (centeredUV.y > CENTER)
                {
                    if (abs(centeredUV.x) < CENTER)
                        col.g = 1;
                    else
                        col.g = (centeredUV.y - CENTER) / (1 - CENTER);
                }
                if (centeredUV.y < -CENTER)
                {
                    if (abs(centeredUV.x) < CENTER)
                        col.g = -1;
                    else
                        col.g = (centeredUV.y + CENTER) / (1 - CENTER);
                }

                // distortion blur (b)
                if (abs(centeredUV.x) < CENTER)
                    if (abs(centeredUV.y) < CENTER)
                        col.b = saturate( 1 - length(centeredUV.xy) / CENTER );

                centeredUV = abs(centeredUV);
                centeredUV -= CENTER;
                centeredUV /= 1 - CENTER;
                centeredUV = centeredUV * 2 - 1;

                col.b += 1-saturate( length(centeredUV) );

                return col;
            }
            ENDCG
        }
    }
}
