using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Security.Permissions;
using System.Windows.Threading;
using Dynamo.Core;
using Dynamo.Graph.Workspaces;
using Dynamo.Models;
using Dynamo.UI.Commands;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;
using ExportSampleImages.Controls;

namespace ExportSampleImages
{
    public class ExportSampleImagesViewModel : NotificationObject, IDisposable
    {
        #region Closing

        /// <summary>
        ///     Remove event handlers
        /// </summary>
        public void Dispose()
        {
            SourcePathViewModel.PropertyChanged -= SourcePathPropertyChanged;
        }

        #endregion

        #region Fields and Properties

        private readonly ViewLoadedParams viewLoadedParamsInstance;
        internal DynamoViewModel DynamoViewModel;
        internal HomeWorkspaceModel CurrentWorkspace;
        private readonly List<string> cleanupImageList = new List<string>();
        private Dictionary<string, GraphViewModel> graphDictionary = new Dictionary<string, GraphViewModel>();

        /// <summary>
        ///     Collection of graphs loaded for exporting
        /// </summary>
        public ObservableCollection<GraphViewModel> Graphs { get; set; }

        /// <summary>
        ///     The source path containing dynamo graphs to be exported
        /// </summary>
        public PathViewModel SourcePathViewModel { get; set; }

        /// <summary>
        ///     The target path where the images will be stored
        /// </summary>
        public PathViewModel TargetPathViewModel { get; set; }

        public DelegateCommand ExportGraphsCommand { get; set; }

        #endregion

        #region Loading

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="p"></param>
        public ExportSampleImagesViewModel(ViewLoadedParams p)
        {
            if (p == null) return;

            viewLoadedParamsInstance = p;

            p.CurrentWorkspaceChanged += OnCurrentWorkspaceChanged;
            p.CurrentWorkspaceCleared += OnCurrentWorkspaceCleared;

            if (viewLoadedParamsInstance.CurrentWorkspaceModel is HomeWorkspaceModel)
            {
                CurrentWorkspace = viewLoadedParamsInstance.CurrentWorkspaceModel as HomeWorkspaceModel;
                DynamoViewModel = viewLoadedParamsInstance.DynamoWindow.DataContext as DynamoViewModel;
            }

            TargetPathViewModel = new PathViewModel
                {Type = PathType.Target, Owner = viewLoadedParamsInstance.DynamoWindow};
            SourcePathViewModel = new PathViewModel
                {Type = PathType.Source, Owner = viewLoadedParamsInstance.DynamoWindow};

            SourcePathViewModel.PropertyChanged += SourcePathPropertyChanged;

            ExportGraphsCommand = new DelegateCommand(ExportGraphs);
        }

        // Handles source path changed
        private void SourcePathPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            var pathVM = sender as PathViewModel;
            if (pathVM == null || pathVM.Type != PathType.Source) return;
            if (propertyChangedEventArgs.PropertyName == nameof(PathViewModel.FolderPath)) SourceFolderChanged(pathVM);
        }

        // Update graphs if source folder is changed by the UI
        private void SourceFolderChanged(PathViewModel pathVM)
        {
            Graphs = new ObservableCollection<GraphViewModel>();
            graphDictionary = new Dictionary<string, GraphViewModel>();

            var files = Utilities.GetAllFilesOfExtension(pathVM.FolderPath);
            if (files == null)
                return;

            foreach (var graph in files)
            {
                var name = Path.GetFileNameWithoutExtension(graph);
                var graphVM = new GraphViewModel {GraphName = name};

                Graphs.Add(graphVM);
                graphDictionary[name] = graphVM;
            }

            RaisePropertyChanged(nameof(Graphs));
        }

        private void OnCurrentWorkspaceCleared(IWorkspaceModel workspace)
        {
            CurrentWorkspace = viewLoadedParamsInstance.CurrentWorkspaceModel as HomeWorkspaceModel;
        }

        private void OnCurrentWorkspaceChanged(IWorkspaceModel workspace)
        {
            CurrentWorkspace = workspace as HomeWorkspaceModel;
            if (CurrentWorkspace == null) return;

            CurrentWorkspace.EvaluationCompleted += CurrentWorkspaceOnEvaluationCompleted;
        }

        private void CurrentWorkspaceOnEvaluationCompleted(object sender, EvaluationCompletedEventArgs e)
        {
            CurrentWorkspace.EvaluationCompleted -= CurrentWorkspaceOnEvaluationCompleted;
        }

        #endregion

        #region Methods

        /// <summary>
        ///     The main method executing the export
        /// </summary>
        /// <param name="obj"></param>
        private void ExportGraphs(object obj)
        {
            if (string.IsNullOrEmpty(SourcePathViewModel.FolderPath) ||
                string.IsNullOrEmpty(TargetPathViewModel.FolderPath))
                return;

            var files = Utilities.GetAllFilesOfExtension(SourcePathViewModel.FolderPath);
            if (files == null)
                return;

            foreach (var file in files)
            {
                // 1 Open a graph
                OpenDynamoGraph(file);

                DoEvents(); // Allows visual tree to be reconstructed.

                // 2 Cleanup Nodes
                DynamoViewModel.GraphAutoLayoutCommand.Execute(null);

                DoEvents();

                // 3 Save an image
                var graphName = Path.GetFileNameWithoutExtension(CurrentWorkspace.FileName);
                ExportCombinedImages(graphName);

                // 4 Update the UI
                graphDictionary[graphName].Exported = true;

                DoEvents();
            }

            CleanUp();
        }


        private void ExportCombinedImages(string graphName)
        {
            var pathForeground = Path.Combine(TargetPathViewModel.FolderPath, graphName + "_f.png");
            var pathBackground = Path.Combine(TargetPathViewModel.FolderPath, graphName + "_b.png");

            DynamoViewModel.Save3DImageCommand.Execute(pathBackground);
            DynamoViewModel.SaveImageCommand.Execute(pathForeground);

            var finalImage = Utilities.OverlayImages(pathBackground, pathForeground);
            Utilities.SaveBitmapToPng(finalImage, TargetPathViewModel.FolderPath, graphName);

            cleanupImageList.Add(pathForeground);
            cleanupImageList.Add(pathBackground);
        }


        private void CleanUp()
        {
            if (cleanupImageList == null || cleanupImageList.Count == 0) return;

            foreach (var image in cleanupImageList) File.Delete(image);
        }

        // TODO: Should we bubble errors to Dynamo?
        private void OpenDynamoGraph(string path)
        {
            try
            {
                DynamoViewModel.OpenCommand.Execute(path);
            }
            catch (Exception)
            {
            }
        }

        #endregion

        #region Utility

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
            ((DispatcherFrame) frame).Continue = false;
            return null;
        }

        #endregion
    }
}