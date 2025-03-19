using linerider.Utils;
using System.Collections.Generic;
using System.IO;

namespace linerider.IO.lrb
{
    public static class LRBWriter
    {
        public static string SaveTrack(Track trk, string savename)
        {
            string dir = TrackIO.GetTrackDirectory(trk);
            if (trk.Name.Equals(Constants.InternalDefaultTrackName))
                dir = Path.Combine(Settings.Local.UserDirPath, Constants.TracksFolderName, Constants.DefaultTrackName);
            if (!Directory.Exists(dir))
                _ = Directory.CreateDirectory(dir);
            string filename = Path.Combine(dir, savename + ".lrb");
            List<Modtable.Entry> mods = new List<Modtable.Entry>();
            foreach(ModRegistry.ModRegistryEntry mod in ModRegistry.CheckTrackMods)
            {
                if (mod.IoMod == null) continue;
                var entry = mod.IoMod.WriteEntry(trk);
                if (entry != null) mods.Add(entry);
            }
            Modtable modtable = new Modtable(Modtable.EXPECTED_LRB_VERSION, mods.ToArray());
            using (FileStream file = File.Create(filename))
            {
                modtable.Write(file);
            }

            return filename;
        }
    }
}