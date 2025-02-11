using linerider.Game;
using OpenTK.Mathematics;
using System;
using System.IO;

namespace linerider.IO.lrb.BuiltinMods
{
    /// <summary>
    /// IoMod implementation for <c>base.simline</c>
    /// </summary>
    public class base_simline : IOMod
    {
        public override Modtable.Entry WriteEntry(Track track)
        {
            var stream = new MemoryStream();
            var bw = new BinaryWriter(stream);
            uint count = 0;
            // pad with 0
            bw.Write(count);
            foreach (GameLine gline in track.GetLines())
            {
                if (gline is SceneryLine) continue;
                var line = (StandardLine)gline;
                count++;
                bw.Write((uint)line.ID);
                
                var flags = (LineFlags)0;
                if (line is RedLine) flags |= LineFlags.Red;
                if (line.inv) flags |= LineFlags.Inverted;
                if (line.Extension.HasFlag(StandardLine.Ext.Left)) flags |= LineFlags.LeftExtension;
                if (line.Extension.HasFlag(StandardLine.Ext.Right)) flags |= LineFlags.RightExtension;
                bw.Write((byte)flags);
                
                bw.Write(line.Position1.X);
                bw.Write(line.Position1.Y);
                bw.Write(line.Position2.X);
                bw.Write(line.Position2.Y);
            }

            stream.Seek(0, SeekOrigin.Begin);
            bw.Write(count);

            bw.Flush();

            var entry = CreateEntry(stream.ToArray(), "contains simulation lines, which won't be present if mod is missing");
            return entry;
        }

        public override void LoadEntry(Modtable.Entry entry, ref Track track)
        {
            var br = new BinaryReader(new MemoryStream(entry.ModData));
            var linecount = br.ReadUInt32();
            for (int i = 0; i < linecount; i++)
            {
                var ID = br.ReadUInt32();
                
                var flags = (LineFlags)br.ReadByte();
                
                var x1 = br.ReadDouble();
                var y1 = br.ReadDouble();
                var x2 = br.ReadDouble();
                var y2 = br.ReadDouble();

                var inv = flags.HasFlag(LineFlags.Inverted);
                var red = flags.HasFlag(LineFlags.Red);
                
                uint ext_n = 0;
                if (flags.HasFlag(LineFlags.LeftExtension)) ext_n = 1;
                if (flags.HasFlag(LineFlags.RightExtension)) ext_n += 2;
                var ext = (StandardLine.Ext)ext_n;

                if (red)
                {
                    track.AddLine(new RedLine(new Vector2d(x1,y1), new Vector2d(x2,y2), inv)
                    {
                        ID = (int)ID,
                        Extension = ext,
                    });
                }
                else
                {
                    track.AddLine(new StandardLine(new Vector2d(x1,y1), new Vector2d(x2,y2), inv)
                    {
                        ID = (int)ID,
                        Extension = ext,
                    });
                }
            }
        }

        public override ushort ModVersion { get => 0; }
        public override string ModName { get => "base.simline"; }
        
        [Flags]
        enum LineFlags : byte
        {
            Red = 1 << 0,
            Inverted = 1 << 1,
            LeftExtension = 1 << 2,
            RightExtension = 1 << 3,
        }
    }
}