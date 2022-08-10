using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
        internal HomeWorkspaceModel CurrentWorkspace;

        private readonly ViewModelCommandExecutive ViewModelExecutive;
        private readonly ICommandExecutive CommandExecutive;
        private readonly ViewModelCommandExecutive ViewModelCommandExecutive;

        private string sourcePath;
        private string targetPath;
        private string fileName;

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

        public string FileName
        {
            get { return fileName; }
            set
            {
                if (value != fileName)
                {
                    fileName = value;
                    RaisePropertyChanged(nameof(FileName));
                }
            }
        }

        public DelegateCommand SourceFolderCommand { get; set; }
        public DelegateCommand TargetFolderCommand { get; set; }
        public DelegateCommand ExportGraphsCommand { get; set; }
        public DelegateCommand OpenGraphsCommand { get; set; }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p"></param>
        public ExportSampleImagesViewModel(ViewLoadedParams p)
        {
            if (p == null) return;

            this.viewLoadedParamsInstance = p;

            p.CurrentWorkspaceChanged += OnCurrentWorkspaceChanged;
            p.CurrentWorkspaceCleared += OnCurrentWorkspaceCleared;

            if (this.viewLoadedParamsInstance.CurrentWorkspaceModel is HomeWorkspaceModel)
            {
                this.CurrentWorkspace = this.viewLoadedParamsInstance.CurrentWorkspaceModel as HomeWorkspaceModel;
                this.DynamoViewModel = this.viewLoadedParamsInstance.DynamoWindow.DataContext as DynamoViewModel;

                this.CurrentWorkspace.EvaluationCompleted += CurrentWorkspaceOnEvaluationCompleted;

                this.ViewModelExecutive = p.ViewModelCommandExecutive;
                this.CommandExecutive = p.CommandExecutive;
                this.ViewModelCommandExecutive = p.ViewModelCommandExecutive;
            }

            SourceFolderCommand = new DelegateCommand(SetSourceFolder);
            TargetFolderCommand = new DelegateCommand(SetTargetFolder);
            ExportGraphsCommand = new DelegateCommand(ExportGraphs);
            OpenGraphsCommand = new DelegateCommand(OpenGraphs);
        }

        private void OnCurrentWorkspaceCleared(IWorkspaceModel workspace)
        {
            CurrentWorkspace = this.viewLoadedParamsInstance.CurrentWorkspaceModel as HomeWorkspaceModel;

        }

        private void OnCurrentWorkspaceChanged(IWorkspaceModel workspace)
        {
            CurrentWorkspace.EvaluationCompleted -= CurrentWorkspaceOnEvaluationCompleted;

            CurrentWorkspace = workspace as HomeWorkspaceModel;
            CurrentWorkspace.EvaluationCompleted += CurrentWorkspaceOnEvaluationCompleted;
        }

        private void CurrentWorkspaceOnEvaluationCompleted(object sender, EvaluationCompletedEventArgs e)
        {
            FileName += " Rendered!";
        }
        
        private void OnCurrentWorkspaceOpened(IWorkspaceModel workspace)
        {
            if (workspace as HomeWorkspaceModel == null) return;

            this.CurrentWorkspace = workspace as HomeWorkspaceModel;
            this.DynamoViewModel = this.viewLoadedParamsInstance.DynamoWindow.DataContext as DynamoViewModel;

            if (this.DynamoViewModel == null) return;
            this.DynamoViewModel.Model.WorkspaceOpened += WorkspaceOpened;
        }

        #region Commands
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
        #endregion

        private void OpenGraphs(object obj)
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
            }
        }

        // TODO: Try catch? How to capture if the opening was successful?
        private void OpenDynamoGraph(string path)
        {
            DynamoViewModel.OpenCommand.Execute(path);
        }

        /// <summary>
        /// Export all the graphs we have found in the destination folder
        /// </summary>
        /// <param name="obj"></param>
        private void ExportGraphs(object obj)
        {
            // 3 Export an image
            SaveGraphToImage();
        }
        
        private void SaveGraphToImage()
        {
            if (string.IsNullOrEmpty(TargetPath) || string.IsNullOrEmpty(FileName)) 
                return;

            var path = Path.Combine(TargetPath, FileName + ".png");
            
            this.DynamoViewModel.SaveImage(path); 
        }

        /// <summary>
        /// Triggers when the graph view opens
        /// </summary>
        /// <param name="workspace"></param>
        private void WorkspaceOpened(WorkspaceModel workspace)
        {
            FileName = Path.GetFileNameWithoutExtension(workspace.FileName);

            var homespace = workspace as HomeWorkspaceModel;
            homespace.

            FileName += " Run completed?";

            // 2 Prepare the graph for exporting 
            //CleanUpGraph();

            // 3 Export an image
            //SaveGraphToImage();
            //CloseGraph();
        }

        /// <summary>
        /// Remove event handlers
        /// </summary>
        public void Dispose()
        {
            viewLoadedParamsInstance.CurrentWorkspaceChanged -= OnCurrentWorkspaceOpened; 
            this.CurrentWorkspace.EvaluationCompleted -= CurrentWorkspaceOnEvaluationCompleted;

        }
    }
}
