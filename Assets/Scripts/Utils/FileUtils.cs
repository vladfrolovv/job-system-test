#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

#endregion

namespace Utils
{
    public static class FileUtils
    {
        public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo self, string[] includes, string[] excludes = null)
        {
            foreach (FileSystemInfo fileSystemInfo in EnumerateFileSystemInfos(self, includes, excludes))
            {
                if (fileSystemInfo is FileInfo file)
                {
                    yield return file;
                }
            }
        }


        public static IEnumerable<string> EnumerateParentDirectories(this string path)
        {
            while (!string.IsNullOrEmpty(path))
            {
                path = Path.GetDirectoryName(path);
                yield return path;
            }
        }


        public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(this DirectoryInfo self, string[] includes, string[] excludes = null)
        {
            if (includes.Length == 0)
            {
                yield break;
            }

            if (excludes == null)
            {
                excludes = Array.Empty<string>();
            }

            Regex includesRegex = new Regex(string.Join("|", includes));
            Regex excludesRegex = new Regex(string.Join("|", excludes));

            foreach (FileSystemInfo file in self.EnumerateFileSystemInfos("*.*", SearchOption.AllDirectories))
            {
                string relativePath = file.GetRelativePath(self).Replace("\\", "/");

                if (includesRegex.IsMatch(relativePath) &&
                    (excludes.Length == 0 || !excludesRegex.IsMatch(relativePath)))
                {
                    yield return file;
                }
            }
        }


        public static string GetRelativePath(this FileSystemInfo self, DirectoryInfo dir)
        {
            return GetRelativePath(self.FullName, dir.FullName);
        }


        public static string GetRelativePath(string fileName, string dir)
        {
            if (string.IsNullOrEmpty(dir))
            {
                return fileName;
            }

            // Very naive implementation, will work only for simple cases.
            string directoryPath = dir + Path.DirectorySeparatorChar;
            return fileName.Remove(0, directoryPath.Length);
        }


        public static void CopyFile(string sourcePath, string targetPath, bool overwrite = false)
        {
            CreateContainingDirectory(targetPath);
            File.Copy(sourcePath, targetPath, overwrite);
        }


        public static void CopyFiles(this DirectoryInfo self, string dstDirPath, string[] includes, string[] excludes = null)
        {
            foreach (FileInfo file in self.EnumerateFiles(includes, excludes))
            {
                string fileName = file.GetRelativePath(self);
                string destPath = Path.Combine(dstDirPath, fileName);
                CopyFile(file.FullName, destPath);
            }
        }


        public static void CreateContainingDirectory(string targetPath)
        {
            string directoryPath = Path.GetDirectoryName(targetPath);

            if (directoryPath != null &&
                !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }


        public static void DeleteFiles(this DirectoryInfo self, string[] includes, string[] excludes = null)
        {
            foreach (FileSystemInfo file in self.EnumerateFileSystemInfos(includes, excludes).Reverse())
            {
                DirectoryInfo dir = file as DirectoryInfo;
                if (dir == null || dir.GetFileSystemInfos().Length == 0)
                {
                    file.Delete();
                }
            }
        }


        public static string ReadText(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            {
                using (StreamReader streamReader = new StreamReader(stream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }


        public static void WriteText(string path, string text)
        {
            using (FileStream stream = File.Create(path))
            {
                using (StreamWriter streamWriter = new StreamWriter(stream))
                {
                    streamWriter.Write(text);
                }
            }
        }


        /// <summary>
        ///     More robust analogue of <see cref="Path.GetTempFileName" />. Note that temporary file will not
        ///     be created automatically, function just returns random temporary file name.
        /// </summary>
        /// <param name="extension">File extension (in some cases random file should have specific extension)</param>
        /// <returns>Unique random name for a temporary file.</returns>
        public static string GetTempFileName(string extension = "tmp")
        {
            int attemptCount = 10;
            while (true)
            {
                string fileName = Path.GetRandomFileName();
                fileName = Path.ChangeExtension(fileName, extension);
                fileName = Path.Combine(Path.GetTempPath(), fileName);

                if (!File.Exists(fileName))
                {
                    return fileName;
                }

                if (--attemptCount == 0)
                {
                    throw new IOException("No unique temporary file name is available.");
                }
            }
        }


        public static void RecreateDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                DirectoryInfo directory = new DirectoryInfo(path);

                foreach (FileInfo file in directory.EnumerateFiles())
                {
                    file.Delete();
                }

                foreach (DirectoryInfo dir in directory.EnumerateDirectories())
                {
                    dir.Delete(true);
                }
            }
            else
            {
                Directory.CreateDirectory(path);
            }
        }


        public static string ToSlashPath(this string path)
        {
            return path.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
        }
    }
}
