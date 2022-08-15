using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Dynamo.Core;
using Dynamo.UI;
using Dynamo.UI.Commands;
using DynamoUtilities;

namespace ExportSampleImagesViewExtension.Controls
{
    public class PathViewModel : NotificationObject
    {
        internal Window Owner { get; set; }

        private string folderPath;
        /// <summary>
        /// The selected path associated with this control
        /// </summary>
        public string FolderPath
        {
            get { return folderPath; }
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
        /// Handles path update call
        /// </summary>
        public DelegateCommand UpdatePathCommand { get; set; }
        
        public PathViewModel()
        {
            UpdatePathCommand = new DelegateCommand(UpdatePath);  
        }

        private void UpdatePath(object obj)
        {
            string folder = FolderPath;
            
            var dialog = new DynamoFolderBrowserDialog
            {
                SelectedPath = System.IO.Directory.Exists(folder) ? folder : string.Empty,
                Owner = Window.GetWindow(Owner)
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                folder = dialog.SelectedPath;
            }

            if (string.IsNullOrEmpty(folder))
                return;

            FolderPath = folder;
        }
    }
}
