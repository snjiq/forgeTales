using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;

namespace ForgeTales.Helpers
{
    public static class RenPyHelper
    {
        public static string DefaultRenPyPath = @"C:\Users\snjiq\OneDrive\Desktop\renpy-8.3.7-sdk\renpy.exe";

        public static bool ValidateRenPyProject(string folderPath)
        {
            try
            {
                string gameFolder = Path.Combine(folderPath, "game");
                return Directory.Exists(gameFolder) &&
                       Directory.GetFiles(gameFolder, "*.rpy", SearchOption.AllDirectories).Length > 0;
            }
            catch
            {
                return false;
            }
        }

        public static string ZipRenPyProject(string projectPath)
        {
            try
            {
                string gameFolder = Path.Combine(projectPath, "game");
                if (!Directory.Exists(gameFolder))
                    throw new DirectoryNotFoundException("Папка 'game' не найдена");

                using (var memoryStream = new MemoryStream())
                {
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                    {
                        foreach (var file in Directory.GetFiles(gameFolder, "*", SearchOption.AllDirectories))
                        {
                            string relativePath = file.Substring(gameFolder.Length + 1);
                            var entry = archive.CreateEntry(relativePath);

                            using (var entryStream = entry.Open())
                            using (var fileStream = File.OpenRead(file))
                            {
                                fileStream.CopyTo(entryStream);
                            }
                        }
                    }
                    return Convert.ToBase64String(memoryStream.ToArray());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка архивации: {ex.Message}");
                throw;
            }
        }

        public static void LaunchRenPyProject(string projectFolder)
        {
            try
            {
                if (!File.Exists(DefaultRenPyPath))
                    throw new FileNotFoundException("Ren'Py не найден по указанному пути");

                Process.Start(new ProcessStartInfo
                {
                    FileName = DefaultRenPyPath,
                    Arguments = $"\"{projectFolder}\"",
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(DefaultRenPyPath)
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка запуска: {ex.Message}");
                throw;
            }
        }
    }
}