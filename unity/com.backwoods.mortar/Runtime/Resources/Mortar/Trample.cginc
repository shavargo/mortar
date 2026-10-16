#include "UnityCG.cginc"

sampler2D _MainTex;
float4 _MainTex_TexelSize;
float _Decay;
float _WalkerCount;
float4 _Walkers[32];

fixed4 FragDecay(v2f_img i) : SV_Target
{
    return tex2D(_MainTex, i.uv) * _Decay;
}

fixed4 FragStamp(v2f_img i) : SV_Target
{
    float prior = tex2D(_MainTex, i.uv).r;
    float2 cell = floor(i.uv * _MainTex_TexelSize.zw) + 0.5;
    float stamped = prior;
    for (int n = 0; n < (int)_WalkerCount; n++)
    {
        float4 walker = _Walkers[n];
        float2 delta = (cell - walker.xy) / max(walker.zw, float2(0.001, 0.001));
        float falloff = saturate(1.0 - length(delta));
        stamped = max(stamped, falloff * falloff);
    }
    return fixed4(stamped, stamped, stamped, 1.0);
}
