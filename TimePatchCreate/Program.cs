using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Providers;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.IO;
using Kore.Factories;
using Kore.Managers;
using plugin_criware.Archives;
using plugin_nintendo.Archives;

namespace TimePatchCreate
{
    class Program
    {
        private const string Welcome_ =
@"
##################################################
# This is the Time Travelers Patch Creator. This #
# tool creates a patch file containing delta-    #
# diffed files from the cpk of the game and the  #
# executable partition.                          #
##################################################
";

        static async Task Main(string[] args)
        {
            // Print welcome text
            Console.WriteLine(Welcome_);

            // Get path arguments
            GetPathArguments(args, out var gamePath, out var patchPath, out var outputPath);

            // Create patch
            await CreatePatch(gamePath, patchPath, outputPath);
        }

        static void GetPathArguments(string[] args, out string gamePath, out string patchPath, out string outputPath)
        {
            // Get cia or 3ds game path
            Console.WriteLine("Enter the path to the .3ds or .cia of Time Travelers:");
            Console.Write("> ");

            gamePath = args.Length > 0 ? args[0] : Console.ReadLine();
            if (args.Length > 0)
                Console.WriteLine(args[0]);
            Console.WriteLine();

            // Get patch path
            Console.WriteLine("Enter the directory, which contains the files to patch:");
            Console.Write("> ");

            patchPath = args.Length > 1 ? args[1] : Console.ReadLine();
            if (args.Length > 1)
                Console.WriteLine(args[1]);
            Console.WriteLine();

            // Get output path
            Console.WriteLine("Enter the file path for the created patch:");
            Console.Write("> ");

            outputPath = args.Length > 2 ? args[2] : Console.ReadLine();
            if (args.Length > 2)
                Console.WriteLine(args[2]);
            Console.WriteLine();
        }

        static async Task CreatePatch(string gamePath, string patchPath, string outputPath)
        {
            // Try to open game
            var partitions = await LoadGamePartitions(gamePath);
            if (partitions == null)
                return;

            // Try to open GameData.cxi
            IArchiveFileInfo gameDataFile = partitions.FirstOrDefault(x => x.FilePath == "/GameData.cxi");
            if (gameDataFile == null)
            {
                Console.WriteLine($"Could not find GameData.cxi in \"{gamePath}\".");
                return;
            }

            Stream gameDataFileStream = await gameDataFile.GetFileData();

            if (!TryLoadGameFiles(gameDataFileStream, gamePath, out IList<IArchiveFileInfo> gameFiles))
                return;

            // Try to open tt1_ctr.cpk
            IArchiveFileInfo cpkArchiveFile = gameFiles.FirstOrDefault(x => x.FilePath == "/RomFs/tt1_ctr.cpk");
            if (cpkArchiveFile == null)
            {
                Console.WriteLine($"Could not find tt1_ctr.cpk in \"{gamePath}\".");
                return;
            }

            Stream cpkArchiveFileStream = await cpkArchiveFile.GetFileData();

            if (!TryLoadCpkFiles(cpkArchiveFileStream, gamePath, out IList<IArchiveFileInfo> cpkFiles))
                return;

            // Create VCDiff's
            Console.Write("Create VCDiff patches...");

            var patchFile = PatchFile.Create(outputPath);
            string cpkFilePath = Path.Combine(patchPath, "tt1_ctr");
            if (Directory.Exists(cpkFilePath))
            {
                foreach (string file in Directory.EnumerateFiles(cpkFilePath, "*", SearchOption.AllDirectories))
                {
                    UPath relativePath = ((UPath)Path.GetRelativePath(cpkFilePath, file)).ToAbsolute();

                    IArchiveFileInfo cpkFile = cpkFiles.FirstOrDefault(x => x.FilePath == relativePath);
                    if (cpkFile == null)
                        continue;

                    // Encode diff
                    await using Stream source = await cpkFile.GetFileData();
                    await using Stream target = File.OpenRead(file);
                    using var output = new MemoryStream();

                    var coder = new VCDiff.Encoders.VcEncoder(source, target, output);
                    await coder.EncodeAsync();

                    // Store diff
                    patchFile.WritePatch(output, relativePath.FullName);
                }
            }

            // Add code.bin diff
            string newCodePath = Path.Combine(patchPath, "code.bin");
            if (File.Exists(newCodePath))
            {
                IArchiveFileInfo codeFile = gameFiles.FirstOrDefault(x => x.FilePath == "/ExeFs/.code");
                if (codeFile != null)
                {
                    // Encode diff
                    await using Stream source1 = await codeFile.GetFileData();
                    await using Stream target1 = File.OpenRead(newCodePath);
                    using var output1 = new MemoryStream();

                    var coder1 = new VCDiff.Encoders.VcEncoder(source1, target1, output1);
                    await coder1.EncodeAsync();

                    // Store diff
                    patchFile.WritePatch(output1, ".code");
                }
            }

            // Add exheader.bin diff
            string newExHeaderPath = Path.Combine(patchPath, "exheader.bin");
            if (File.Exists(newExHeaderPath))
            {
                IArchiveFileInfo codeFile = gameFiles.FirstOrDefault(x => x.FilePath == "/ExHeader.bin");
                if (codeFile != null)
                {
                    // Encode diff
                    await using Stream source1 = await codeFile.GetFileData();
                    await using Stream target1 = File.OpenRead(newExHeaderPath);
                    using var output1 = new MemoryStream();

                    var coder1 = new VCDiff.Encoders.VcEncoder(source1, target1, output1);
                    await coder1.EncodeAsync();

                    // Store diff
                    patchFile.WritePatch(output1, "exheader.bin");
                }
            }

            // Save patch file
            patchFile.Persist();
            Console.WriteLine(" Done");
        }

