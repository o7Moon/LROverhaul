using System.IO;
using System.Text;
using linerider.Game;
using OpenTK.Mathematics;

namespace linerider.Utils
{
    public static class EntityRig
    {
        static void outputTextures(BinaryWriter bw)
        {
            bw.Write((byte)4);
            foreach (string s in new[] { "Sled", "Body", "Arm", "Leg" })
            {
                bw.Write((byte)Encoding.UTF8.GetByteCount(s));
                bw.Write(Encoding.UTF8.GetBytes(s));
            }
        }

        enum SpriteType
        {
            Regular = 0,
            Scarf = 1,
        }

        static void outputRegularSprite(BinaryWriter bw, Vector2d pivot, byte id, byte a, byte b)
        {
            bw.Write((byte)SpriteType.Regular);
            bw.Write((byte)a);
            bw.Write((byte)b);
            bw.Write((byte)id);
            // ignored by the blender renderer but corresponds to a pixel on the texture or something
            bw.Write(pivot.X);
            bw.Write(pivot.Y);
        }

        static void outputScarfSprite(BinaryWriter bw, byte start, byte length)
        {
            bw.Write((byte)SpriteType.Scarf);
            bw.Write((byte)start);
            bw.Write((byte)length);
        }

        private class ContactPointPos
        {
            public static readonly Vector2d Sled = new(-552, -167);
            public static readonly Vector2d Body = new(-424, 0);
            public static readonly Vector2d Arm = new(-216, 0);
            public static readonly Vector2d Leg = new(-224, 0);
        }

        enum TextureType
        {
            String = 0,
            Sled = 1,
            Body = 2,
            Arm = 3,
            Leg = 4,
        }

        static void outputSprites(BinaryWriter bw, Rider r)
        {
            bw.Write((byte)9); // count
            outputRegularSprite(
                bw,
                ContactPointPos.Sled,
                (byte)TextureType.Sled,
                RiderConstants.SledTL,
                RiderConstants.SledTR
            );

            outputScarfSprite(bw, (byte)r.Body.Length, (byte)r.Scarf.Length);

            outputRegularSprite(
                bw,
                ContactPointPos.Arm,
                (byte)TextureType.Arm,
                RiderConstants.BodyShoulder,
                RiderConstants.BodyHandLeft
            );

            outputRegularSprite(
                bw,
                ContactPointPos.Arm, // pivot doesnt really matter for the string
                (byte)TextureType.String,
                RiderConstants.BodyHandLeft,
                RiderConstants.SledTR
            );

            outputRegularSprite(
                bw,
                ContactPointPos.Leg,
                (byte)TextureType.Leg,
                RiderConstants.BodyButt,
                RiderConstants.BodyFootLeft
            );

            outputRegularSprite(
                bw,
                ContactPointPos.Body,
                (byte)TextureType.Body,
                RiderConstants.BodyButt,
                RiderConstants.BodyShoulder
            );

            outputRegularSprite(
                bw,
                ContactPointPos.Leg,
                (byte)TextureType.Leg,
                RiderConstants.BodyButt,
                RiderConstants.BodyFootRight
            );

            outputRegularSprite(
                bw,
                ContactPointPos.Arm, // same as above
                (byte)TextureType.String,
                RiderConstants.BodyHandRight,
                RiderConstants.SledTR
            );

            outputRegularSprite(
                bw,
                ContactPointPos.Arm,
                (byte)TextureType.Arm,
                RiderConstants.BodyShoulder,
                RiderConstants.BodyHandRight
            );
        }

        // does not support scenery thickness
        static void outputLines(BinaryWriter bw, GameLine[] lines)
        {
            bw.Write((System.UInt32)lines.Length);
            foreach (GameLine l in lines)
            {
                bw.Write(l.Position1.X);
                bw.Write(l.Position1.Y);
                bw.Write(l.Position2.X);
                bw.Write(l.Position2.Y);
            }
        }

        static void outputFrame(BinaryWriter bw, Rider r)
        {
            for (int i = 0; i < r.Body.Length; i++)
            {
                SimulationPoint p = r.Body[i];
                bw.Write(p.Location.X);
                bw.Write(p.Location.Y);
            }
            for (int i = 0; i < r.Scarf.Length; i++)
            {
                SimulationPoint p = r.Scarf[i];
                bw.Write(p.Location.X);
                bw.Write(p.Location.Y);
            }
        }

        public static void outputTrackData(Track t, uint flagframe)
        {
            Stream stdout = System.Console.OpenStandardOutput();
            BinaryWriter bw = new BinaryWriter(stdout);

            Rider r = t.GetStart();

            outputTextures(bw);
            outputSprites(bw, r);
            outputLines(bw, t.GetLines());

            bw.Write((System.UInt32)flagframe);

            var point_count = r.Body.Length + r.Scarf.Length;
            bw.Write((byte)point_count);

            uint currentframe = 0;

            while (currentframe <= flagframe + 1)
            {
                outputFrame(bw, r);
                r = r.Simulate(t);
                currentframe++;
            }
            bw.Write(0xffffffff); // DONE
        }
    }
}
