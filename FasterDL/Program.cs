using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FasterDL
{
    class Program
    {
        private static bool Use7zip = File.Exists($@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\7-Zip\7z.exe");
        private static string Folder;
        private static int TotalFiles;
        private static int ValidFiles = 0;
        private static int FinishedFiles = 0;
        private static long FileSize = 0;
        private static long OutputFileSize = 0;
        private static List<string> Resources = new List<string> { };

        private static Thread ProgressThread = new Thread(DrawProgress);

        private static void DrawProgress()
        {
            while (true)
            {
                Thread.Sleep(100);

                Console.CursorLeft = 0;
                Console.CursorTop = 0;

                Console.Write($"Running... ");
                Console.CursorLeft = 11;
                Console.Write("[");
                Console.CursorLeft = 12;

                double perc = ((double)FinishedFiles / TotalFiles);
                int finished = Convert.ToInt32(50 * perc);

                Console.ForegroundColor = ConsoleColor.Black;
                for (int i = 0; i < 50; i++)
                {
                    Console.CursorLeft = 13 + i;
                    Console.BackgroundColor = i <= finished ? ConsoleColor.Green : ConsoleColor.Gray;
                    Console.Write(i <= finished ? "#" : "-");
                }

                Console.CursorLeft = 64;
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Write($"] - {FinishedFiles}/{TotalFiles} - {Convert.ToInt32(perc * 100)}%");
                
                if (perc >= 1)
                    ProgressThread.Abort();
            }
        }

        private static void HandleFile(string absolutepath)
        {
            FastDLFile file = new FastDLFile
            {
                BaseFolder = Folder,
                AbsolutePath = absolutepath
            };

            if (!file.IsUseless)
            {
                file.Run(Use7zip);

                if (file.Info.Extension != "bsp")
                    lock (Resources)
                        Resources.Add(file.Resource);

                Interlocked.Increment(ref ValidFiles);
                Interlocked.Add(ref FileSize, file.Info.Length);
                Interlocked.Add(ref OutputFileSize, file.OutputInfo.Length);
            }

            Interlocked.Increment(ref FinishedFiles);
        }

        private static bool IsAddonFormat(string dir)
        {
            switch (dir)
            {
                case "maps":
                case "materials":
                case "models":
                case "particles":
                case "resource":
                case "sound":
                    return true;
            }

            return false;
        }

        static void Main(string[] args)
        {
            Console.Clear(); 
            Console.Title = $"FasterDL{(Use7zip ? " - 7zip" : "")}";

            if (args.Length < 1)
            {
                Console.WriteLine("Invalid usage! Drag and drop a folder onto the exe or run: FasterDL.exe <absolute folder path> <optional create resource file 0 or 1>");
                Console.ReadLine();
                return;
            }

            Folder = args[0].Replace("\"", "").Replace("/", @"\");
            if (!Directory.Exists(Folder))
            {
                Console.WriteLine($"Invalid Directory: {Folder}");
                Console.ReadLine();
                return;
            }

            string outputfolder = $"{Folder}_fasterdl_output";
            if (Directory.Exists(outputfolder))
            {
                Console.WriteLine($"Output folder already exists!: {outputfolder}");
                Console.WriteLine($"Would you like to delete it? [y/n]");

                if (Console.ReadLine().ToLower() == "y")
                    Directory.Delete(outputfolder, true);
                else
                    Environment.Exit(0);

                Console.Clear();
            }

            IEnumerable<string> files = Directory.EnumerateFiles(Folder, "*.*", SearchOption.AllDirectories);
            TotalFiles = files.Count();

            ProgressThread.Start(); // Progress is important ok

            Parallel.ForEach(files, new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) // Leave one core free
            }, HandleFile);

            bool enableresource = args.Length == 2 ? args[1] == "1" : args.Length == 1;
            bool createresource = false;

            if (enableresource)
                foreach (string v in Directory.GetDirectories(Folder))
                    if (IsAddonFormat(v.Substring(v.LastIndexOf(@"\") + 1)))
                    {
                        createresource = true;
                        break;
                    }

            if (enableresource && !createresource)
                Console.WriteLine("\n\nFailed to create resource file because the folder was not in addon format!");

            if (createresource)
            {
                string luafolder = $"{outputfolder}/lua/autorun/server";
                Directory.CreateDirectory(luafolder);

                using (StreamWriter ResourceFile = new StreamWriter($"{luafolder}/fasterdl {DateTime.Now.ToString("yyyy_dd_M HH_mm_ss")}.lua"))
                    foreach (string v in Resources)
                        ResourceFile.WriteLine($"resource.AddSingleFile'{v}'");
            }

            Console.WriteLine("\n\nComplete:");
            Console.WriteLine($"   Compressed files: {ValidFiles}/{FinishedFiles}");
            Console.WriteLine($"   Useless files: {FinishedFiles - ValidFiles}");
            Console.WriteLine($"   Uncompressed Size: {FileSize / 1024 / 1024}mb");
            Console.WriteLine($"   Compressed Size: {OutputFileSize / 1024 / 1024}mb");
            Console.WriteLine($"   Total Saved: {(FileSize - OutputFileSize) / 1024 / 1024}mb");

            Console.WriteLine("\n\nPress enter to exit...");

            Console.ReadLine();
        }
    }
}
