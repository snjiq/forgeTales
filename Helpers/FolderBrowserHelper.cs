using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace ForgeTales.Helpers
{
    public static class FolderBrowserHelper
    {
        public static string SelectRenPyProjectFolder(Window owner)
        {
            try
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.Description = "Выберите папку с проектом Ren'Py";
                    dialog.ShowNewFolderButton = false;
                    dialog.RootFolder = Environment.SpecialFolder.MyComputer;

                    if (dialog.ShowDialog(new Win32Window(owner)) == System.Windows.Forms.DialogResult.OK)
                    {
                        return dialog.SelectedPath;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка выбора папки: {ex}");
            }
            return null;
        }

        private class Win32Window : System.Windows.Forms.IWin32Window
        {
            private readonly IntPtr _handle;
            public Win32Window(Window window)
            {
                _handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
            }
            public IntPtr Handle => _handle;
        }
    }
}