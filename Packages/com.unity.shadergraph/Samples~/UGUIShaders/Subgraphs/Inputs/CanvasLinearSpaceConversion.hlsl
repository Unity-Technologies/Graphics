#ifndef UI_COLOR_SPACE_CFN
#define UI_COLOR_SPACE_CFN

int _UIVertexColorAlwaysGammaSpace;

void IsLinearSpaceConversionRequired_float(out bool linearSpaceConversionRequired)
{
    if (_UIVertexColorAlwaysGammaSpace && !IsGammaSpace())
        linearSpaceConversionRequired = true;
    else
        linearSpaceConversionRequired = false;
}
#endif
