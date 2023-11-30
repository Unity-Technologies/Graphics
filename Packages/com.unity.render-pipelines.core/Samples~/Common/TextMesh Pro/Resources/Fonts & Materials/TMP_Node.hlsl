#ifndef TMPNODEINCLUDED
#define TMPNODE_INCLUDED

void TMPFunction_float(float4 texcoord, float4 clipPos, float normalWeight, float boldWeight, out float scale, out float bias)
{
    
    float bold = step(texcoord.w, 0); 
    float2 pixelSize = clipPos.w; //Clip Pos is Screen Position Raw data
    pixelSize /= abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
    scale = rsqrt(dot(pixelSize, pixelSize));
    
    float weight = lerp(normalWeight, boldWeight, bold) / 4.0; //To create Bold
	weight = weight * 0.5;


    scale *= abs(texcoord.w) * lerp(5, 12, bold);//adapt the pixel size of the letters to limit aliasing issues
	bias = (0.5 - weight) * scale - 0.5;

}

#endif //TMPNODE_INCLUDED