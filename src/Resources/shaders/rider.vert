#version 330 core
uniform sampler2D u_bodytex;
uniform sampler2D u_limbtex;
uniform sampler2D u_sledtex;
layout(std140) uniform Matrices
{
    mat4 projection;
    mat4 modelview;
    mat4 mvp;
};
in vec2 in_vertex;
in vec2 in_texcoord;
in float in_unit;
in vec4 in_color;

out vec4 v_color;
out vec2 v_texcoord; 
out float v_unit;
void main() 
{
    gl_Position = mvp * vec4(in_vertex, 0.0, 1.0);
    v_color = in_color;
    v_texcoord = in_texcoord;
    v_unit = in_unit;
}