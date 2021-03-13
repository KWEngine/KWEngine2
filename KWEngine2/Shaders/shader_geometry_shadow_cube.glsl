#version 430
layout (triangles) in;
layout (triangle_strip, max_vertices=18) out;

uniform mat4 uShadowMatrices[6];

out vec4 gPosition; // gPosition from GS (output per emitvertex)

void main()
{
    for(int face = 0; face < 6; ++face)
    {
        gl_Layer = face; // built-in variable that specifies to which face we render.
        for(int i = 0; i < 3; ++i) // for each triangle vertex
        {
            gPosition = gl_in[i].gl_Position;
            gl_Position = uShadowMatrices[face] * gPosition;
            EmitVertex();
        }    
        EndPrimitive();
    }
}  