using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using linerider.Drawing;
using System.Runtime.CompilerServices;
using System.Net;
using System;

// manages the UBO that contains the camera ortho transform matrix which the game shaders share

namespace linerider.Rendering {
    static class CameraMatrixUniform {
        static int buffer;
        static bool inited = false;
        const int BINDPOINT = 0; 
        const int buffer_size = sizeof(float) * 16;// 4x4 matrix
        static void Init() {
            CameraMatrixUniform.buffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.UniformBuffer, CameraMatrixUniform.buffer);
            GL.BufferData(BufferTarget.UniformBuffer, buffer_size, 0, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, BINDPOINT, buffer);
            CameraMatrixUniform.inited = true;
        }
        public static void BindShader(int program) {
            if (!CameraMatrixUniform.inited) {
                CameraMatrixUniform.Init();
            }

            int idx = GL.GetUniformBlockIndex(program, "CameraMatrix");
            GL.UniformBlockBinding(program, idx, BINDPOINT);
        }

        public static unsafe void UpdateCamera(Size surface_size, Vector2 translation, float zoom) {
            if (!CameraMatrixUniform.inited) {
                CameraMatrixUniform.Init();
            }

            Matrix4 ortho = Matrix4.CreateOrthographic(surface_size.Width, surface_size.Height, 0f, 1f);
            Matrix4 trans = Matrix4.CreateTranslation(translation.X, -translation.Y, 0);
            Matrix4 center = Matrix4.CreateTranslation(-1f, 1f, 0f);
            Matrix4 scale = Matrix4.CreateScale(zoom);
            Matrix4 result = trans *scale * ortho * center;//* trans; //* scale;
            GL.BindBuffer(BufferTarget.UniformBuffer, CameraMatrixUniform.buffer);
            Matrix4* ptr = (Matrix4*)&result;
            GL.BufferData(BufferTarget.UniformBuffer, buffer_size, (IntPtr)ptr, BufferUsageHint.DynamicDraw);
        }
    }
}