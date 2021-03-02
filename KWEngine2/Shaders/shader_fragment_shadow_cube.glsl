#version 430

out float map;

void main()
{
	map = gl_FragCoord.z;
}