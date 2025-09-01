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
using linerider.Game;
using linerider.Utils;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace linerider.Rendering
{
    public static class GameRenderer
    {
        public static MainWindow Game;
        private static LineVAO _linevao = null;
        private static GenericVAO _gvao = null;

        public static void DrawTrackLine(StandardLine line, Color color, bool drawwell, bool drawcolor)
        {
            AutoArray<LineVertex> lv = new(24);
            AutoArray<GenericVertex> verts = new(30);
            if (drawcolor)
            {
                if (line is RedLine redline)
                {
                    verts.AddRange(LineAccelRenderer.GetAccelDecor(redline));
                }

                lv.AddRange(LineColorRenderer.CreateDecorationLine(line, line.Color));
            }

            lv.AddRange(
                LineRenderer.CreateTrackLine(
                    line.Start,
                    line.End,
                    line.Width * 2,
                    Utility.ColorToRGBA_LE(color)));
            if (drawwell)
            {
                verts.AddRange(WellRenderer.GetWell(line));
            }

            LineVAO vao = GetLineVAO();
            vao.Scale = Game.Track.Zoom;
            foreach (LineVertex v in lv.unsafe_array)
            {
                vao.AddVertex(v);
            }

            GameDrawingMatrix.Enter();
            using (new GLEnableCap(EnableCap.Blend))
            {
                if (verts.Count != 0)
                {
                    GenericVAO gvao = GetGenericVAO();
                    gvao.Clear();
                    foreach (GenericVertex v in verts.unsafe_array)
                    {
                        gvao.AddVertex(v);
                    }

                    gvao.Draw(PrimitiveType.Triangles);
                }

                vao.Draw(PrimitiveType.Triangles);
            }

            GameDrawingMatrix.Exit();
        }

        public static GenericVAO GetGenericVAO()
        {
            _gvao ??= new GenericVAO();
            _gvao.Clear();
            return _gvao;
        }

        private static LineVAO GetLineVAO()
        {
            _linevao ??= new LineVAO();
            _linevao.Clear();
            return _linevao;
        }

        public static void DrawKnob(
            Vector2d position,
            bool highlight,
            bool lifelock,
            float linewidth,
            float growratio)
        {
            Color color = Settings.Computed.BGColor;
            float size = Settings.LimitLineKnobsSize
                ? Math.Min(
                    Constants.KnobSize,
                    Constants.KnobSize * (float)Settings.Computed.UIScale * Constants.MaxLimitedKnobSize /
                    Game.Track.Zoom
                )
                : Constants.KnobSize;

            if (highlight)
            {
                float from = size * 1.05f; // +0.05f to hide antialiased pixels
                float to = 1.05f;
                size = from + (to - 1f) * growratio;
                color = Utility.MixColors(Settings.Computed.BGColor, Settings.Computed.LineColor, 0.5f);
            }

            if (lifelock)
            {
                color = Constants.KnobLifelockColor;
            }

            size *= linewidth * 2;

            RenderRoundedLine(position, position, color, size);
        }

        public static void RenderRoundedLine(Vector2d position, Vector2d position2, Color color, float thickness,
            bool knobs = false, bool redknobs = false)
        {
            using (new GLEnableCap(EnableCap.Blend))
            {
                //using (new GLEnableCap(EnableCap.Texture2D))
                //{
                GameDrawingMatrix.Enter();
                LineVAO vao = GetLineVAO();
                vao.Scale = GameDrawingMatrix.Scale;
                vao.AddLine(position, position2, color, thickness);
                vao.knobstate =
                    knobs ? (redknobs ? (int)KnobState.LifeLock : (int)KnobState.Shown) : (int)KnobState.Hidden;
                vao.Draw(PrimitiveType.Triangles);
                GameDrawingMatrix.Exit();
                //}
            }
        }

        public static void RenderRoundedRectangle(DoubleRect rect, Color color, float thickness, bool gamecoords = true)
        {
            using (new GLEnableCap(EnableCap.Blend))
            {
                //using (new GLEnableCap(EnableCap.Texture2D))
                //{
                if (gamecoords)
                    GameDrawingMatrix.Enter();
                LineVAO vao = GetLineVAO();
                vao.Scale = GameDrawingMatrix.Scale;
                Vector2d vec1 = rect.Vector;
                Vector2d vec2 = vec1 + new Vector2d(rect.Width, 0);
                Vector2d vec3 = vec1 + rect.Size;
                Vector2d vec4 = vec1 + new Vector2d(0, rect.Height);
                vao.AddLine(vec1, vec2, color, thickness);
                vao.AddLine(vec2, vec3, color, thickness);
                vao.AddLine(vec3, vec4, color, thickness);
                vao.AddLine(vec4, vec1, color, thickness);
                vao.knobstate = (int)KnobState.Hidden;
                vao.Draw(PrimitiveType.Triangles);
                if (gamecoords)
                    GameDrawingMatrix.Exit();
                //}
            }
        }

        public static void DbgDrawCamera()
        {
            GameDrawingMatrix.UniformBlock.PushMatrix();
            Vector2 center = new(Game.RenderSize.Width / 2, Game.RenderSize.Height / 2);
            DoubleRect rect = Game.Track.Camera.GetClamp(1, Game.RenderSize.Width, Game.RenderSize.Height);

            rect.Width *= Game.Track.Zoom;
            rect.Height *= Game.Track.Zoom;
            Vector2[] circle = StaticRenderer.GenerateEllipse((float)rect.Width, (float)rect.Height, 100);

            DoubleRect clamprect = new(center.X, center.Y, 0, 0);
            clamprect.Left -= rect.Width / 2;
            clamprect.Top -= rect.Height / 2;
            clamprect.Width = rect.Width;
            clamprect.Height = rect.Height;
            GenericVAO gvao;
            if (!Settings.SmoothCamera && !Settings.RoundLegacyCamera)
            {
                gvao = GetGenericVAO();
                //GL.Begin(PrimitiveType.LineStrip);
                //GL.Color3(0, 0, 0);
                gvao.AddVertex(new GenericVertex(new Vector2((float)clamprect.Left, (float)clamprect.Top),
                    Color.Black));
                gvao.AddVertex(
                    new GenericVertex(new Vector2((float)clamprect.Right, (float)clamprect.Top), Color.Black));
                gvao.AddVertex(new GenericVertex(new Vector2((float)clamprect.Right, (float)clamprect.Bottom),
                    Color.Black));
                gvao.AddVertex(new GenericVertex(new Vector2((float)clamprect.Left, (float)clamprect.Bottom),
                    Color.Black));
                gvao.AddVertex(new GenericVertex(new Vector2((float)clamprect.Left, (float)clamprect.Top),
                    Color.Black));
                gvao.Draw(PrimitiveType.LineStrip);
                GameDrawingMatrix.UniformBlock.PopMatrix();
                return;
            }

            gvao = GetGenericVAO();
            //GL.Begin(PrimitiveType.LineStrip);
            //GL.Color3(0, 0, 0);
            for (int i = 0; i < circle.Length; i++)
            {
                Vector2d pos = (Vector2d)center + (Vector2d)circle[i];
                Vector2d square = clamprect.Clamp(pos);
                Vector2d oval = clamprect.EllipseClamp(pos);
                pos = Vector2d.Lerp(square, oval, CameraBoundingBox.roundness);
                gvao.AddVertex(new GenericVertex((Vector2)pos, Color.Black));
            }

            gvao.Draw(PrimitiveType.LineStrip);

            gvao = GetGenericVAO();
            //GL.End();
            // Visualize example points being clamped
            //GL.Begin(PrimitiveType.Lines);
            circle = StaticRenderer.GenerateEllipse((float)rect.Width / 1.5f, (float)rect.Height / 1.5f, 20);
            for (int i = 0; i < circle.Length; i++)
            {
                Vector2d pos = (Vector2d)center + (Vector2d)circle[i];
                Vector2d square = clamprect.Clamp(pos);
                Vector2d oval = clamprect.EllipseClamp(pos);
                pos = Vector2d.Lerp(square, oval, CameraBoundingBox.roundness);
                if (pos != (Vector2d)center + (Vector2d)circle[i])
                {
                    gvao.AddVertex(new GenericVertex((Vector2)pos, Color.Black));
                    gvao.AddVertex(new GenericVertex(center + circle[i], Color.Black));
                    //GL.Vertex2(pos);
                    //GL.Vertex2((Vector2d)center + (Vector2d)circle[i]);
                }
            }

            gvao.Draw(PrimitiveType.Lines);
            //GL.End();
            GameDrawingMatrix.UniformBlock.PopMatrix();
            // Visualize rider center
            //DrawCircle(Game.Track.Camera.GetSmoothPosition(), 5, Color.Red);
            //DrawCircle(Game.Track.Camera.GetSmoothedCameraOffset(), 5, Color.Blue);
            DrawCircle(Game.Track.Timeline.GetFrame(Game.Track.Offset).CalculateCenter(), 5, Color.Green);
        }

        public static void DrawCircle(Vector2d point, float size, Color color)
        {
            GameDrawingMatrix.Enter();
            Vector2 center = (Vector2)point;
            Vector2d[] circ = StaticRenderer.GenerateCircle(center.X, center.Y, size, 360);
            GenericVAO gvao = GetGenericVAO();
            //GL.Begin(PrimitiveType.LineStrip);
            //GL.Color3(color.R, color.G, color.B);
            for (int i = 0; i < circ.Length; i++)
            {
                gvao.AddVertex(new GenericVertex((Vector2)circ[i], color));
                //GL.Vertex2((Vector2)circ[i]);
            }

            gvao.Draw(PrimitiveType.LineStrip);
            //GL.End();
            GameDrawingMatrix.Exit();
        }

        public static void DrawBezierCurve(Vector2[] points, Color color, int resolution)
        {
            Vector2[] curvePoints = GenerateBezierCurve(points, resolution);
            if (points.Length > 0)
            {
                GameDrawingMatrix.Enter();
                GenericVAO gvao = GetGenericVAO();
                //GL.Begin(PrimitiveType.LineStrip);
                //GL.Color3(color.R, color.G, color.B);
                for (int i = 0; i < curvePoints.Length; i++)
                {
                    gvao.AddVertex(new GenericVertex(curvePoints[i], color));
                    //GL.Vertex2(curvePoints[i]);
                }

                gvao.Draw(PrimitiveType.LineStrip);
                //GL.End();
                GameDrawingMatrix.Exit();
            }
        }

        public static void RenderPoints(List<Vector2d> points, Color color, float nodeSize, float thickness)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (i == 0 || i == points.Count - 1)
                {
                    DoubleRect rect = new(points[i].X - nodeSize, points[i].Y - nodeSize, nodeSize * 2, nodeSize * 2);
                    RenderRoundedRectangle(rect, color, thickness);
                }
                else
                {
                    DrawCircle(points[i], nodeSize, color);
                }
            }

            RenderPointOutline(points, color);
        }

        private static void RenderPointOutline(List<Vector2d> points, Color color)
        {
            GameDrawingMatrix.Enter();
            //GL.Begin(PrimitiveType.LineStrip);
            GenericVAO gvao = GetGenericVAO();
            for (int i = 0; i < points.Count; i++)
            {
                Color col = (i < 1 || i == points.Count - 1) ? color : Color.FromArgb(255, 200, 0);
                //GL.Color3(col.R, col.G, col.B);
                //GL.Vertex2(points[i]);
                gvao.AddVertex(new GenericVertex((Vector2)points[i], col));
            }

            gvao.Draw(PrimitiveType.LineStrip);
            //GL.End();
            GameDrawingMatrix.Exit();
        }

        public static void RenderPoints(List<Vector2d> points, BezierCurve curve, Color color, float nodeSize,
            float thickness)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (i == 0 || i == points.Count - 1)
                {
                    DoubleRect rect = new(points[i].X - nodeSize, points[i].Y - nodeSize, nodeSize * 2, nodeSize * 2);
                    RenderRoundedRectangle(rect, color, thickness);
                }
                else
                {
                    DrawCircle(points[i], nodeSize, color);
                }
            }

            renderPointTrace(points, curve, color);
        }

        private static void renderPointTrace(List<Vector2d> points, BezierCurve curve, Color color)
        {
            double lineLength = 0;
            List<double> lengthsPerPoint = [0];
            for (int i = 1; i < points.Count; i++)
            {
                lineLength += Distance(points[i - 1], points[i]);
                lengthsPerPoint.Add(lineLength);
            }

            for (int i = 1; i < points.Count - 1; i++)
            {
                Vector2 curvePoint = curve.CalculatePoint((float)lengthsPerPoint[i] / (float)lineLength);
                GameDrawingMatrix.Enter();
                GenericVAO gvao = GetGenericVAO();
                //GL.Begin(PrimitiveType.LineStrip);
                //GL.Color3(color.R, color.B, color.G);
                //GL.Vertex2(points[i]);
                gvao.AddVertex(new GenericVertex((Vector2)points[i], color));
                //GL.Vertex2(curvePoint);
                gvao.AddVertex(new GenericVertex(curvePoint, color));
                //GL.End();
                gvao.Draw(PrimitiveType.LineStrip);
                GameDrawingMatrix.Exit();
            }
        }

        public static double Distance(Vector2d a, Vector2d b)
            => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

        public static Vector2[] GenerateBezierCurve(Vector2[] points, int resPerHundred)
        {
            BezierCurve curve = new(points);
            float curveLength = curve.CalculateLength(0.1f);
            float resolution = curveLength / 100 * resPerHundred;
            List<Vector2> curvePoints = [];

            for (int i = 0; i < resolution; i++)
            {
                float t = i / resolution;
                curvePoints.Add(curve.CalculatePoint(t));
            }

            return [.. curvePoints];
        }

        public static Vector2[] GenerateBezierCurve(Vector2d[] points, int resPerHundred)
        {
            Vector2[] newPoints = new Vector2[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                newPoints[i] = (Vector2)points[i];
            }

            BezierCurve curve = new(newPoints);
            List<Vector2> curvePoints = [];
            float curveLength = curve.CalculateLength(0.1f);
            float resolution = curveLength / 100 * resPerHundred;

            for (int i = 0; i < resolution; i++)
            {
                float t = i / (float)resolution;
                curvePoints.Add(curve.CalculatePoint(t));
            }

            return [.. curvePoints];
        }

        public static Vector2d[] GenerateBezierCurve2d(Vector2d[] points, int resPerHundred, out BezierCurve curveOut)
        {
            Vector2[] newPoints = new Vector2[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                newPoints[i] = (Vector2)points[i];
            }

            BezierCurve curve = new(newPoints);
            curveOut = curve;
            List<Vector2d> curvePoints = [];
            float curveLength = curve.CalculateLength(0.1f);
            float resolution = curveLength / 100 * resPerHundred;

            for (int i = 0; i < resolution; i++)
            {
                float t = i / (float)resolution;
                curvePoints.Add((Vector2d)curve.CalculatePoint(t));
            }

            return [.. curvePoints];
        }

        private static int _gridvao = -1;

        static int GridVAO
        {
            get {
                if (_gridvao == -1)
                {
                    _gridvao = GL.GenVertexArray();
                    GL.BindVertexArray(_gridvao);
                    var vbo = GridVBO;
                    GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
                    GL.EnableVertexAttribArray(0);
                    GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2*sizeof(float), 0);
                    GL.BindVertexArray(0);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                }
                return _gridvao;
            }
        }

        // used for both float and sim grid
        
        private static int _gridvbo = -1;

        static int GridVBO
        {
            get
            {
                if (_gridvbo == -1)
                {
                    _gridvbo = GL.GenBuffer();
                }

                return _gridvbo;
            }
        }

        public static unsafe void DrawFloatGrid() // Draws the grid of floating-point 'regions', used in the creation of stable angled kramuals
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, GridVBO);
            GL.BindVertexArray(GridVAO);
            Shader _shader = Shaders.FloatGridShader;
            _shader.Use();

            int u_zoom = _shader.GetUniform("u_zoom"); // Set uniform var used in fragment shader

            GL.Uniform1(u_zoom, Game.Track.Zoom);
            
            GameDrawingMatrix.UniformBlock.PushMatrix();
            GameDrawingMatrix.UniformBlock.Translate(new Vector3d(-Game.ScreenTranslation * Game.Track.Zoom)); // This transforms from pixel coordinates back to world coordinates (used in vert shader)
            //GameDrawingMatrix.UniformBlock.Scale(Game.Track.Zoom, Game.Track.Zoom, 0);
            GameDrawingMatrix.UniformBlock.Scale((float)1.0 / Game.Track.Zoom, (float)1.0 / Game.Track.Zoom, 0);

            float[] verts = new float[8]
            {
                0, 0,
                Game.RenderSize.Width, 0,
                0, Game.RenderSize.Height,
                Game.RenderSize.Width, Game.RenderSize.Height,
            };
            
            fixed (float* ptr = verts)
                GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * 8, (IntPtr)ptr, BufferUsageHint.StreamDraw);
            
            //GL.Begin(PrimitiveType.Quads);

            //GL.Vertex2(new Vector2d(0, 0));
            //GL.Vertex2(new Vector2d(Game.RenderSize.Width, 0));
            //GL.Vertex2(new Vector2d(Game.RenderSize.Width, Game.RenderSize.Height));
            //GL.Vertex2(new Vector2d(0, Game.RenderSize.Height));

            //GL.End();
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
            _shader.Stop();
            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GameDrawingMatrix.UniformBlock.PopMatrix();
        }

        public static unsafe void DrawGrid_Shader(int sqsize) // Draw the grid using per-pixel shading (more efficient for low zoom where more grid-lines are needed)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, GridVBO);
            GL.BindVertexArray(GridVAO);
            Shader _shader = Shaders.SimGridShader;
            _shader.Use();

            int u_zoom = _shader.GetUniform("u_zoom"); // Set uniform var used in fragment shader
            int u_cellsize = _shader.GetUniform("u_cellsize");

            GL.Uniform1(u_zoom, Game.Track.Zoom);
            GL.Uniform1(u_cellsize, (float)sqsize); // TODO: make this sync with DbgDrawGrid() cellsize

            GameDrawingMatrix.UniformBlock.PushMatrix();
            GameDrawingMatrix.UniformBlock.Translate(new Vector3d(-Game.ScreenTranslation * Game.Track.Zoom)); // This transforms from pixel coordinates back to world coordinates (used in vert shader)
            GameDrawingMatrix.UniformBlock.Scale((float)1.0 / Game.Track.Zoom, (float)1.0 / Game.Track.Zoom, 0);

            float[] verts = new float[8]
            {
                0, 0,
                Game.RenderSize.Width, 0,
                0, Game.RenderSize.Height,
                Game.RenderSize.Width, Game.RenderSize.Height,
            };
            
            fixed (float* ptr = verts)
                GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * 8, (IntPtr)ptr, BufferUsageHint.StreamDraw);
            
            /*GL.Begin(PrimitiveType.Quads);

            GL.Vertex2(new Vector2d(0, 0));
            GL.Vertex2(new Vector2d(Game.RenderSize.Width, 0));
            GL.Vertex2(new Vector2d(Game.RenderSize.Width, Game.RenderSize.Height));
            GL.Vertex2(new Vector2d(0, Game.RenderSize.Height));

            GL.End();*/
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
            _shader.Stop();
            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GameDrawingMatrix.UniformBlock.PopMatrix();
        }

        public static void DbgDrawGrid()
        {
            bool fastgrid = false;
            bool renderridersquare = true;
            bool useshadergrid = true;
            int sqsize = fastgrid ? EditorGrid.CellSize : SimulationGrid.CellSize;

            GameDrawingMatrix.UniformBlock.PushMatrix();
            GameDrawingMatrix.UniformBlock.Scale(Game.Track.Zoom, Game.Track.Zoom, 0);
            GameDrawingMatrix.UniformBlock.Translate(new Vector3d(Game.ScreenTranslation));
            //GL.Begin(PrimitiveType.Quads);
            GenericVAO gvao = GetGenericVAO();
            for (int x = -sqsize; x < (Game.RenderSize.Width / Game.Track.Zoom); x += sqsize)
            {
                for (int y = -sqsize; y < (Game.RenderSize.Height / Game.Track.Zoom); y += sqsize)
                {
                    Vector2d yv = new(x + (Game.ScreenPosition.X - Game.ScreenPosition.X % sqsize), y + (Game.ScreenPosition.Y - Game.ScreenPosition.Y % sqsize));

                    if (!fastgrid)
                    {
                        GridPoint gridpos = new((int)Math.Floor(yv.X / sqsize), (int)Math.Floor(yv.Y / sqsize));

                        if (renderridersquare)
                        {
                            if (Game.Track.RenderRider.PhysicsBounds.ContainsPoint(gridpos))
                            {
                                Color col = Color.LightGray;
                                //GL.Color3(Color.LightGray.R, Color.LightGray.G, Color.LightGray.B);
                                
                                Vector2d[] verts = new Vector2d[6]
                                {
                                    yv,
                                    yv + Vector2d.UnitY * sqsize,
                                    yv + Vector2d.UnitY * sqsize + Vector2d.UnitX * sqsize,
                                
                                    yv + Vector2d.UnitY * sqsize + Vector2d.UnitX * sqsize,
                                    yv + Vector2d.UnitX * sqsize,
                                    yv,
                                };

                                foreach (Vector2d v in verts)
                                {
                                    gvao.AddVertex(new GenericVertex((Vector2)v, col));
                                }
                                
                                /*GL.Vertex2(yv);
                                yv.Y += sqsize;
                                GL.Vertex2(yv);
                                yv.X += sqsize;
                                GL.Vertex2(yv);
                                yv.Y -= sqsize;
                                GL.Vertex2(yv);*/
                            }
                        }
                        
                        if (Game.Track.GridCheck(yv.X, yv.Y))
                        {
                            Color col;
                            if (Game.Track.RenderRider.PhysicsBounds.ContainsPoint(gridpos))
                                col=Color.LightSlateGray;//GL.Color3(Color.LightSlateGray.R, Color.LightSlateGray.G, Color.LightSlateGray.B);
                            else
                                col=Color.Yellow;//GL.Color3(Color.Yellow.R, Color.Yellow.G, Color.Yellow.B);
                            Vector2d[] verts = new Vector2d[6]
                            {
                                yv,
                                yv + Vector2d.UnitY * sqsize,
                                yv + Vector2d.UnitY * sqsize + Vector2d.UnitX * sqsize,
                                
                                yv + Vector2d.UnitY * sqsize + Vector2d.UnitX * sqsize,
                                yv + Vector2d.UnitX * sqsize,
                                yv,
                            };

                            foreach (Vector2d v in verts)
                            {
                                gvao.AddVertex(new GenericVertex((Vector2)v, col));
                            }
                            //GL.Vertex2(yv);
                            //yv.Y += sqsize;
                            //GL.Vertex2(yv);
                            //yv.X += sqsize;
                            //GL.Vertex2(yv);
                            //yv.Y -= sqsize;
                            //GL.Vertex2(yv);
                        }
                        
                    }
                    else if (Game.Track.FastGridCheck(yv.X, yv.Y))
                    {
                        /*GL.Color3(Color.Yellow.R, Color.Yellow.G, Color.Yellow.B);
                        GL.Vertex2(yv);
                        yv.Y += sqsize;
                        GL.Vertex2(yv);
                        yv.X += sqsize;
                        GL.Vertex2(yv);
                        yv.Y -= sqsize;
                        GL.Vertex2(yv);*/
                        Color col = Color.Yellow;
                        Vector2d[] verts = new Vector2d[6]
                        {
                            yv,
                            yv + Vector2d.UnitY * sqsize,
                            yv + Vector2d.UnitY * sqsize + Vector2d.UnitX * sqsize,
                                
                            yv + Vector2d.UnitY * sqsize + Vector2d.UnitX * sqsize,
                            yv + Vector2d.UnitX * sqsize,
                            yv,
                        };

                        foreach (Vector2d v in verts)
                        {
                            gvao.AddVertex(new GenericVertex((Vector2)v, col));
                        }
                    }
                }
            }

            //GL.End();
            gvao.Draw(PrimitiveType.Triangles);

            if (!useshadergrid)
            {
                gvao = GetGenericVAO();
                //GL.Begin(PrimitiveType.Lines);
                Color col = Color.Red;
                //GL.Color3(Color.Red.R, Color.Red.G, Color.Red.B);
                for (int x = -sqsize; x < (Game.RenderSize.Width / Game.Track.Zoom); x += sqsize)
                {
                    Vector2d yv = new(x + (Game.ScreenPosition.X - Game.ScreenPosition.X % sqsize), Game.ScreenPosition.Y);
                    gvao.AddVertex(new GenericVertex((Vector2)yv, col));
                    //GL.Vertex2(yv);
                    yv.Y += Game.RenderSize.Height / Game.Track.Zoom;
                    gvao.AddVertex(new GenericVertex((Vector2)yv, col));
                    //GL.Vertex2(yv);
                }
                for (int y = -sqsize; y < (Game.RenderSize.Height / Game.Track.Zoom); y += sqsize)
                {
                    Vector2d yv = new(Game.ScreenPosition.X, y + (Game.ScreenPosition.Y - Game.ScreenPosition.Y % sqsize));
                    gvao.AddVertex(new GenericVertex((Vector2)yv, col));
                    //GL.Vertex2(yv);
                    yv.X += Game.RenderSize.Width / Game.Track.Zoom;
                    gvao.AddVertex(new GenericVertex((Vector2)yv, col));
                    //GL.Vertex2(yv);
                }
                //GL.End();
                gvao.Draw(PrimitiveType.Lines);
            }
            GameDrawingMatrix.UniformBlock.PopMatrix();
            if (useshadergrid)
            {
                DrawGrid_Shader(sqsize);
            }
        }
        public static void DrawAGWs()
        {
            bool renderext = true;

            if (renderext)
            {
                using TrackReader trk = Game.Track.CreateTrackReader();
                foreach (GameLine v in trk.GetLinesInRect(Game.Track.Camera.GetViewport(
                    Game.Track.Zoom,
                    Game.RenderSize.Width,
                    Game.RenderSize.Height), false))
                {
                    if (v is StandardLine std)
                    {
                        if (std.Extension != StandardLine.Ext.None)
                        {
                            Vector2d d = std.Difference * std.ExtensionRatio;
                            if (std.Extension.HasFlag(StandardLine.Ext.Left))
                            {
                                RenderRoundedLine(std.Position1 - d, std.Position1, Color.Red, 1);
                            }
                            if (std.Extension.HasFlag(StandardLine.Ext.Right))
                            {
                                RenderRoundedLine(std.Position2 + d, std.Position2, Color.Red, 1);
                            }
                        }
                    }
                }
            }
        }
    }
}