//  Author:
//       Noah Ablaseau <nablaseau@hotmail.com>
//
//  Copyright (c) 2017 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using linerider.Drawing;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
namespace linerider.Rendering
{
    public class GameDrawingMatrix : GameService
    {
        // hacky reimplementation of the legacy matrix stack
        // on opengl core with a uniform block
        public static class UniformBlock
        {
            public enum Mode
            {
                ModelView,
                Projection,
            }

            public static Mode _mode = Mode.ModelView;
            static List<Matrix4> projection = new List<Matrix4>() { Matrix4.Identity };
            static List<Matrix4> modelview = new List<Matrix4>() { Matrix4.Identity };
            static Matrix4 mvp = Matrix4.Identity;
            private static int buffer;
            private static bool inited = false;
            private const int BINDPOINT = 0;
            private const int buffer_size = sizeof(float) * 16 * 3;

            static void Init()
            {
                buffer = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.UniformBuffer, buffer);
                GL.BufferData(BufferTarget.UniformBuffer, buffer_size, 0, BufferUsageHint.DynamicDraw);
                GL.BindBuffer(BufferTarget.UniformBuffer, 0);
                GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 0, buffer);
                RecomputeMVP();
                updateBuffer();
                inited = true;
            }

            static unsafe void updateBuffer()
            {
                Matrix4[] m_buffer = new Matrix4[3];
                m_buffer[0] = projection[projection.Count - 1];
                m_buffer[1] = modelview[modelview.Count - 1];
                m_buffer[2] = mvp;

                GL.BindBuffer(BufferTarget.UniformBuffer, buffer);
                fixed (Matrix4* ptr = m_buffer)
                    GL.BufferSubData(BufferTarget.UniformBuffer, (IntPtr)0, buffer_size, (IntPtr)ptr);
                GL.BindBuffer(BufferTarget.UniformBuffer, 0);
            }

            public static void MatrixMode(Mode mode)
            {
                _mode = mode;
            }

            public static void Scale(float x, float y, float z)
            {
                List<Matrix4> current = currentMatrix();
                current[current.Count - 1] *= Matrix4.CreateScale(new Vector3(x, y, z));
                RecomputeMVP();
                updateBuffer();
            }

            public static void Translate(Vector3d translation)
            {
                List<Matrix4> current = currentMatrix();
                current[current.Count - 1] = Matrix4.CreateTranslation((float)translation.X, (float)translation.Y, (float)translation.Z) * current[current.Count - 1];
                RecomputeMVP();
                updateBuffer();
            }

            public static void Rotate(float angle, float x, float y, float z)
            {
                List<Matrix4> current = currentMatrix();
                current[current.Count - 1] = Matrix4.CreateFromAxisAngle(new Vector3(x, y, z), angle) * current[current.Count - 1];
                RecomputeMVP();
                updateBuffer();
            }

            public static void Ortho(float left, float right, float bottom, float top, float nearVal, float farVal)
            {
                List<Matrix4> current = currentMatrix();
                current[current.Count - 1] = Matrix4.CreateOrthographicOffCenter(left, right, bottom, top, nearVal, farVal);
                RecomputeMVP();
                updateBuffer();
            }

            public static void LoadIdentity()
            {
                List<Matrix4> current = currentMatrix();
                current[current.Count - 1] = Matrix4.Identity;
                RecomputeMVP();
                updateBuffer();
            }

            static List<Matrix4> currentMatrix()
            {
                return _mode == Mode.ModelView ? modelview : projection;
            }

            static void RecomputeMVP()
            {
                mvp = modelview[modelview.Count - 1] * projection[projection.Count - 1];
            }

            public static void PushMatrix()
            {
                List<Matrix4> current = currentMatrix();
                current.Add(current[current.Count - 1]);
            }

            public static void PopMatrix()
            {
                List<Matrix4> current = currentMatrix();
                current.RemoveAt(current.Count - 1);
                RecomputeMVP();
                updateBuffer();
            }

            public static void BindShader(int program)
            {
                if (!inited)
                {
                    Init();
                }
                
                int idx = GL.GetUniformBlockIndex(program, "Matrices");
                GL.UniformBlockBinding(program, idx, BINDPOINT);
            }
        }

        public static float Scale => game.Track.Zoom;
        public static void Enter()
        {
            UniformBlock.PushMatrix();
            UniformBlock.Scale(game.Track.Zoom, game.Track.Zoom, 0);
            UniformBlock.Translate(new Vector3d(game.ScreenTranslation));
        }
        public static void Exit() => UniformBlock.PopMatrix();
        /// <summary>
        /// Converts the input Vector2d in game coordinates to a screen coord
        /// </summary>
        /// <returns>A vector2 ready for drawing in screen space</returns>
        public static Vector2 ScreenCoord(Vector2d coords) => (Vector2)ScreenCoordD(coords);
        /// <summary>
        /// Converts the input Vector2d in game coordinates to a screen coord
        /// </summary>
        public static Vector2d ScreenCoordD(Vector2d coords) => (coords + game.ScreenTranslation) * Scale;
        /// <summary>
        /// Converts the input Vector2d in game coordinates to a screen coord
        /// </summary>
        /// <returns>A vector2 ready for drawing in screen space</returns>
        public static Vector2[] ScreenCoords(Vector2d[] coords)
        {
            Vector2[] screen = new Vector2[coords.Length];
            for (int i = 0; i < coords.Length; i++)
            {
                screen[i] = ScreenCoord(coords[i]);
            }
            return screen;
        }
    }
}
