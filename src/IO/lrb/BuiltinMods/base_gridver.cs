using System.IO;

namespace linerider.IO.lrb.BuiltinMods
{
    /// <summary>
    /// IoMod implementation for <c>base.gridver</c>
    /// </summary>
    public class base_gridver : IOMod
    {
        enum grid_version
        {
            six2 = 0,
            six1 = 1,
            six0 = 2,
        }

        static int versionFromGridVersion(grid_version version)
        {
            switch (version)
            {
                case grid_version.six2: 
                    return 62;
                case grid_version.six1: 
                    return 61;
                case grid_version.six0: 
                    return 60;
            }
            return 0;
        }

        public override Modtable.Entry? WriteEntry(Track track)
        {
            // 6.2 is assumed if the mod is omitted
            if (track.GetVersion() == 62) return null;
            if (track.GetVersion() == 61) return CreateEntry(new byte[]{(byte)grid_version.six1}, "indicates grid version 6.1, physics might be incorrect if missing");
            if (track.GetVersion() == 60) return CreateEntry(new byte[]{(byte)grid_version.six0}, "indicates grid version 6.0, physics might be incorrect if missing");
            // shouldn't be able to reach here, don't write a mod that is invalid
            return null;
        }

        public override void LoadEntry(Modtable.Entry entry, ref Track track)
        {
            var br = new BinaryReader(new MemoryStream(entry.ModData));
            byte version = br.ReadByte();
            // we currently only support 6.2 and 6.1
            if (version > 1) return;
            track.SetVersion(versionFromGridVersion((grid_version)version));
        }

        public override ushort ModVersion { get => 0; }
        public override string ModName { get => "base.gridver"; }
    }
}