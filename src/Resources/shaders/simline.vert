#version 330 core
uniform vec4 u_color;
uniform bool u_overlay;
uniform float u_scale;
layout(std140) uniform Matrices 
{
    mat4 projection;
    mat4 modelview;
    mat4 mvp;
};

in vec2 in_vertex;
in vec2 in_circle;
in float in_selectflags;
in vec4 in_color;
in vec2 in_linesize;

out vec2 v_circle;
out vec2 v_linesize;
out vec4 v_color;
out float v_selectflags;
void main() 
{
    gl_Position = mvp * vec4(in_vertex,0.0,1.0);
    v_circle = in_circle;
    v_linesize = in_linesize;
    // Alpha channel is priority
    // if equal, prefer vertex color
    if (u_overlay)
        v_color = vec4(0.5,0.5,0.5,0.5);
    else
        v_color = mix(u_color, in_color, float(in_color.a >= u_color.a));
    v_selectflags = in_selectflags;
}