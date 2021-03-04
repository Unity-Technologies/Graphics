// UI Editable properties
uniform fixed4      _FaceColor;                 // RGBA : Color + Opacity
uniform float       _FaceDilate;                // v[ 0, 1]

uniform fixed4      _OutlineColor;              // RGBA : Color + Opacity
uniform float       _OutlineWidth;              // v[ 0, 1]
uniform float       _OutlineSoftness;           // v[ 0, 1]

// API Editable properties
uniform float       _WeightNormal;
uniform float       _WeightBold;

uniform float       _ScaleRatioA;
uniform float       _ScaleRatioB;
uniform float       _ScaleRatioC;

uniform float       _VertexOffsetX;
uniform float       _VertexOffsetY;

// Font Atlas properties
uniform sampler2D   _MainTex;
uniform float       _GradientScale;
uniform float       _ScaleX;
uniform float       _ScaleY;
uniform float       _Sharpness;
