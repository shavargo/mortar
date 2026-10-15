#include "UnityCG.cginc"

sampler2D _MainTex;
float4 _MainTex_TexelSize;
float4 _CellCount;

// Fifteen bilinear taps between pixel pairs average a 6x10 cell (AsciiSettings.CellWidth/CellHeight).
fixed4 FragDownsample(v2f_img i) : SV_Target
{
    float2 origin = floor(i.uv * _CellCount.xy) * float2(6.0, 10.0);
    float3 sum = 0.0;
    for (int y = 0; y < 5; y++)
    {
        for (int x = 0; x < 3; x++)
        {
            float2 pixel = origin + float2(1.0 + 2.0 * x, 1.0 + 2.0 * y);
            sum += tex2Dlod(_MainTex, float4(pixel * _MainTex_TexelSize.xy, 0.0, 0.0)).rgb;
        }
    }
    return fixed4(sum / 15.0, 1.0);
}
