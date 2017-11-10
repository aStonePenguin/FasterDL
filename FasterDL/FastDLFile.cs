using System;
using System.IO;
using System.Diagnostics;
using ICSharpCode.SharpZipLib.BZip2;

namespace FasterDL
{
    class FastDLFile
    {
        public FileInfo Info { get; internal set; }
        public string Folder { get; internal set; }
        
        private string _BaseFolder;
        public string BaseFolder {
            get
            {
                
                return _BaseFolder;
            }
            set
            {
                _BaseFolder = value.Replace(@"\", "/");

                if (BaseFolder.EndsWith("/"))
                    _BaseFolder = BaseFolder.Substring(0, BaseFolder.Length - 1);
            }
        }

        private string _AbsolutePath;
        public string AbsolutePath
        {
            get
            {
                return _AbsolutePath;
            }
            set
            {
                _AbsolutePath = value.Replace(@"\", "/");

                Info = new FileInfo(AbsolutePath);
                Folder = AbsolutePath.Substring(BaseFolder.Length + 1);
                Folder = Folder.Substring(0, Math.Max(0, Folder.Length - Info.Name.Length - 1));
                OutputInfo = new FileInfo(OutputCompressedAbsolutePath);
            }
        }

        public string Resource 
        {
            get
            {
                return $"{Folder}/{Info.Name}";
            }
        }

        public bool IsUseless {
            get
            {
                switch (Info.Extension.ToLower())
                {
                    case ".bsp":
                    case ".ain":
                    case ".vmt":
                    case ".vtf":
                    case ".png":
                    case ".vtx":
                    case ".mdl":
                    case ".phy":
                    case ".vvd":
                    case ".mp3":
                    case ".wav":
                    case ".ogg":
                    case ".pcf":
                    case ".ttf":
                        return (Info.Name.EndsWith(".xbox.vtx") || Info.Name.EndsWith(".sw.vtx"));
                }

                return true;
            }
        }

        public FileInfo OutputInfo { get; internal set; }

        public string OutputName
        {
            get
            {
                return $"{Info.Name}.bz2";
            }
        }

        public string OutputBaseFolder
        {
            get
            {
                return $"{BaseFolder}_fasterdl_output";
            }
        }

        public string OutputCopyAbsolutePath
        {
            get
            {
                return $"{OutputBaseFolder}/{Folder}/{Info.Name}";
            }
        }

        public string OutputCompressedAbsolutePath
        {
            get
            {
                return $"{OutputBaseFolder}/{Folder}/{OutputName}";
            }
        }

        private void Copy()
            => Info.CopyTo(OutputCopyAbsolutePath);

        private void CompressInternal()
        {
            using (FileStream OriginFile = Info.OpenRead())
            using (FileStream CompressedFile = OutputInfo.Create())
            using (BZip2OutputStream BzipOutput = new BZip2OutputStream(CompressedFile, 9))
                ICSharpCode.SharpZipLib.Core.StreamUtils.Copy(OriginFile, BzipOutput, new byte[4096]);
        }

        private void Compress7zip()
        {
            using (Process proc = new Process())
            {
                proc.StartInfo = new ProcessStartInfo
                {
                    FileName = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\7-Zip\7z.exe",
                    Arguments = $"a -tbzip2 -mx9 \"{OutputCompressedAbsolutePath}\" \"{OutputCopyAbsolutePath}\" -mmt=off",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                proc.Start();
                proc.WaitForExit();
            }
        }

        public void Run(bool use7zip = false)
        {
            if (!OutputInfo.Directory.Exists)
                OutputInfo.Directory.Create();

            Copy();

            if (use7zip)
                Compress7zip();
            else
                CompressInternal();
        }
    }
}
