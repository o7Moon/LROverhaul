#version 330 core

layout(std140) uniform Matrices
{
	mat4 projection;
	mat4 modelview;
	mat4 mvp;
};

in vec2 vertex_position;

out vec2 world_position;
out vec2 pixel_coord;

void main() 
{
	gl_Position = projection * vec4(vertex_position, 0, 1); // Vertex position is not translated/scaled as it's just used to cover the screen, so only use projection matrix
    vec4 position = modelview * vec4(vertex_position, 0, 1); // Position of the vertex in the track 'world'
	world_position = position.xy;
	pixel_coord = vertex_position.xy; // The pixel coordinate of the vertex (to be used in frag shader to determine line location)
}