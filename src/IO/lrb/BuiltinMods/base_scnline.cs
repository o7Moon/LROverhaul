using linerider.Game;
using OpenTK.Mathematics;
using System;
using System.IO;

namespace linerider.IO.lrb.BuiltinMods
{
    /// <summary>
    /// IoMod implementation for <c>base.scnline</c>
    /// </summary>
    public class base_scnline : IOMod
    {
        public override Modtable.Entry? WriteEntry(Track track)
        {
            var stream = new MemoryStream();
            var bw = new BinaryWriter(stream);
            uint count = 0;
            // pad with 0, we seek back to this position and write the length after we know how many lines were written
            bw.Write(count);
            bool any_lines = false;
            foreach (GameLine gline in track.GetLines())
            {
                if (!(gline is SceneryLine)) continue;
                any_lines = true;
                var line = (SceneryLine)gline;
                count++;
                bw.Write((uint)line.ID);
                
                
                bw.Write(line.Position1.X);
                bw.Write(line.Position1.Y);
                bw.Write(line.Position2.X);
                bw.Write(line.Position2.Y);
            }

            // don't write the mod if there aren't any scnlines
            if (!any_lines) return null;

            stream.Seek(0, SeekOrigin.Begin);
            bw.Write(count);

            bw.Flush();

            var entry = CreateEntry(stream.ToArray(), "contains scenery lines, which won't be present if mod is missing");
            return entry;
        }

        public override void LoadEntry(Modtable.Entry entry, ref Track track)
        {
            var br = new BinaryReader(new MemoryStream(entry.ModData));
            var linecount = br.ReadUInt32();
            for (int i = 0; i < linecount; i++)
            {
                var ID = br.ReadUInt32();
                
                var x1 = br.ReadDouble();
                var y1 = br.ReadDouble();
                var x2 = br.ReadDouble();
                var y2 = br.ReadDouble();
                track.AddLine(new SceneryLine(new Vector2d(x1,y1), new Vector2d(x2,y2))
                {
                    ID = (int)ID,
                });

            }
        }

        public override ushort ModVersion { get => 0; }
        public override string ModName { get => "base.scnline"; }
    }
}