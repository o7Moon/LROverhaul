#version 330 core

in vec4 vColor;
in vec2 vUV;

uniform bool uUseTexture;
uniform sampler2D uTex;

out vec4 FragColor;

void main() {
    if (uUseTexture)
        FragColor = texture(uTex, vUV) * vColor;
    else
        FragColor = vColor;
}