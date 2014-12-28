Shader "Custom/VoxelShader" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" 
		_TopSkin ("Top Skin (RGB)", 2D) = "white" {}
		_SideSkin ("Side Skin (RGB)", 2D) = "white" {}
		_BottomSkin ("Bottom Skin (RGB)", 2D) = "white" {}
		_Break ("Break texture (RGB)", 2D) = "white" {}
		_BreakCoords ("Break coordinates (RGB)", 2D) = "zero" {}
		_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
		_Multiplier("Color Multiplier",Range(1,10)) = 1.0
		_AO ("Ambient Occlusion", 3D) = "white" 
		_ChunkPos ("Chunk Position", Vector) = (0,0,0)
	}
	SubShader {
		
		AlphaTest Greater 0.5
		Cull Off
		

		Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout"}
		LOD 200
		
		CGPROGRAM
		
		#pragma surface surf Lambert alphatest:_Cutoff addshadow
		#pragma target 3.0

		fixed4 _Color;
		float _Multiplier;
		sampler2D _MainTex;
		sampler2D _TopSkin;
		sampler2D _SideSkin;
		sampler2D _BottomSkin;
		sampler2D _Break;
		sampler2D _BreakCoords;
		sampler3D _AO;
		float3 _ChunkPos;

		struct Input {
			float2 uv_MainTex;
			float3 worldNormal;
			float3 worldPos;
			//LIGHTING_COORDS(4,5);			
		};

		float3 mod(float3 p)
		{
			return float3(p.x - floor(p.x), p.y - floor(p.y), p.z - floor(p.z));
		}

		float4 GetTexture(int tile, int tilemod, float3 uv, float3 nrm)
		{			
			if(tile == 0)return (float4)0;

			float3 wuv = uv;
			uv = mod(uv);

			float2 iuv = (float2)0;	
			nrm*=-1.0;

			if(nrm.x < -0.5)
			{
				iuv.x = 1-uv.z;
				iuv.y = 1-uv.y;
			}
			if(nrm.x > 0.5)
			{
				iuv.x = uv.z;
				iuv.y = 1-uv.y;
			}
			if(nrm.y < -0.5)
			{
				iuv.x = uv.x;
				iuv.y = uv.z;
			}
			if(nrm.y > 0.5)
			{
				iuv.x = 1-uv.x;
				iuv.y = uv.z;				
			}
			if(nrm.z < -0.5)
			{
				iuv.x = uv.x;
				iuv.y = 1-uv.y;
			}
			if(nrm.z > 0.5)
			{
				iuv.x = 1-uv.x;
				iuv.y = 1-uv.y;
			}			

			float2 buv = iuv;
			iuv.x /= 64.0;		
			iuv.y /= 64.0;			
			iuv.x += float(tile % 64) / 64.0;
			iuv.y += float(tile / 64) / 64.0;
			iuv = iuv-floor(iuv);						
			iuv.y = 1.0-iuv.y;

			float4 brk = float4(0,0,0,0);
			float4 br = (float4)0;
			for(int la=0;la<16;la++)
			{			
				br = tex2D(_BreakCoords, float2((la+0.5)/16.0,0.5));
				int3 crd1 = ceil((br.rgb * 255.0 - (wuv - _ChunkPos)) + 0.001);
				int3 crd2 = ceil((br.rgb * 255.0 - (wuv - _ChunkPos)) - 0.001);
				if(length((float3)(crd1*crd2))<1) if(br.a > 0.125)
				{
					brk = tex2D(_Break, float2(buv.x/8.0+floor(br.a*8.0)/8.0,buv.y));					
					break;
				}
			}
			//brk.a = 0;
			brk.rgb = brk.a * 0.25;			
			brk.a = 0;								

			if(abs(nrm.x) > 0.5)
			{
				return tex2D(_SideSkin, iuv)-brk;
			}
			if(abs(nrm.z) > 0.5)
			{
				return tex2D(_SideSkin, iuv)-brk;	 
			}
			if(nrm.y > 0.5)
			{
				return tex2D(_BottomSkin, iuv)-brk;
			}
			if(nrm.y < -0.5)
			{
				return tex2D(_TopSkin, iuv)-brk;
			}
			return float4(0,0,0,0);
		}

		void surf (Input IN, inout SurfaceOutput o) {
					
			float2 uvn = IN.uv_MainTex;			
			float4 col = tex2D(_MainTex, uvn);
			
			float4 ct = GetTexture((int)(col.r * 255) + (int)(col.g * 65280), (int)(col.b * 255) , IN.worldPos, IN.worldNormal);				
			ct *=  _Color * _Multiplier;					
			
			float3 wp= IN.worldPos-0.001-_ChunkPos;			
			
			float ao = tex3D(_AO, wp.xyz/16.0).r;			

			o.Albedo = ct.rgb *  pow(saturate(ao+0.05),4.3);
			o.Alpha = ct.a;			
		}
		ENDCG
	} 
	FallBack Off
}
