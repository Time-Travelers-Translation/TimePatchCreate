using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Komponent.IO;

namespace TimePatchCreate
{
    class PatchFile
    {
        private IList<PatchEntry> _entries = new List<PatchEntry>();
        private IList<string> _paths = new List<string>();
        private int _currentOffset = 0x10;
        private Stream _output;

        private PatchFile(Stream output)
        {
            _output = output;
        }

        public static PatchFile Create(string path)
        {
            return new PatchFile(File.Create(path));
        }

        public void WritePatch(Stream input, string path)
        {
            // Compress diff with ZLib
            using var zlibOutput = new MemoryStream();
            using var zlib = new DeflateStream(zlibOutput, CompressionMode.Compress);

            input.Position = 0;
            input.CopyTo(zlib);

            zlib.Flush();

            // Write compressed VCDiff to patch file
            _output.Position = _currentOffset;
            zlibOutput.Position = 0;
            zlibOutput.CopyTo(_output);

            // Add entry for patch
            _entries.Add(new PatchEntry { offset = _currentOffset, length = (int)zlibOutput.Length });
            _paths.Add(path);
            _currentOffset += (int)zlibOutput.Length;
        }

        public void Persist()
        {
            using var bw = new BinaryWriterX(_output);

            // Write file table
            var tableOffset = _currentOffset;
            _output.Position = tableOffset;

            foreach (PatchEntry entry in _entries)
            {
                bw.Write(entry.offset);
                bw.Write(entry.length);
            }

            // Write string table
            var stringOffset = (int)_output.Position;
            foreach (var path in _paths)
                bw.WriteString(path, Encoding.ASCII, false);

            // Write header information
            _output.Position = 0;

            bw.WriteString("DIFF", Encoding.ASCII, false, false);
            bw.Write(tableOffset);
            bw.Write(stringOffset);
            bw.Write(_entries.Count);
        }
    }

    struct PatchEntry
    {
        public int offset;
        public int length;
    }
}
