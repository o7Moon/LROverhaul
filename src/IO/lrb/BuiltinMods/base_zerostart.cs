namespace linerider.IO.lrb.BuiltinMods
{
    /// <summary>
    /// IoMod implementation for <c>base.zerostart</c>
    /// </summary>
    public class base_zerostart : IOMod
    {
        public override Modtable.Entry? WriteEntry(Track track)
        {
            if (track.ZeroStart) return CreateEntry();
            // omit if not enabled
            return null;
        }

        public override void LoadEntry(Modtable.Entry entry, ref Track track)
        {
            track.ZeroStart = true;
        }

        public override ushort ModVersion { get => 0; }
        public override string ModName { get => "base.zerostart"; }
    }
}