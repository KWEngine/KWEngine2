#version 450

in		vec3 vPosition;
in		vec3 vNormal;
in		vec2 vTexture;
in		vec2 vTexture2;
in		mat3 vTBN;
in		vec4 vShadowCoord[3];

uniform sampler2D uTextureAlbedo;
uniform int uUseTextureAlbedo;

uniform sampler2D uTextureNormal;
uniform int uUseTextureNormal;

uniform sampler2D uTextureRoughness;
uniform int uUseTextureRoughness;
uniform int uUseTextureRoughnessIsSpecular;
uniform float uRoughness;

uniform sampler2D uTextureMetalness;
uniform int uUseTextureMetalness;
uniform float uMetalness;

uniform sampler2D uTextureEmissive;
uniform int uUseTextureEmissive;
uniform vec4 uEmissiveColor;

uniform sampler2D uTextureLightmap;
uniform int uUseTextureLightmap;

uniform sampler2DShadow uTextureShadowMap[3];
uniform samplerCube uTextureShadowMapCubeMap[3];

uniform float uOpacity;
uniform vec3 uAlbedoColor;
uniform vec4 uGlow;
uniform vec4 uOutline;
uniform vec3 uTintColor;



uniform vec3 uCameraPos;
uniform vec3 uCameraDirection;

uniform samplerCube uTextureSkybox;
uniform sampler2D uTexture2D;
uniform int uUseTextureSkybox;

uniform vec4 uLightsPositions[10];
uniform vec4 uLightsTargets[10];
uniform vec4 uLightsColors[10];
uniform vec2 uLightsMeta[10];
uniform int uLightCount;
uniform int uLightAffection;
uniform vec4 uSunAmbient;
//uniform float uFarPlane;

uniform int uSpecularReflectionFactor;

out vec4 color;
out vec4 bloom;

const float cpi = 3.14159265358979323846264338327950288419716939937510f;

float chiGGX(float f)
{
	return clamp(f * f, 0.0, 1.0);
}

float chiGGX2(float f)
{
	return f > 0.0 ? 1.0 : 0.0 ;
}

float computeGGXDistribution(float pDotNormalLight, float pRoughness)
{
	float fNormalDotLightSquared = pDotNormalLight * pDotNormalLight ;
	float fRoughnessSquared = pRoughness * pRoughness;
	float fDen = fNormalDotLightSquared * fRoughnessSquared + (1.0 - fNormalDotLightSquared);
	return clamp((chiGGX(pDotNormalLight) * fRoughnessSquared) / (cpi * fDen * fDen), 0.0, 1.0);
}

float computeGGXPartialGeometryTerm(vec3 pSurfaceToCameraDirection, vec3 pSurfaceNormal, vec3 pLightViewHalfVector, float pRoughness)
{
	float fViewerDotLightViewHalf = max(dot(pSurfaceToCameraDirection, pLightViewHalfVector), 0.0);
	float fChi = chiGGX2(fViewerDotLightViewHalf / max(dot(pSurfaceToCameraDirection, pSurfaceNormal), 0.0));
	fViewerDotLightViewHalf *= fViewerDotLightViewHalf;
	float fTan2 = (1.0 - fViewerDotLightViewHalf) / fViewerDotLightViewHalf;
	return clamp((fChi * 2.0) / (1.0 + sqrt(1 + pRoughness * pRoughness * fTan2)), 0.0, 1.0) ;
}

float calculateDarkening(float cosTheta, int index)
{
	float bias = uLightsMeta[index].x * sqrt ( 1.0f - cosTheta * cosTheta   ) / cosTheta;
	bias = clamp(bias, 0.0, 0.01);
	vec4 shadowCoord = vShadowCoord[index];
	shadowCoord.z -= bias;
	float darkening = 0.0;
	darkening += textureProjOffset(uTextureShadowMap[index], shadowCoord, ivec2(-1,-1));
	darkening += textureProjOffset(uTextureShadowMap[index], shadowCoord, ivec2(-1, 1));
	darkening += textureProjOffset(uTextureShadowMap[index], shadowCoord, ivec2( 0, 0));
	darkening += textureProjOffset(uTextureShadowMap[index], shadowCoord, ivec2( 1, 1));
	darkening += textureProjOffset(uTextureShadowMap[index], shadowCoord, ivec2( 1,-1));
	darkening /= 5.0;
	return max(darkening, 0.0);
}

