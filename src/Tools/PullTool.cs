using System;
using linerider.Game;
using linerider.Rendering;
using linerider.UI;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common.Input;
using SkiaSharp;

namespace linerider.Tools
{
    public class PullTool : Tool
    {
        public override string Name => "Pull Tool";
        public override SKBitmap Icon => GameResources.icon_tool_pan.Bitmap;
        public override MouseCursor Cursor => game.Cursors.List[CursorsHandler.Type.Line];
        public override bool ShowSwatch => false;
        public bool CanLifelock =>
            InputUtils.Check(Hotkey.ToolLifeLock) && CurrentTools.CurrentTool == this;

        private int target_contact_point = 0;

        private bool picker_enabled = false;

        private bool drawing;

        public double LineLength => Settings.Editor.PullToolLineLength;

        private GameLine workingLine = null;

        private Vector2d _start;
        private Vector2d _end;
        private Vector2d _center;
        private Vector2d _right;
        private int _multiplier;

        public PullTool()
            : base() { }

        void updateTargetContactPoint(Vector2d gameMousePosition)
        {
            Rider currentBosh = game.Track.Timeline.GetFrame(game.Track.momentOffset);
            target_contact_point = 0;
            double min_distance = double.MaxValue;
            for (int i = 0; i < currentBosh.Body.Length; i++)
            {
                SimulationPoint point = currentBosh.Body[i];
                double dist = (gameMousePosition - point.Location).LengthSquared;
                if (dist < min_distance)
                {
                    min_distance = dist;
                    target_contact_point = i;
                }
            }
        }

        private void DeleteLine()
        {
            using TrackWriter trk = game.Track.CreateTrackWriter();
            trk.DisableUndo();
            if (workingLine == null)
                return;
            trk.RemoveLine(workingLine);
            workingLine = null;
            game.Track.Invalidate();
            game.Track.NotifyTrackChanged();
        }

        private void FinalizePlacement()
        {
            Active = false;
            DeleteLine();
            using (TrackWriter trk = game.Track.CreateTrackWriter())
            {
                PlaceLine(trk, false);
            }
            game.Invalidate();
            workingLine = null;
        }

        private void GeneratePreview()
        {
            DeleteLine();
            using TrackWriter trk = game.Track.CreateTrackWriter();
            trk.DisableUndo();
            PlaceLine(trk, true);
        }

        private void PlaceLine(TrackWriter trk, bool preview)
        {
            if (workingLine == null)
            {
                if (!preview)
                    game.Track.UndoManager.BeginAction();
                GameLine added = CreateLine(
                    trk,
                    _multiplier < 0 ? _end : _start,
                    _multiplier < 0 ? _start : _end,
                    _multiplier < 0 ? true : false,
                    false,
                    false,
                    _multiplier == 0 ? LineType.Standard : LineType.Acceleration,
                    Math.Abs(_multiplier),
                    Swatch.GreenMultiplier
                );
                workingLine = added;
                game.Track.NotifyTrackChanged();
                if (!preview)
                    game.Track.UndoManager.EndAction();
            }
            game.Invalidate();
        }

        void calculateLine(Vector2d mouse)
        {
            Vector2d gamespaceMouse = ScreenToGameCoords(mouse);
            Vector2d contact_point = game
                .Track.Timeline.GetFrame(game.Track.momentOffset)
                .Body[target_contact_point]
                .Location;
            Vector2d pointToMouse = gamespaceMouse - contact_point;
            _right = pointToMouse.PerpendicularRight.Normalized();
            _center = gamespaceMouse;
            _start = _center + _right * LineLength * 0.5;
            _end = _center - _right * LineLength * 0.5;
        }

        public override void OnMouseDown(Vector2d mouse)
        {
            base.OnMouseDown(mouse);
            if (game.Track.Playing) return;
            if (picker_enabled)
                return;
            calculateLine(mouse);
            _multiplier = 0;
            GeneratePreview();
            drawing = true;
        }

        public override void OnMouseUp(Vector2d mouse)
        {
            base.OnMouseUp(mouse);
            drawing = false;
            FinalizePlacement();
        }

        public override void Render()
        {
            if (game.Track.Playing) return;
            Vector2d contact_point = game
                .Track.Timeline.GetFrame(game.Track.momentOffset)
                .Body[target_contact_point]
                .Location;
            Color color = Color.Yellow;
            color.A = (byte)(picker_enabled ? 130 : 50);
            GameRenderer.RenderRoundedLine(contact_point, contact_point, color, 1);
            if (!picker_enabled && !drawing)
            {
                Color linecolor = Color.Gray;
                linecolor.A = 130;
                GameRenderer.RenderRoundedLine(_start, _end, linecolor, 2);
            }

            if (drawing)
            {
                GameRenderer.DrawKnob(_start, true, CanLifelock, 1, 1);
                GameRenderer.DrawKnob(_end, true, CanLifelock, 1, 1);
            }
        }

        public override void OnMouseRightDown(Vector2d pos)
        {
            base.OnMouseRightDown(pos);
            if (game.Track.Playing) return;
            if (drawing)
                return;
            picker_enabled = true;
            updateTargetContactPoint(ScreenToGameCoords(pos));
        }

        public override void OnMouseMoved(Vector2d pos)
        {
            if (picker_enabled)
            {
                updateTargetContactPoint(ScreenToGameCoords(pos));
            }
            else if (!drawing)
            {
                calculateLine(pos);
            }
            else if (drawing)
            {
                Vector2d from_original = ScreenToGameCoords(pos) - _center;
                double along_right = Vector2d.Dot(from_original, _right) * Settings.Editor.PullToolMultiplierScale;
                int multiplier_was = _multiplier;
                bool did_lifelock = false;
                _multiplier = -(int)Math.Floor(Math.Clamp(along_right, -255, 255));
                GeneratePreview();
                if (CanLifelock)
                {
                    using TrackReader trk = game.Track.CreateTrackReader();
                    if (!LifeLock(trk, game.Track.Timeline, (StandardLine)workingLine))
                    {
                        _multiplier = multiplier_was;
                        did_lifelock = true;
                    }
                }
                if (did_lifelock)
                {
                    GeneratePreview();
                }
            }
        }

        public override void OnMouseRightUp(Vector2d pos)
        {
            base.OnMouseRightUp(pos);
            picker_enabled = false;
            updateTargetContactPoint(ScreenToGameCoords(pos));
        }

        public override void Cancel()
        {
            drawing = false;
        }

        public override void Stop()
        {
            Cancel();
            workingLine = null;
        }
    }
}
