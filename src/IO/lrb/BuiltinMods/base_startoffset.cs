using linerider.Utils;
using OpenTK.Mathematics;
using System;
using System.Text;
using System.IO;
namespace linerider.IO.lrb.BuiltinMods
{
    /// <summary>
    /// IOMod implementation for <c>base.startoffset</c>.
    /// </summary>
    public class base_startoffset : IOMod
    {
        public override void LoadEntry(Modtable.Entry entry, ref Track track)
        {
            var br = new BinaryReader(new MemoryStream(entry.ModData));
            var X = br.ReadDouble();
            var Y = br.ReadDouble();
            track.StartOffset = new Vector2d(X, Y);
        }

        public override Modtable.Entry? WriteEntry(Track track)
        {
            // omit if 0, 0
            if (track.StartOffset == Vector2.Zero) return null;
            byte[] bytes = new byte[sizeof(double) * 2];
            var bw = new BinaryWriter(new MemoryStream(bytes));
            bw.Write(track.StartOffset.X);
            bw.Write(track.StartOffset.Y);
            bw.Flush();
            var entry = CreateEntry(bytes, "contains the start offset, physics will be very wrong if missing");
            return entry;
        }
        public override string ModName { get => "base.startoffset"; }
        public override ushort ModVersion { get => 0; }
    }
}