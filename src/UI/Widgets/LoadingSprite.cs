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

using Gwen.Controls;
using linerider.UI.Components;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using SkiaSharp;
using System;
using linerider.Rendering;
using linerider.Utils;

namespace linerider.UI.Widgets
{
    public class LoadingSprite : Sprite
    {
        private readonly Color Color = Settings.Colors.StandardLine;

        public LoadingSprite(ControlBase canvas) : base(canvas)
        {
            IsTabable = false;
            KeyboardInputEnabled = false;
            MouseInputEnabled = false;

            // Double resolution for less janky sprite quality
            SKBitmap bitmap = SkiaUtils.LoadSVG(GameResources.ux_loading.Raw, 2);
            SetImage(bitmap);
        }
        protected override void Render(Gwen.Skin.SkinBase skin)
        {
            ((Gwen.Renderer.OpenTK)skin.Renderer).Flush();
            float rotation = Environment.TickCount % 1000 / 1000f;
            Vector3d trans = new(X + Width / 2.0f, Y + Height / 2.0f, 0);
            GameDrawingMatrix.UniformBlock.PushMatrix();
            GameDrawingMatrix.UniformBlock.LoadIdentity();
            GameDrawingMatrix.UniformBlock.Translate(new Vector3(Bounds.Left, Bounds.Top + 16, 0));
            GameDrawingMatrix.UniformBlock.Rotate(360 * rotation, 0, 0, 1);
            
            //GameDrawingMatrix.UniformBlock.Translate(trans);
            //GameDrawingMatrix.UniformBlock.Scale(0.5f, 0.5f, 0);
            //GameDrawingMatrix.UniformBlock.Translate(-trans);
            StaticRenderer.DrawTexture((int)m_texture.RendererData, new DoubleRect(-16, -16, 32, 32),
                r: Color.R,
                g: Color.G,
                b: Color.B
            );
            //skin.Renderer.DrawColor = Color.FromArgb(Alpha, Color);
            //skin.Renderer.DrawTexturedRect(m_texture, RenderBounds);
            //((Gwen.Renderer.OpenTK)skin.Renderer).Flush();
            GameDrawingMatrix.UniformBlock.PopMatrix();
        }
    }
}