#version 330 core
layout(std140) uniform Matrices
{
    mat4 projection;
    mat4 modelview;
    mat4 mvp;
};

layout(location = 0) in vec2 aPos;
layout(location = 1) in vec4 aColor;
layout(location = 2) in vec2 aUV;

out vec4 vColor;
out vec2 vUV;
void main() {
    gl_Position = mvp * vec4(aPos, 0.0, 1.0);
    vColor = aColor;
    vUV = aUV;
}