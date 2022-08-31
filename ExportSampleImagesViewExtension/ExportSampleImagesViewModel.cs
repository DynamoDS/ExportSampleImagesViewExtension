using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Threading;
using Dynamo.Core;
using Dynamo.Graph.Workspaces;
using Dynamo.Models;
using Dynamo.UI.Commands;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;
using Dynamo.Wpf.Utilities;
using ExportSampleImages.Controls;

namespace ExportSampleImages
{
    public class ExportSampleImagesViewModel : NotificationObject, IDisposable
    {

        #region Fields and Properties

        private readonly ViewLoadedParams viewLoadedParamsInstance;
        internal DynamoViewModel DynamoViewModel;
        internal HomeWorkspaceModel CurrentWorkspace;
        private readonly List<string> cleanupImageList = new List<string>();
        private Dictionary<string, GraphViewModel> graphDictionary = new Dictionary<string, GraphViewModel>();
        private bool cancel;

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

        private bool canExport;
        /// <summary>
        ///     Checks if both folder paths have been set
        /// </summary>
        public bool CanExport
        {
            get
            {
                if (SourcePathViewModel == null || TargetPathViewModel == null)
                    return false;
                return Utilities.AreValidPaths(SourcePathViewModel.FolderPath, TargetPathViewModel.FolderPath);
            }
            private set
            {
                if (canExport != value)
                {
                    canExport = value;
                    RaisePropertyChanged(nameof(CanExport));
                }
            }
        }

        private bool isSkip = true;
        /// <summary>
        ///     Contains user preference for skipping the pre-run
        /// </summary>
        public bool IsSkip
        {
            get
            {
                return isSkip;
            }
            set
            {
                if (isSkip != value)
                {
                    isSkip = value;
                    RaisePropertyChanged(nameof(IsSkip));
                }
            }
        }

        private bool isKeepFolderStructure = true;
        /// <summary>
        ///     Contains user preference to retain folder structure for images
        /// </summary>
        public bool IsKeepFolderStructure
        {
            get
            {
                return isKeepFolderStructure;
            }
            set
            {
                if (isKeepFolderStructure != value)
                {
                    isKeepFolderStructure = value;
                    RaisePropertyChanged(nameof(IsKeepFolderStructure));
                }
            }
        }
        
        private string notificationMessage;
        /// <summary>
        ///     Contains notification text displayed on the UI
        /// </summary>
        public string NotificationMessage
        {
            get
            {
                return notificationMessage;
            }

            set
            {
                if (notificationMessage != value)
                {
                    notificationMessage = value;
                    RaisePropertyChanged(nameof(notificationMessage));
                }
            }
        }

        public DelegateCommand ExportGraphsCommand { get; set; }
        public DelegateCommand CancelCommand { get; set; }

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

            TargetPathViewModel.PropertyChanged += SourcePathPropertyChanged;
            SourcePathViewModel.PropertyChanged += SourcePathPropertyChanged;

            ExportGraphsCommand = new DelegateCommand(ExportGraphs);
            CancelCommand = new DelegateCommand(Cancel);
        }

