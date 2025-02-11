namespace linerider.IO.lrb
{
    public abstract class IOMod : ModRegistry.ModRegistryEntry
    {
        /// <summary>
        /// Read info from the track and create an entry to be added to the track file for this mod.
        /// </summary>
        /// <returns>a new <see cref="Modtable.Entry"/> to be written into the track file</returns>
        public abstract Modtable.Entry WriteEntry(Track track);
        /// <summary>
        /// Read info from an entry for this mod and apply the changes to the track.
        /// </summary>
        public abstract void LoadEntry(Modtable.Entry entry, ref Track track);

        public abstract ushort ModVersion
        {
            get;
        }

        public abstract string ModName
        {
            get;
        }

        public IOMod? IoMod { get => this; }

        /// <summary>
        /// Helper function to create a default modtable entry from this object's info and optional extra parameters.
        /// </summary>
        public Modtable.Entry CreateEntry(byte[]? bytes = null, string? optionalmessage = null)
        {
            return new Modtable.Entry(name: ModName, modVersion: ModVersion, modData: bytes, optionalMessage: optionalmessage);
        }
    }
}