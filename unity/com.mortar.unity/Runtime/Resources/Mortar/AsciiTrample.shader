Shader "Hidden/Mortar/AsciiTrample"
{
    Properties
    {
        _MainTex ("Field", 2D) = "black" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "Decay"
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragDecay
            #pragma target 3.0
            #include "Trample.cginc"
            ENDCG
        }

        Pass
        {
            Name "Stamp"
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragStamp
            #pragma target 3.0
            #include "Trample.cginc"
            ENDCG
        }
    }
    Fallback Off
}