        // Handles source path changed
        private void SourcePathPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            var pathVM = sender as PathViewModel;
            if (pathVM == null) return;
            if (propertyChangedEventArgs.PropertyName == nameof(PathViewModel.FolderPath))
            {
                if (pathVM.Type == PathType.Source)
                {
                    SourceFolderChanged(pathVM);
                }

                RaisePropertyChanged(nameof(CanExport));
            }
        }

        // Update graphs if source folder is changed by the UI
        private void SourceFolderChanged(PathViewModel pathVM)
        {
            Graphs = new ObservableCollection<GraphViewModel>();
            graphDictionary = new Dictionary<string, GraphViewModel>();

            var files = Utilities.GetAllFilesOfExtension(pathVM.FolderPath)?.OrderBy(x => x);
            if (files == null)
                return;
            
            foreach (var graph in files)
            {
                var name = Path.GetFileNameWithoutExtension(graph);
                var graphVM = new GraphViewModel {GraphName = name};

                Graphs.Add(graphVM);
                graphDictionary[name] = graphVM;
            }

            NotificationMessage = String.Format(Properties.Resources.NotificationMsg, Graphs.Count.ToString());
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

            var files = Utilities.GetAllFilesOfExtension(SourcePathViewModel.FolderPath)?.OrderBy(x => x);
            if (files == null)
                return;

            if (!isSkip)
            {
                PrepareAutomaticGraphs(files);
                DoEvents();
            }

            ResetUi();

            int progress = 0;

            foreach (var (file, index) in files.Select((file, index) => (file, index)))
            {
                if (cancel) break;

                NotificationMessage = String.Format(Properties.Resources.ProcessMsg, (index+1).ToString(), files.Count().ToString());
                progress = index+1;

                // 1 Open a graph
                OpenDynamoGraph(file);
                DoEvents(); // Allows visual tree to be reconstructed.

                // 2 Run 
                CurrentWorkspace.Run();
                DoEvents();

                //CurrentWorkspace.RunSettings.RunType = RunType.Automatic;
                //DoEvents();

                //CurrentWorkspace.RunSettings.RunEnabled = true;
                //DoEvents();

                //CurrentWorkspace.RequestRun();
                //DoEvents();

                // 3 Zoom to fit Geometry
                DynamoViewModel.BackgroundPreviewViewModel.ZoomToFitCommand.Execute(null);
                DoEvents();
                DynamoViewModel.BackgroundPreviewViewModel.CanNavigateBackground = true;
                DoEvents();
                DynamoViewModel.ZoomOutCommand.Execute(null);
                DoEvents();

                DynamoViewModel.BackgroundPreviewViewModel.CanNavigateBackground = false;
                DoEvents();

                // 4 Auto Layout Nodes
                DynamoViewModel.GraphAutoLayoutCommand.Execute(null);
                DoEvents();

                // 5 Save an image
                var graphName = GetImagePath(CurrentWorkspace.FileName);
                ExportCombinedImages(graphName);

                // 6 Update the UI
                graphDictionary[Path.GetFileNameWithoutExtension(CurrentWorkspace.FileName)].Exported = true;
                DoEvents();
            }

            CleanUp();

            InformFinish(progress.ToString());
        }

        private void ResetUi()
        {
            cancel = false;
            graphDictionary.Values.ToList().ForEach(x => x.Exported = false);
        }


        private string GetImagePath(string graph)
        {
            var graphName = Path.GetFileNameWithoutExtension(graph);
            var directory = Path.GetDirectoryName(graph);
            if (directory == null) return null;

            var graphFolder = Path.GetFullPath(directory);
            var newFolder = IsKeepFolderStructure ? 
                    TargetPathViewModel.FolderPath + graphFolder.Substring(SourcePathViewModel.FolderPath.Length) :
                    TargetPathViewModel.FolderPath;

            if(isKeepFolderStructure)
            {
                Directory.CreateDirectory(newFolder);
            }

            return Path.Combine(newFolder, graphName);
        }

        private void PrepareAutomaticGraphs(IOrderedEnumerable<string> files)
        {
            foreach (var file in files)
            {
                OpenDynamoGraph(file);
                //DoEvents(); // Allows visual tree to be reconstructed.

                CurrentWorkspace.RunSettings.RunType = RunType.Automatic;
                DoEvents();

                //CurrentWorkspace.Save(file);    // Un hides all geometry nodes ... cannot use
                //DoEvents(); // Allows visual tree to be reconstructed.

                DynamoViewModel.SaveCommand.Execute(null);
                DoEvents(); // Allows visual tree to be reconstructed.

                DynamoViewModel.CloseHomeWorkspaceCommand.Execute(null);
                DoEvents(); // Allows visual tree to be reconstructed.
            }
        }

        private void InformFinish(string count)
        {
            var successMessage = String.Format(Properties.Resources.FinishMsg, count);
            var owner = Window.GetWindow(viewLoadedParamsInstance.DynamoWindow);

            MessageBoxService.Show(owner, successMessage, Properties.Resources.FinishMsgTitle, MessageBoxButton.OK, MessageBoxImage.Information);
        }


        private void ExportCombinedImages(string graphName)
        {
            var pathForeground = Path.Combine(TargetPathViewModel.FolderPath, graphName + "_f.png");
            var pathBackground = Path.Combine(TargetPathViewModel.FolderPath, graphName + "_b.png");

            DynamoViewModel.Save3DImageCommand.Execute(pathBackground);
            DynamoViewModel.SaveImageCommand.Execute(pathForeground);

            var finalImage = Utilities.OverlayImages(pathBackground, pathForeground, 0.9);
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

        private void Cancel(object obj)
        {
            cancel = true;
        }

        #endregion

        #region Closing

        /// <summary>
        ///     Remove event handlers
        /// </summary>
        public void Dispose()
        {
            TargetPathViewModel.PropertyChanged -= SourcePathPropertyChanged;
            SourcePathViewModel.PropertyChanged -= SourcePathPropertyChanged;
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