#version 430

in		vec3 aPosition;
in		ivec3 aBoneIds;
in		vec3 aBoneWeights;

uniform int uUseAnimations;
uniform mat4 uModelMatrix;
uniform mat4 uBoneTransforms[96];

void main()
{
	mat4 BoneTransform = mat4(0.0);
	if(uUseAnimations > 0)
	{	
		BoneTransform += uBoneTransforms[aBoneIds[0]] * aBoneWeights[0];
		BoneTransform += uBoneTransforms[aBoneIds[1]] * aBoneWeights[1];
		BoneTransform += uBoneTransforms[aBoneIds[2]] * aBoneWeights[2];
	}
	else
	{
		BoneTransform = mat4(1.0);
	}
	vec4 totalLocalPos = BoneTransform * vec4(aPosition, 1.0);


	gl_Position = uModelMatrix * totalLocalPos;
}