vec3 getSpecularComponent(vec3 theNormal, vec3 fragmentToLight, float roughnessInverted, float distanceFactor, int i, vec3 fragmentToCamera)
{
	vec3 reflectVector = reflect(-fragmentToLight, theNormal);
	float dotReflectionCamera = pow(max(dot(reflectVector, fragmentToCamera), 0.0), 2.0);
	return 
		  distanceFactor 
		* dotReflectionCamera
		* roughnessInverted 
		* uLightsColors[i].xyz * uLightsColors[i].w
		* pow(max(dot(reflectVector, fragmentToCamera), 0.0), (roughnessInverted * roughnessInverted) * 8192.0);
}

void main()
{
	// Texture mapping:
	vec3 albedo = uAlbedoColor;
	if(uUseTextureAlbedo > 0)
	{
		vec4 texColor4 = texture(uTextureAlbedo, vTexture);
		if(texColor4.w <= 0.5)
		{
			discard;
		}
		albedo = texColor4.xyz * uTintColor;
		if(uUseTextureLightmap > 0)
		{
			albedo *= texture(uTextureLightmap, vTexture2).xyz;
		}
	}
	albedo *= uTintColor;

	float test = texture(uTextureShadowMapCubeMap[0], albedo).r;

	vec3 fragmentToCamera = normalize(uCameraPos - vPosition);

	// Normal mapping:
	vec3 theNormal = vNormal;
	if(uUseTextureNormal > 0)
    {
        theNormal = vTBN * (texture(uTextureNormal, vTexture).xyz * 2.0 - 1.0);	// ? normalize necessary?
    }
	

	// Ambient and emissive:
	vec3 ambient = uSunAmbient.xyz * uSunAmbient.w;
	vec3 emissive = vec3(0.0);
	if(uUseTextureEmissive > 0)
	{
		emissive = texture(uTextureEmissive, vTexture).xyz;
	}
	else
	{
		emissive = uEmissiveColor.xyz;
	}



	// Metalness / Reflections:
	vec3 refl = vec3(0.22 * uSunAmbient.xyz * uSunAmbient.w);
	vec3 metalnessTextureLookup = vec3(0);
	if(uUseTextureSkybox > 0)
	{
		vec3 reflectedCameraSurfaceNormal = reflect(-fragmentToCamera, theNormal);
		refl = texture(uTextureSkybox, reflectedCameraSurfaceNormal).xyz * (uSunAmbient.xyz * uSunAmbient.w);
	}
	vec3 reflection = refl;
	vec3 metalness = vec3(uMetalness);
	if(uUseTextureMetalness > 0)
	{
		metalnessTextureLookup = texture(uTextureMetalness, vTexture).xyz;
		metalness = vec3(metalnessTextureLookup.z);
	}
	reflection *= metalness;
	reflection = min(refl, reflection);

	// Roughness:
	float roughness = uRoughness;
	if(uUseTextureRoughness > 0)
	{
		roughness = texture(uTextureRoughness, vTexture).y;
		if(uUseTextureRoughnessIsSpecular > 0)
		{
			roughness = 1.0 - roughness;
		}
	}
	else
	{
		if(uUseTextureRoughnessIsSpecular > 0)
		{
			roughness = metalnessTextureLookup.y;
		}
	}
	float roughnessInverted = 1.0 - roughness;
	float roughnessInvertedClamped = clamp(roughnessInverted, 0.0, 0.999);

	// Loop for lights:
	vec3 colorComponentSpecularTotalFromLights = vec3(0.0);
	vec3 colorComponentIntensityTotalFromLights = vec3(0.0);
	if(uLightAffection > 0)
	{
		for(int i = 0; i < uLightCount; i++)
		{
			vec3 lightPos = uLightsPositions[i].xyz;
			vec3 fragmentToCurrentLightNotNormalized = lightPos - vPosition;
			vec3 fragmentToCurrentLightNormalized = normalize(fragmentToCurrentLightNotNormalized);
			float currentLightDistanceSq = dot(fragmentToCurrentLightNotNormalized, fragmentToCurrentLightNotNormalized);
			float distanceFactor = clamp(uLightsPositions[i].w / currentLightDistanceSq, 0.0, 1.0);

			// Shadow mapping:
			float darkeningCurrentLight = 1.0;
			if(uLightsMeta[i].y > 0.0)
			{
				float dotLightSurfaceVNormal = max(dot(vNormal, fragmentToCurrentLightNormalized), 0.0);
				darkeningCurrentLight = calculateDarkening(dotLightSurfaceVNormal, i);
			}


			// optional: spot light cone
			float differenceLightDirectionAndFragmentDirection = 1.0;
			if(uLightsTargets[i].w > 0.0)
			{
				vec3 lightDirection = normalize(uLightsTargets[i].xyz - lightPos);
				differenceLightDirectionAndFragmentDirection = max(dot(lightDirection, -fragmentToCurrentLightNormalized), 0.0);
			}

			// light intensity:
			float dotNormalCurrentLight = max(dot(theNormal, fragmentToCurrentLightNormalized), 0.0);
			float currentLightIntensity = dotNormalCurrentLight * distanceFactor * pow(differenceLightDirectionAndFragmentDirection, 4.0);

			// roughness:
			float distributionMicroFacetCurrentLight = computeGGXDistribution(dotNormalCurrentLight, roughness);
			float geometryMicroFacetCurrentLight = computeGGXPartialGeometryTerm(fragmentToCamera, theNormal, (0.5 * fragmentToCamera + 0.5 * fragmentToCurrentLightNormalized), roughness);
			float microFacetContributionCurrentLight = distributionMicroFacetCurrentLight * geometryMicroFacetCurrentLight;

			vec3 rgbSpecularCurrentLight = vec3(0.0);
			if(dotNormalCurrentLight > 0 && darkeningCurrentLight > 0)
			{
				rgbSpecularCurrentLight = uLightsColors[i].xyz * uLightsColors[i].w * clamp(distanceFactor * 10, 0.0, 1.0) * differenceLightDirectionAndFragmentDirection;
				rgbSpecularCurrentLight *= microFacetContributionCurrentLight;
				rgbSpecularCurrentLight = min(uLightsColors[i].xyz * uLightsColors[i].w, rgbSpecularCurrentLight) ; // conservation of energy
			}
			if(uSpecularReflectionFactor > 0)
			{	
				rgbSpecularCurrentLight += getSpecularComponent(theNormal, fragmentToCurrentLightNormalized, roughnessInverted, distanceFactor, i, fragmentToCamera);
			}

			colorComponentIntensityTotalFromLights += uLightsColors[i].xyz * uLightsColors[i].w * currentLightIntensity * darkeningCurrentLight;
			colorComponentSpecularTotalFromLights += rgbSpecularCurrentLight;
		}
	}



	// read pure texture and tone down for ambient:
	vec3 rgbFragment = albedo * (1.0 - metalness) * uSunAmbient.xyz * uSunAmbient.w + emissive;
	rgbFragment += colorComponentIntensityTotalFromLights;
	rgbFragment += colorComponentSpecularTotalFromLights;

	// Add reflection from skybox:
	rgbFragment += reflection;
	
	float dotOutline = max(1.0 - 4.0 * pow(abs(dot(uCameraDirection, vNormal)), 2.0), 0.0) * uOutline.w * test;
	color.xyz = rgbFragment + uOutline.xyz * dotOutline * 0.9;
	color.w = uOpacity;

	vec3 addedBloom = vec3(max(rgbFragment.x - 1.0, 0.0), max(rgbFragment.y - 1.0, 0.0), max(rgbFragment.z - 1.0, 0.0));
	addedBloom *= 0.1;
	bloom.x = addedBloom.x + uGlow.x * uGlow.w + uOutline.x * dotOutline * 0.15 + emissive.x * 0.125;
	bloom.y = addedBloom.y + uGlow.y * uGlow.w + uOutline.y * dotOutline * 0.15 + emissive.y * 0.125;
	bloom.z = addedBloom.z + uGlow.z * uGlow.w + uOutline.z * dotOutline * 0.15 + emissive.z * 0.125;
	bloom.w = uEmissiveColor.w > 0.0 ? uEmissiveColor.w : 1.0;
}