using linerider.Utils;
using OpenTK.Mathematics;
using System.IO;

namespace linerider.IO.lrb.BuiltinMods
{
    /// <summary>
    /// IoMod implementation for <c>base.startline</c>
    /// </summary>
    public class base_startline : IOMod
    {
        // we don't support writing a start line - it is converted to a start offset as soon as we load the track
        public override Modtable.Entry? WriteEntry(Track track) => throw new System.NotImplementedException();

        public override void LoadEntry(Modtable.Entry entry, ref Track track)
        {
            var br = new BinaryReader(new MemoryStream(entry.ModData));
            uint lineID = br.ReadUInt32();
            Line line = track.LineLookup[(int)lineID];
            track.StartOffset = line.Position1 + new Vector2d(0, -25);
        }

        public override ushort ModVersion { get => 0; }
        public override string ModName { get => "base.startline"; }
    }
}