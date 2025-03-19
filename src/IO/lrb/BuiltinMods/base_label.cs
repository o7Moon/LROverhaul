using linerider.Utils;
using System;
using System.Text;
using System.IO;
namespace linerider.IO.lrb.BuiltinMods
{
    /// <summary>
    /// IOMod implementation for <c>base.label</c>.
    /// </summary>
    public class base_label : IOMod
    {
        public override void LoadEntry(Modtable.Entry entry, ref Track track)
        {
            var br = new BinaryReader(new MemoryStream(entry.ModData));
            var len = br.ReadUInt16();
            var utf8 = br.ReadBytes(len);
            track.Name = Encoding.UTF8.GetString(utf8);
        }

        public override Modtable.Entry? WriteEntry(Track track)
        {
            var label = track.Name;
            // we don't really need to write the label mod if it's just the default name
            if (label == Constants.InternalDefaultTrackName) return null;
            var labelbytes = Encoding.UTF8.GetBytes(label);
            if (labelbytes.Length > ushort.MaxValue)
            {
                Array.Resize(ref labelbytes, ushort.MaxValue);
            }
            ushort length = (ushort)labelbytes.Length;
            byte[] bytes = new byte[(uint)length + 2];
            var bw = new BinaryWriter(new MemoryStream(bytes));
            bw.Write(length);
            bw.Write(labelbytes);
            bw.Flush();
            var entry = CreateEntry(bytes, "contains the track's name, only really necessary for displaying in an editor.");
            return entry;
        }
        public override string ModName { get => "base.label"; }
        public override ushort ModVersion { get => 0; }
    }
}