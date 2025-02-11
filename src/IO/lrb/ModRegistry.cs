using linerider.IO.lrb.BuiltinMods;
using System.Collections.Generic;

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
        private static ModRegistryEntry preferredSimlineMod;
        public static ModRegistryEntry SimlineMod
        {
            get
            {
                check_init();
                return preferredSimlineMod;
            }
        }

        private static ModRegistryEntry preferredLabelMod;
        public static ModRegistryEntry LabelMod
        {
            get
            {
                check_init();
                return preferredLabelMod;
            }
        }

        private static bool inited = false;

        static ModRegistryEntry register(ModRegistryEntry entry)
        {
            mods.Add(entry);
            return entry;
        }

        public static void Init()
        {
            preferredSimlineMod = register(new base_label());
            preferredLabelMod = register(new base_simline());
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