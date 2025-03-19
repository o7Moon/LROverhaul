using linerider.IO.lrb.BuiltinMods;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace linerider.IO.lrb
{
    public static class ModRegistry
    {
        public interface ModRegistryEntry
        {
            string ModName { get; }
            ushort ModVersion { get; }
            IOMod? IoMod { get; }// can have null here to register mods we don't support but know about
        }

        private static List<ModRegistryEntry> mods = new List<ModRegistryEntry>();
        private static List<ModRegistryEntry> check_track_mods = new List<ModRegistryEntry>();
        public static ReadOnlyCollection<ModRegistryEntry> CheckTrackMods {
            get => check_track_mods.AsReadOnly();
        }

        private static bool inited = false;

        /// <summary>
        /// register an iomod handler.
        /// </summary>
        /// <param name="entry">an IOMod to register</param>
        /// <param name="check_track">if true, this mod's writer will always be called when saving a track, in order to check if the mod needs to be written. mods registered this way can return null in their writer to indicate they don't need to be written. this is really only for builtin mods which may or may not need to be written depending on the contents of the track object, mods registered this way should be kept to a minimum to minimize overhead</param>
        /// <returns>the same entry passed</returns>
        static ModRegistryEntry register(ModRegistryEntry entry, bool check_track = false)
        {
            mods.Add(entry);
            if (check_track) check_track_mods.Add(entry);
            return entry;
        }

        public static void Init()
        {
            register(new base_label(), true);
            register(new base_gridver(), true);
            register(new base_zerostart(), true);
            register(new base_simline(), true);
            register(new base_scnline(), true);
            register(new base_startoffset(), true);
            // don't check track, because we don't write this mod, only load it
            register(new base_startline());
            
            inited = true;
        }

        static void check_init()
        {
            if (!inited) Init();
        }

        public static IOMod? FindMatchingMod(string modName, ushort modVersion)
        {
            check_init();
            foreach (ModRegistryEntry mod in mods) {
                if (mod.ModName == modName && mod.ModVersion == modVersion)
                {
                    return mod.IoMod;
                }
            }

            return null;
        }

        public static IOMod? FindMatchingMod(Modtable.Entry entry)
        {
            return FindMatchingMod(entry.ModName, entry.ModVersion);
        }
    }
}