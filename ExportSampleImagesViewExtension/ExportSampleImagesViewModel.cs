using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Dynamo.Controls;
using Dynamo.Core;
using Dynamo.Extensions;
using Dynamo.Graph.Workspaces;
using Dynamo.Models;
using Dynamo.UI.Commands;
using Dynamo.ViewModels;
using Dynamo.Views;
using Dynamo.Wpf.Extensions;

namespace ExportSampleImagesViewExtension
{
    public class ExportSampleImagesViewModel : NotificationObject, IDisposable
    {
        #region Fields and Properties
        private readonly ViewLoadedParams viewLoadedParamsInstance;

        //private readonly Window dynamoWindow;
        internal DynamoViewModel DynamoViewModel;
        internal WorkspaceModel CurrentWorkspace;

        private readonly ViewModelCommandExecutive ViewModelExecutive;
        private readonly ICommandExecutive CommandExecutive;
        private readonly ViewModelCommandExecutive ViewModelCommandExecutive;

        private string sourcePath;
        private string targetPath;

        public string SourcePath
        {
            get { return sourcePath; }
            set
            {
                if (value != sourcePath)
                {
                    sourcePath = value;
                    RaisePropertyChanged(nameof(SourcePath));
                }
            }
        }

        public string TargetPath
        {
            get { return targetPath; }
            set
            {
                if (value != targetPath)
                {
                    targetPath = value;
                    RaisePropertyChanged(nameof(TargetPath));
                }
            }
        }

        public DelegateCommand SourceFolderCommand { get; set; }
        public DelegateCommand TargetFolderCommand { get; set; }
        public DelegateCommand ExportGraphsCommand { get; set; }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p"></param>
        public ExportSampleImagesViewModel(ViewLoadedParams p)
        {
            if (p == null) return;

            this.viewLoadedParamsInstance = p;
            this.viewLoadedParamsInstance.CurrentWorkspaceOpened += OnCurrentWorkspaceOpened;

            if (this.viewLoadedParamsInstance.CurrentWorkspaceModel is HomeWorkspaceModel)
            {
                this.CurrentWorkspace = this.viewLoadedParamsInstance.CurrentWorkspaceModel as HomeWorkspaceModel;
                this.DynamoViewModel = this.viewLoadedParamsInstance.DynamoWindow.DataContext as DynamoViewModel;

                this.ViewModelExecutive = p.ViewModelCommandExecutive;
                this.CommandExecutive = p.CommandExecutive;
                this.ViewModelCommandExecutive = p.ViewModelCommandExecutive;
            }

            SourceFolderCommand = new DelegateCommand(SetSourceFolder);
            TargetFolderCommand = new DelegateCommand(SetTargetFolder);
            ExportGraphsCommand = new DelegateCommand(ExportGraphs);
        }

        private void OnCurrentWorkspaceOpened(IWorkspaceModel workspace)
        {
            if (workspace as HomeWorkspaceModel == null) return;

            this.CurrentWorkspace = workspace as HomeWorkspaceModel;
            this.DynamoViewModel = this.viewLoadedParamsInstance.DynamoWindow.DataContext as DynamoViewModel;
        }


        private void SetSourceFolder(object obj)
        {
            string folder = Utilities.Utilities.GetFolderDialog();

            if (string.IsNullOrEmpty(folder))
                return;
            if (CurrentWorkspace == null)
                return;

            SourcePath = folder;
        }

        private void SetTargetFolder(object obj)
        {
            string folder = Utilities.Utilities.GetFolderDialog();

            if (string.IsNullOrEmpty(folder))
                return;
            if (CurrentWorkspace == null)
                return;

            TargetPath = folder;
        }


        /// <summary>
        /// Export all the graphs we have found in the destination folder
        /// </summary>
        /// <param name="obj"></param>
        private void ExportGraphs(object obj)
        {
            if (string.IsNullOrEmpty(SourcePath) || string.IsNullOrEmpty(TargetPath)) 
                return;

            var files = Utilities.Utilities.GetAllFilesOfExtension(SourcePath);
            if (files == null)
                return;

            foreach (var file in files)
            {
                // 1 Open a graph
                OpenDynamoGraph(file);
                // 2 Prepare the graph for exporting 
                CleanUpGraph();
                // 3 Export an image
                SaveGraphToImage(file);
            }
        }

        private void CleanUpGraph()
        {
            this.DynamoViewModel.Model.ForceRun();
            this.ViewModelExecutive.FitViewCommand();
        }

        private void SaveGraphToImage(string fileName)
        {
            var path = Path.Combine(TargetPath, System.IO.Path.GetFileNameWithoutExtension(fileName) + ".png");
            
            this.DynamoViewModel.SaveImage(path); 
        }

        // TODO: Try catch? How to capture if the opening was successful?
        private void OpenDynamoGraph(string path)
        {
            this.DynamoViewModel.OpenCommand.Execute(path);
        }


        public void Dispose()
        {
            viewLoadedParamsInstance.CurrentWorkspaceChanged -= OnCurrentWorkspaceOpened;
        }
    }
}
