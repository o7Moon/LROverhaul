using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace linerider.IO.lrb
{
    public class Modtable
    {
        public const byte EXPECTED_LRB_VERSION = 0;
        static readonly byte[] EXPECTED_LRB_MAGIC_NUMBER = new byte[3]{ 0x4C, 0x52, 0x42 };

        byte lrb_version;
        public Entry[] Entries;

        public class Entry
        {
            public readonly string ModName;
            public readonly ushort ModVersion;
            public byte[]? ModData;
            public readonly string? OptionalMessage;

            public Entry(string name, ushort modVersion, byte[]? modData, string? optionalMessage)
            {
                this.ModName = name;
                this.ModVersion = modVersion;
                this.ModData = modData;
                this.OptionalMessage = optionalMessage;
            }
        }

        public Modtable(byte version, Entry[] entries)
        {
            this.lrb_version = version;
            this.Entries = entries;
            return;
        }

        [Flags]
        enum modflags : byte
        {
            optional = 1 << 0,
            physics = 1 << 1,
            camera = 1 << 2,
            scenery = 1 << 3,
            extra_data = 1 << 4,
        }

        public static bool TryParse(Stream file, out Modtable? modtable)
        {
            // value that will be kept if the fn exits early
            // has to be set to satisfy the compiler
            modtable = null;
            try
            {
                var br = new BinaryReader(file);
                byte[] magic_number = br.ReadBytes(3);
                if (!magic_number.SequenceEqual(EXPECTED_LRB_MAGIC_NUMBER))
                {
                    return false;
                }

                byte version = br.ReadByte();
                if (version != EXPECTED_LRB_VERSION)
                {
                    return false;
                }

                ushort mod_count = br.ReadUInt16();

                Entry[] entries = new Entry[mod_count];
                for (int i = 0; i < entries.Length; i++)
                {
                    byte namelen = br.ReadByte();
                    string name = Encoding.UTF8.GetString(br.ReadBytes(namelen));
                    ushort modversion = br.ReadUInt16();
                    modflags flags = (modflags) br.ReadByte();
                    bool includes_data = flags.HasFlag(modflags.extra_data);
                    byte[]? data = null;
                    if (includes_data)
                    {
                        UInt64 data_begin = br.ReadUInt64();
                        UInt64 data_len = br.ReadUInt64();
                        Int64 current_seek = file.Position;
                        file.Position = (long)data_begin;
                        data = br.ReadBytes((int)data_len);
                        file.Position = current_seek;
                    }
                    bool includes_optional = flags.HasFlag(modflags.optional);
                    string? optional_string = null;
                    if (includes_optional)
                    {
                        byte len = br.ReadByte();
                        optional_string = Encoding.UTF8.GetString(br.ReadBytes(len));
                    }

                    entries[i] = new Entry(name, modversion, data, optional_string);
                }
                modtable = new Modtable(version, entries);

                return true;
            }
            catch (IOException _)
            {
                return false;
            }
            catch (InvalidCastException _)// if data_begin exceeds int64 max (probably not but why is it signed xd??? thx microsoft)
                                          // or if data_len exceeds int32 max (this one might actually happen, maybe needs a different solution)
            {
                return false;
            }
        }

        public void Write(Stream file)
        {
            BinaryWriter bw = new BinaryWriter(file);
            bw.Write(EXPECTED_LRB_MAGIC_NUMBER);
            bw.Write(lrb_version);
            bw.Write((ushort)Entries.Length);
            Dictionary<Entry, long> entries_need_data = new Dictionary<Entry, long>();
            foreach (Entry e in Entries)
            {
                bw.Write((byte)e.ModName.Length);
                bw.Write(Encoding.UTF8.GetBytes(e.ModName));
                
                bw.Write(e.ModVersion);
                
                modflags flags = 0;
                if (e.OptionalMessage != null)
                {
                    flags |= modflags.optional;
                }

                if (e.ModData != null)
                {
                    flags |= modflags.extra_data;
                }
                
                bw.Write((byte)flags);
                
                if (e.ModData != null)
                {
                    entries_need_data.Add(e, file.Position);
                    // pad with zero for now
                    bw.Write((UInt64)0);
                    bw.Write((UInt64)0);
                }
                
                if (e.OptionalMessage != null)
                {
                    bw.Write((byte)e.OptionalMessage.Length);
                    bw.Write(Encoding.UTF8.GetBytes(e.OptionalMessage));
                }
            }

            foreach (KeyValuePair<Entry, long> p in entries_need_data)
            {
                long position = file.Position;
                file.Seek(p.Value, SeekOrigin.Begin);
                // seek back to the entry to overwrite the data pointer and length
                bw.Write((UInt64)position);
                bw.Write((UInt64)p.Key.ModData.Length);
                // then go and actually write the data there
                file.Seek(position, SeekOrigin.Begin);
                bw.Write(p.Key.ModData);
            }
            bw.Flush();
        }
    }
}