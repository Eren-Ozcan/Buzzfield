using System;
using System.IO;

namespace Buzzfield.Core
{
    /// <summary>Which file a load came from.</summary>
    public enum SaveSource
    {
        /// <summary>No save file exists: a new game.</summary>
        None,
        Main,
        /// <summary>The main file was missing or unreadable; the backup was used.</summary>
        Backup,
        /// <summary>Files exist but neither could be read: a new game.</summary>
        Unreadable,
    }

    /// <summary>
    /// Save file with atomic replace and one backup. A write goes to a temp file first and
    /// then replaces the main file, whose previous content becomes the backup, so a crash
    /// mid-write never leaves the game without a readable save.
    /// </summary>
    public sealed class SaveFileStore
    {
        public SaveFileStore(string directory, string fileName)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentException("Save directory is empty.", nameof(directory));
            Directory = directory;
            MainPath = Path.Combine(directory, fileName);
            BackupPath = MainPath + ".bak";
            TempPath = MainPath + ".tmp";
        }

        public string Directory { get; }
        public string MainPath { get; }
        public string BackupPath { get; }
        public string TempPath { get; }

        public void Write(string text)
        {
            System.IO.Directory.CreateDirectory(Directory);
            using (var stream = new FileStream(TempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(text);
                writer.Flush();
                stream.Flush(true);
            }

            if (!File.Exists(MainPath))
            {
                File.Move(TempPath, MainPath);
                return;
            }
            try
            {
                File.Replace(TempPath, MainPath, BackupPath, true);
            }
            catch (Exception e) when (e is PlatformNotSupportedException || e is IOException || e is UnauthorizedAccessException)
            {
                // Some file systems have no atomic replace; keep the same order by hand.
                File.Copy(MainPath, BackupPath, true);
                File.Delete(MainPath);
                File.Move(TempPath, MainPath);
            }
        }

        /// <summary>
        /// Reads the main file, then the backup. <paramref name="accept"/> parses the text and
        /// returns false when it is unusable, which moves on to the next file.
        /// </summary>
        public SaveSource Read(Func<string, bool> accept)
        {
            bool anyFile = false;
            if (TryRead(MainPath, accept, ref anyFile))
                return SaveSource.Main;
            if (TryRead(BackupPath, accept, ref anyFile))
                return SaveSource.Backup;
            return anyFile ? SaveSource.Unreadable : SaveSource.None;
        }

        public void Delete()
        {
            DeleteIfExists(MainPath);
            DeleteIfExists(BackupPath);
            DeleteIfExists(TempPath);
        }

        static bool TryRead(string path, Func<string, bool> accept, ref bool anyFile)
        {
            if (!File.Exists(path))
                return false;
            anyFile = true;
            string text;
            try
            {
                text = File.ReadAllText(path);
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            return accept(text);
        }

        static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