        #region Cpk Data

        private static bool TryLoadCpkFiles(Stream cpkData, string gamePath, out IList<IArchiveFileInfo> files)
        {
            files = null;

            try
            {
                var cpk = new Cpk();
                files = cpk.Load(cpkData);

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not load tt1_ctr.cpk from \"{gamePath}\". Error: {e.Message}");
                return false;
            }
        }

        #endregion

        #region Game Data

        private static bool TryLoadGameFiles(Stream gameData, string gamePath, out IList<IArchiveFileInfo> files)
        {
            files = null;

            try
            {
                var ncch = new NCCH();
                files = ncch.Load(gameData);

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not load GameData.cxi from \"{gamePath}\". Error: {e.Message}");
                return false;
            }
        }

        #endregion

        #region Game Card

        static async Task<IList<IArchiveFileInfo>> LoadGamePartitions(string gamePath)
        {
            // Check if file exists
            if (!File.Exists(gamePath))
            {
                Console.WriteLine($"The file \"{gamePath}\" does not exist.");
                return null;
            }

            // Check if the file can be opened as readable
            FileStream fileStream;
            try
            {
                fileStream = File.OpenRead(gamePath);
            }
            catch (Exception)
            {
                Console.WriteLine($"The file \"{gamePath}\" can not be opened. Is it open in another program?");
                return null;
            }

            bool isNcsd = await IsNcsd(gamePath);

            if (!TryLoadGamePartitions(fileStream, isNcsd, out IList<IArchiveFileInfo> files))
                return null;

            return files;
        }

        private static async Task<bool> IsNcsd(string gamePath)
        {
            IStreamManager streamManager = new StreamManager();

            using IFileSystem fileSystem = FileSystemFactory.CreatePhysicalFileSystem(streamManager);
            gamePath = (string)fileSystem.ConvertPathFromInternal(gamePath);

            ITemporaryStreamProvider temporaryStreamProvider = streamManager.CreateTemporaryStreamProvider();

            var ncsdPlugin = new NcsdPlugin();
            var identifyContext = new IdentifyContext(temporaryStreamProvider);

            bool isNcsd = await ncsdPlugin.IdentifyAsync(fileSystem, gamePath, identifyContext);

            streamManager.ReleaseAll();

            return isNcsd;
        }

        private static bool TryLoadGamePartitions(FileStream fileStream, bool isNcsd, out IList<IArchiveFileInfo> files)
        {
            files = null;

            try
            {
                if (isNcsd)
                {
                    var ncsd = new NCSD();
                    files = ncsd.Load(fileStream);

                    return true;
                }

                var cia = new CIA();
                files = cia.Load(fileStream);

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not load file \"{fileStream.Name}\". Error: {e.Message}");
                Console.WriteLine("Possible reasons could be that the file is not a .3ds or .cia, or is not decrypted.");

                throw e;
            }
        }

        #endregion
    }
}
