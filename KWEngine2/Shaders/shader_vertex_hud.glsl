#version 430
 
in		vec3 aPosition;
in		vec2 aTexture;

out		vec2 vTexture;

uniform mat4 uMVP;
uniform int uOffset;
uniform int uIsText;
 
void main()
{
	if(uIsText > 0)
	{
		vTexture.x = aTexture.x / 256.0 + (uOffset / 256.0);// float(uOffset) * (1.0 / 256.0); 
	}
	else
	{
		vTexture.x = aTexture.x;
	}

	vTexture.y = 1.0 - aTexture.y;
	gl_Position = (uMVP * vec4(aPosition, 1.0)).xyww; 
}