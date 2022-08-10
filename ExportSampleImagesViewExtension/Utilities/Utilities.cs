using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoreNodeModels.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Shell;
using Directory = CoreNodeModels.Input.Directory;

namespace ExportSampleImagesViewExtension.Utilities
{
    public static class Utilities
    {
        /// <summary>
        /// Returns a folder path
        /// Source: https://stackoverflow.com/questions/8142109/how-can-i-make-commonopenfiledialog-select-folders-only-but-still-show-files/20102239#20102239
        /// </summary>
        /// <returns></returns>
        public static string GetFolderDialog()
        {
            var openFolder = new CommonOpenFileDialog();

            openFolder.AllowNonFileSystemItems = true;
            openFolder.Multiselect = true;
            openFolder.IsFolderPicker = true;
            openFolder.Title = "Select destination folder.";

            if (openFolder.ShowDialog() != CommonFileDialogResult.Ok)
            {
                MessageBox.Show("No Folder selected");
                return null;
            }
            
            return openFolder.FileName;
        }

        /// <summary>
        /// Returns a list of files of given path and extension
        /// </summary>
        /// <param name="path"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetAllFilesOfExtension(string path, string extension = ".dyn")
        {
            var files = System.IO.Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                .Where(x => extension.Equals(Path.GetExtension(x).ToLowerInvariant()));

            return files;
        }

        public static string GetFolder()
        {
            string folder = "";

            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrEmpty(fbd.SelectedPath))
                {
                    folder = fbd.SelectedPath;
                }
            }

            return folder;
        }
    }
}
