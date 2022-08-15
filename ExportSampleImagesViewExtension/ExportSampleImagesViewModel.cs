using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Threading;
using Dynamo.Controls;
using Dynamo.Core;
using Dynamo.Extensions;
using Dynamo.Graph.Workspaces;
using Dynamo.Models;
using Dynamo.UI.Commands;
using Dynamo.ViewModels;
using Dynamo.Views;
using Dynamo.Wpf.Extensions;
using ExportSampleImagesViewExtension.Controls;

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
        
        public PathViewModel SourcePathViewModel { get; set; } 
        public PathViewModel TargetPathViewModel { get; set; } 
        public DelegateCommand OpenGraphsCommand { get; set; }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p"></param>
        public ExportSampleImagesViewModel(ViewLoadedParams p)
        {
            if (p == null) return;
            
            FileName = "Test Name";

            this.viewLoadedParamsInstance = p;

            p.CurrentWorkspaceChanged += OnCurrentWorkspaceChanged;
            p.CurrentWorkspaceCleared += OnCurrentWorkspaceCleared;

            if (this.viewLoadedParamsInstance.CurrentWorkspaceModel is HomeWorkspaceModel)
            {
                this.CurrentWorkspace = this.viewLoadedParamsInstance.CurrentWorkspaceModel as HomeWorkspaceModel;
                this.DynamoViewModel = this.viewLoadedParamsInstance.DynamoWindow.DataContext as DynamoViewModel;

                this.ViewModelExecutive = p.ViewModelCommandExecutive;
                this.CommandExecutive = p.CommandExecutive;
                this.ViewModelCommandExecutive = p.ViewModelCommandExecutive;
            }

            SourcePathViewModel = new PathViewModel { FolderPath = "Test 1 you fucking idiot", Owner = viewLoadedParamsInstance.DynamoWindow };
            TargetPathViewModel = new PathViewModel { FolderPath = "Test 2 is working god damn it", Owner = viewLoadedParamsInstance.DynamoWindow };

            OpenGraphsCommand = new DelegateCommand(OpenGraphs);
        }

        private void OnCurrentWorkspaceCleared(IWorkspaceModel workspace)
        {
            CurrentWorkspace = this.viewLoadedParamsInstance.CurrentWorkspaceModel as HomeWorkspaceModel;
        }

        private void OnCurrentWorkspaceChanged(IWorkspaceModel workspace)
        {
            CurrentWorkspace = workspace as HomeWorkspaceModel;
            
            CurrentWorkspace.EvaluationCompleted += CurrentWorkspaceOnEvaluationCompleted;
        }

        /// <summary>
        /// There is a race condition here I cannot currently solve
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CurrentWorkspaceOnEvaluationCompleted(object sender, EvaluationCompletedEventArgs e)
        {
            CurrentWorkspace.EvaluationCompleted -= CurrentWorkspaceOnEvaluationCompleted;
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
        private string UpdatePath(object path)
        {
            string folder = Utilities.Utilities.GetFolderDialog();

            if (string.IsNullOrEmpty(folder))
                return string.Empty;
            if (CurrentWorkspace == null)
                return string.Empty;

            return folder;
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

                DoEvents(); // Allows visual tree to be reconstructed.

                // 2 Cleaunp Nodes
                this.DynamoViewModel.GraphAutoLayoutCommand.Execute(null);

                DoEvents();

                // 3 Save an image
                FileName = Path.GetFileNameWithoutExtension(CurrentWorkspace.FileName);
                var path = Path.Combine(TargetPath, FileName + ".png");
                this.DynamoViewModel.SaveImageCommand.Execute(path);
            }
        }

        // TODO: Try catch? How to capture if the opening was successful?
        private void OpenDynamoGraph(string path)
        {
            try
            {
                this.DynamoViewModel.OpenCommand.Execute(path);
            }
            catch (Exception) { }
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
        }

        /// <summary>
        /// Remove event handlers
        /// </summary>
        public void Dispose()
        {
            viewLoadedParamsInstance.CurrentWorkspaceChanged -= OnCurrentWorkspaceOpened;
        }


        /// <summary>
        ///     Force the Dispatcher to empty it's queue
        /// </summary>
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public static void DoEvents()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(ExitFrame), frame);
            Dispatcher.PushFrame(frame);
        }

        /// <summary>
        ///     Helper method for DispatcherUtil
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        private static object ExitFrame(object frame)
        {
            ((DispatcherFrame)frame).Continue = false;
            return null;
        }
    }
}
