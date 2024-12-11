#version 150 core
in vec3 pos;

layout (std140) uniform CameraMatrix {
    mat4 transform;
};

void main() {
    gl_Position = transform * vec4(pos.xyz, 1.0);
}