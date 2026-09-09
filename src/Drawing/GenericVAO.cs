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

using OpenTK.Graphics.OpenGL;
using System;

namespace linerider.Drawing
{
    public unsafe class GenericVAO : GLArray<GenericVertex>
    {
        private readonly int _vao;
        private readonly GLBuffer<GenericVertex> _vbo;
        public bool useTexture = false;
        public GenericVAO()
        {
            _vbo = new GLBuffer<GenericVertex>(BufferTarget.ArrayBuffer);
            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);
            _vbo.Bind();
            
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, GenericVertex.Size, 0);
            
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.UnsignedByte, true, GenericVertex.Size, 8);
            
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, GenericVertex.Size, 12);

            GL.BindVertexArray(0);
            
            _vbo.Unbind();
        }
        
        private void EnsureBufferSize(int size)
        {
            if (_vbo.BufferSize < size)
            {
                _vbo.SetSize(size * 2, BufferUsageHint.StreamDraw);
            }
        }
        protected override void BeginDraw()
        {
            _vbo.Bind();
            GL.BindVertexArray(_vao);
            EnsureBufferSize(Array.Count);
            _vbo.SetData(Array.unsafe_array, 0, 0, Array.Count);
            Shaders.GenericShader.Use();
            
            int uUseTexture = Shaders.GenericShader.GetUniform("uUseTexture");
            GL.Uniform1(uUseTexture, useTexture ? 1 : 0);
        }
        protected override void InternalDraw(PrimitiveType primitive)
        {
            GL.DrawArrays(primitive, 0, Array.Count);
        }
        protected override void EndDraw()
        {
            Shaders.GenericShader.Stop();
            GL.BindVertexArray(0);
            _vbo.Unbind();
        }
    }
}
