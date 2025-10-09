Shader "Custom/FishEye"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _StrengthX ("Strength X", Range(-0.5, 0.5)) = 0.1
        _StrengthY ("Strength Y", Range(-0.5, 0.5)) = 0.1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _StrengthX;
            float _StrengthY;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;

                // Центр координат
                float2 center = uv - 0.5;
                float dist = dot(center, center);

                // Искажение
                uv.x += center.x * dist * _StrengthX;
                uv.y += center.y * dist * _StrengthY;

                return tex2D(_MainTex, uv);
            }
            ENDCG
        }
    }
}
