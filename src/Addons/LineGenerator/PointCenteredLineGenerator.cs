using linerider.Game.Physics;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace linerider.Game.LineGenerator
{
    internal class PointCenteredLineGenerator : Generator
    {
        public Vector2i start_bit_offset = -Vector2i.UnitX;
        public Vector2i end_bit_offset = Vector2i.UnitX;
        public int multiplier = 1;
        public int contact_point = 0;
        public float width = 1; 
        public bool invert = false;
        //public bool reverse = false;
        public LineType lineType;
        public Moment moment;
        public bool after_subiterations = true;

        public PointCenteredLineGenerator(string _name)
        {
            name = _name;
            lines = new List<GameLine>();
        }

        public static double DoubleBitStep(double value, long steps)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return value;

            ulong bits = BitConverter.DoubleToUInt64Bits(value);

            // Total order mapping: converts IEEE bits to monotonic ordering
            ulong ordered = (bits & 0x8000000000000000UL) == 0
                ? bits | 0x8000000000000000UL
                : ~bits;

            if (steps == 0)
                return value;

            // Perform safe arithmetic on ordered bits
            if (steps > 0)
            {
                if (ulong.MaxValue - ordered < (ulong)steps)
                    return double.PositiveInfinity;
                ordered += (ulong)steps;
            }
            else
            {
                ulong uSteps = (ulong)(-steps);
                if (ordered < uSteps)
                    return double.NegativeInfinity;
                ordered -= uSteps;
            }

            // Reverse total ordering transform
            ulong resultBits = (ordered & 0x8000000000000000UL) == 0
                ? ~ordered
                : ordered & ~0x8000000000000000UL;

            return BitConverter.UInt64BitsToDouble(resultBits);
        }
        
        public override void Generate_Internal(TrackWriter track)
        {
            var current = game.Track.Timeline.GetFrame(moment);
            var position = current.Body[contact_point].Location;
            var start = new Vector2d(DoubleBitStep(position.X, start_bit_offset.X), DoubleBitStep(position.Y, start_bit_offset.Y));
            var end = new Vector2d(DoubleBitStep(position.X, end_bit_offset.X), DoubleBitStep(position.Y, end_bit_offset.Y));
            lines.Add(CreateLine(track, start, end, lineType, invert, multiplier, 1));
        }

        public override void Generate_Preview_Internal(TrackWriter trk)
        {
            Generate_Internal(trk);
        }

        public void setCurrentMoment()
        {
            moment = game.Track.momentOffset;
            if (after_subiterations) moment.Subiteration = Moment.maxSubiteration(moment.Iteration);
        }

        public void setFrame(int f)
        {
            moment.Frame = f;
        }

        public void setIteration(int i)
        {
            moment.Iteration = i;
            if (i == 0 || after_subiterations) moment.Subiteration = Moment.maxSubiteration(i);
        }
    }
}