using linerider.IO.lrb;
using System.IO;

namespace linerider.IO
{
    public static class LRBLoader
    {
        public static Track LoadTrack(string trackfile, string trackname)
        {
            Track ret = new Track
            {
                Filename = trackfile,
                Name = trackname,
            };
            Modtable modtable;
            byte[] bytes = File.ReadAllBytes(trackfile);
            using (MemoryStream file =
                   new MemoryStream(bytes))
            {
                if (!Modtable.TryParse(file, out modtable))
                {
                    throw new TrackIO.TrackLoadException("could not parse lrb modtable");
                }
            }

            foreach (Modtable.Entry entry in modtable.Entries)
            {
                IOMod? reader_impl = ModRegistry.FindMatchingMod(entry);
                if (reader_impl == null)
                {
                    if (entry.ModFlags.HasFlag(Modtable.modflags.required))
                    {
                        throw new TrackIO.TrackLoadException(
                            $"could not find implementation for mod {entry.ModName} version {entry.ModVersion}, which is required to load the track.");
                    }
                    else
                    {
                        // TODO handle optional mods
                    }
                    continue;
                }
                reader_impl.LoadEntry(entry, ref ret);
            }

            return ret;
        }
    }
}