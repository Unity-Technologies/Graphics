//UNITY_SHADER_NO_UPGRADE
#ifndef MYHLSLINCLUDE_INCLUDED
#define MYHLSLINCLUDE_INCLUDED

void ProceduralColorChecker_float(UnityTexture2D CheckerTexture,UnitySamplerState SS, float3 localPos, float3 localNormal,  float NumberOfFields, float FieldsPerRow, float GridThickness, float SquareSize, bool AddGradient, float4 GradientColorA, float4 GradientColorB, float GradientPower, bool SphereMode, bool MaterialMode, bool ReflectionMode, bool CompareToUnlit,  out float4 BaseColor, out float4 Emission, out float Smoothness, out float Metallic)
{
    //procedural creation of color checker
    
    //Number of Rows
    int NumberOfRows = ceil(NumberOfFields/FieldsPerRow);

    //UV : We use object space projection for the checker UV, it's easier because of the changing generated geometry
    float2 baseUV = float2(localPos.x,localPos.y)/float2(FieldsPerRow*SquareSize,NumberOfRows*SquareSize);
    if (!SphereMode)
    {
        baseUV.x = lerp(baseUV.x,1-baseUV.x,step(0.5,localNormal.z)); //so it's in the right way on both sides
    }

    //Contour, so that the checker grid is harmonious around the edge
    float2 contourThickness = GridThickness/(NumberOfRows+1)*0.25;
    float2 contour = baseUV - contourThickness;
    contour = saturate(contour / (1-(contourThickness*2)));


    //Number Of Fields per Row
    //Last Row isn't always full so we need to know its state
    float GetLastRow = step(NumberOfRows-1,NumberOfRows*contour.y);
    float fieldsInlastRow = NumberOfFields%FieldsPerRow;
    fieldsInlastRow = fieldsInlastRow == 0? FieldsPerRow : fieldsInlastRow;
    float TrueFieldsPerRow = lerp(FieldsPerRow,fieldsInlastRow,GetLastRow);


    //unclamped UV of each fields
    float2 unclampedUV = float2(TrueFieldsPerRow*contour.x,NumberOfRows*contour.y);
    //0-1 UV of each fields 
    float2 checkerUV = frac(unclampedUV);

    //Grid
    float FieldsRatio = TrueFieldsPerRow/FieldsPerRow; //As last row isn't always actual squares, we need the ratio to balance the edging values.
    float GridEdge = 0.01*FieldsRatio; // this is so the grids borders aren't too sharp
    float sideMask = step(0.1,abs(localNormal.z));
    float Grid = 1;
    if (SphereMode)
    {
        checkerUV.x=(checkerUV/FieldsRatio)-lerp(1/FieldsRatio*0.5,0,FieldsRatio); //Sphere Mode should keep squared UV
    }
    else
    {
        float GridThicknessColumns = FieldsRatio * GridThickness;
        float2 GridMargins = float2(GridThicknessColumns,GridThickness);
        checkerUV = (checkerUV - GridMargins)/(1-GridMargins*2); //Resized checker UV with the grid
        float2 GridMask = smoothstep (-GridEdge,0,0.5-abs(checkerUV-0.5));
        Grid = GridMask.x*GridMask.y*sideMask;
    }

    //Background Mask

    float backgroundMask = Grid;


    //Sampling the 8x8 procedural colorchecker texture
    float fieldIndex; 
    if (MaterialMode)
    {
        fieldIndex = floor(baseUV.y*(NumberOfFields/6));
    }
    else
    {
        fieldIndex = floor (unclampedUV.x) + FieldsPerRow * floor(unclampedUV.y); 
    }
    float fieldIndexV = floor(fieldIndex/8);
    float fieldIndexU = fieldIndex-fieldIndexV*8;
    float4 colorFields = SAMPLE_TEXTURE2D_LOD(CheckerTexture, SS, float2(fieldIndexU,fieldIndexV)/8,0);

    //BackgroundColor
    float4 backgroundColor = float4(0.04,0.04,0.04,1);
        
    //Base Color
    BaseColor = lerp(backgroundColor,colorFields,Grid);
    
    //CompareToUnlit
    if (CompareToUnlit)
    {
        float unlitMask = smoothstep(-GridEdge*0.5,GridEdge*0.5,dot(checkerUV,normalize(float2(1,-1))))*Grid;
        Emission =colorFields*unlitMask;
        BaseColor=lerp(BaseColor,0,unlitMask);
    }
    else
    {
        Emission = 0;
    }

    //Smoothness and Metallic Default
    Smoothness = 0;
    Metallic = 0;

    //Reflection Mode
    if (ReflectionMode)
    {
        BaseColor = 1;
        Emission = 0;
        Smoothness = 1;
        Metallic=1;
    }

    //Material Mode
    if(MaterialMode)
    {
        Emission = 0;
        Metallic = colorFields.w; //We store the metalness in the alpha channel of the generated texture.
        Smoothness = floor((baseUV.x)*6)/5;
    }

    //Gradient
    if(AddGradient)
    {
        float GradientMask = step(baseUV.y,0.001);
        float4 GradientColor = lerp(GradientColorA,GradientColorB,saturate(pow(baseUV.x,GradientPower)));
        GradientColor = lerp(backgroundColor,GradientColor,sideMask);
        BaseColor = lerp(BaseColor, GradientColor,GradientMask);

        if (CompareToUnlit)
        {
            float UnlitGradientmask = step(baseUV.y,-0.5*(1.0/NumberOfRows))*sideMask; 
            Emission = lerp(Emission, BaseColor*UnlitGradientmask, GradientMask);  
            BaseColor *= 1-UnlitGradientmask;
        }

    }    
}
#endif //MYHLSLINCLUDE_INCLUDED

