///Adapted from Skydome shader by Martijn Dekker aka Pixelstudio
Shader "Holistic/Sky" {
Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _cloudHeight1("Cloud 1 Height",float) = 1
		_cloudHeight2("Cloud 2 Height",float) = 1
		_cloudSpeed1("Cloud Speed 1",float) = 1
		_cloudSpeed2("Cloud Speed 2",float) = 1
		_fade("Cloud fade height",float) = 0
		_WindDir("Wind Direction", Vector) = (0,0,0)
		_SkyCol ("Sky Colour",  Color) = (0.7, 0.8, 0.9)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv1 : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float orgposz : TEXCOORD2;
                float intensity : TEXCOORD3;
                UNITY_FOG_COORDS(4)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _cloudHeight1;
			float _cloudHeight2;
			float _cloudSpeed1;
			float _cloudSpeed2;
			float4 _WindDir;
			float4 _SkyCol;
			float _fade;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.orgposz = abs(v.vertex.y);
                float t1 = _Time * _cloudSpeed1;
				float t2 = _Time * _cloudSpeed2;

                float3 norm = normalize(v.vertex);
                
                float2 len1 = float2(norm.z,norm.x) * _cloudHeight1;
				float2 len2 = float2(norm.z,norm.x) * _cloudHeight2;
                o.uv1.xy = 0.1 * len1 + t1 / 10 * _WindDir.xy * o.orgposz;
				o.uv2.xy = 0.2 * len2 + t2 / 10 * float2(_WindDir.z,0) * o.orgposz;

				float fadeHeight = _fade/64;
				o.intensity = max(norm.y - fadeHeight, 0);

                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float4 noise1 = tex2D( _MainTex, i.uv1.xy/i.orgposz);
				float4 noise2 = tex2D( _MainTex, i.uv2.xy/i.orgposz);
				float cloudAlpha = max(noise1.a, noise2.a);
				float intensity = 1 - exp(-512 * i.intensity);
				float4 cloudColor = (noise1 + noise2);
				float4 col = cloudColor.z * intensity * cloudColor + _SkyCol;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
