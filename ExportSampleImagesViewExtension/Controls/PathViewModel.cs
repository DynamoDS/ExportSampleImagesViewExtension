using System.IO;
using System.Windows;
using System.Windows.Forms;
using Dynamo.Core;
using Dynamo.UI;
using Dynamo.UI.Commands;

namespace ExportSampleImages.Controls
{
    public enum PathType
    {
        Source,
        Target
    }

    public class PathViewModel : NotificationObject
    {
        private string folderPath;

        public PathViewModel()
        {
            UpdatePathCommand = new DelegateCommand(UpdatePath);
        }

        internal Window Owner { get; set; }

        /// <summary>
        ///     The purpose of the folder path
        /// </summary>
        public PathType Type { get; set; }

        /// <summary>
        ///     The selected path associated with this control
        /// </summary>
        public string FolderPath
        {
            get => folderPath;
            set
            {
                if (value != folderPath)
                {
                    folderPath = value;
                    RaisePropertyChanged(nameof(FolderPath));
                }
            }
        }


        /// <summary>
        ///     Handles path update call
        /// </summary>
        public DelegateCommand UpdatePathCommand { get; set; }

        private void UpdatePath(object obj)
        {
            var folder = FolderPath;
            var dialog = new DynamoFolderBrowserDialog
            {
                SelectedPath = Directory.Exists(folder) ? folder : string.Empty,
                Owner = Window.GetWindow(Owner)
            };

            if (dialog.ShowDialog() == DialogResult.OK) folder = dialog.SelectedPath;
            if (string.IsNullOrEmpty(folder))
                return;

            FolderPath = folder;
        }
    }
}