using System;
using linerider.Utils;
using OpenTK.Mathematics;

namespace linerider.Game
{
    public class SoftCamera : ClampCamera
    {
        protected override Vector2d StepCamera(CameraBoundingBox box, ref Vector2d prev, int frame)
        {
            const int threshold_frame = 152 * 40 + 20;

            const double push = 0.6;
            const double pull = 0.05;

            CameraEntry entry = _frames[frame];

            if (threshold_frame < frame)
            {
                CameraEntry preventry = _frames[frame - 1];
                double multiplier =
                    Math.Atan(((frame + 1) - threshold_frame) * 0.01 - 1.5) * 0.3 + 0.3;
                return prev - (entry.RiderCenter - preventry.RiderCenter) * multiplier;
            }

            Vector2d ret = box.Clamp(prev + entry.CameraOffset);
            Angle a = Angle.FromVector(ret);
            double length = ret.Length;
            double prevlength = prev.Length;
            Vector2d edge = a.MovePoint(
                Vector2d.Zero,
                Math.Max(box.Bounds.Width, box.Bounds.Height)
            );
            double maxlength = box.Clamp(edge).Length;
            double lengthratio = length / maxlength;
            double prevratio = prevlength / maxlength;
            _ = Math.Abs(lengthratio - prevratio);
            if (length > prevlength)
            {
                double dr = lengthratio - prevratio;
                double damper = lengthratio - dr / 2;

                double delta = length - prevlength;
                delta *= Math.Max(pull, push * (1 - Math.Max(0, damper)));
                length = prevlength + delta;
            }

            length -= length * pull;
            return box.Clamp(a.MovePoint(Vector2d.Zero, length));
        }
    }
}
