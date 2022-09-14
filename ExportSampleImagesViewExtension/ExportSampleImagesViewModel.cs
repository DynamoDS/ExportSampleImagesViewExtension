using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using Dynamo.Controls;
using Dynamo.Core;
using Dynamo.Graph.Workspaces;
using Dynamo.Models;
using Dynamo.Scheduler;
using Dynamo.UI.Commands;
using Dynamo.Utilities;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;
using Dynamo.Wpf.Utilities;
using Dynamo.Wpf.ViewModels.Watch3D;
using ExportSampleImages.Controls;

namespace ExportSampleImages
{
    public enum RunPhase
    {
        Render,
        Export,
        Save
    };

    public class ExportSampleImagesViewModel : NotificationObject, IDisposable
    {

        #region Fields and Properties

        private readonly ViewLoadedParams viewLoadedParamsInstance;
        internal DynamoViewModel DynamoViewModel;
        internal HelixWatch3DViewModel Helix3DViewModel;
        internal HomeWorkspaceModel CurrentWorkspace;
        private Dictionary<int, GraphViewModel> graphDictionary = new Dictionary<int, GraphViewModel>();
        private RunPhase phase = RunPhase.Render;
        private bool locked = false;
        private bool finished = true;
        private DynamoScheduler scheduler;
        int progress = 0;
        
        public StringBuilder sb;

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

        private bool isZoomedOut = true;
        /// <summary>
        ///     Contains user preference for skipping the pre-run
        /// </summary>
        public bool IsZoomedOut
        {
            get
            {
                return isZoomedOut;
            }
            set
            {
                if (isZoomedOut != value)
                {
                    isZoomedOut = value;
                    RaisePropertyChanged(nameof(IsZoomedOut));
                }
            }
        }

        private bool resume = false;
        /// <summary>
        ///     When this flag is set to true, will attempt to resume progress
        ///     based on the log file in the current destination folder
        /// </summary>
        public bool Resume
        {
            get
            {
                return resume;
            }
            set
            {
                if (resume != value)
                {
                    resume = value;
                    RaisePropertyChanged(nameof(Resume));
                }
            }
        }

        //private bool disablePrompts = true;
        ///// <summary>
        /////     Indicates if Dynamo should suppress Warning dialogs
        /////     If not set, the run will be interrupted when prompt is shown
        ///// </summary>
        //public bool DisablePrompts
        //{
        //    get
        //    {
        //        return disablePrompts;
        //    }
        //    set
        //    {
        //        if (disablePrompts != value)
        //        {
        //            disablePrompts = value;
        //            RaisePropertyChanged(nameof(DisablePrompts));
        //        }
        //    }
        //}

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
        private Dispatcher dispatcher;
        private Queue<string> graphQueue;
        private bool exportGraphWithGeometryBackgorund;
        private bool export = true;

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
                Helix3DViewModel = DynamoViewModel.BackgroundPreviewViewModel as HelixWatch3DViewModel;
                dispatcher = viewLoadedParamsInstance.DynamoWindow.Dispatcher;
                scheduler = DynamoViewModel.Model.Scheduler;
            }

            //if(DisablePrompts) DynamoViewModel.Model.DisablePrompts = true;

            TargetPathViewModel = new PathViewModel
            { Type = PathType.Target, Owner = viewLoadedParamsInstance.DynamoWindow };
            SourcePathViewModel = new PathViewModel
            { Type = PathType.Source, Owner = viewLoadedParamsInstance.DynamoWindow };

            dispatcher.Hooks.DispatcherInactive += OnDispatcherFinished;

            TargetPathViewModel.PropertyChanged += SourcePathPropertyChanged;
            SourcePathViewModel.PropertyChanged += SourcePathPropertyChanged;

            ExportGraphsCommand = new DelegateCommand(ExportGraphs);
            CancelCommand = new DelegateCommand(Cancel);

            graphQueue = new Queue<string>();

            sb = new StringBuilder();
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
                else
                {
                    TargetFolderChanged();
                }

                RaisePropertyChanged(nameof(CanExport));
            }
        }

        // Update graphs if source folder is changed by the UI
        private void SourceFolderChanged(PathViewModel pathVM)
        {
            Graphs = new ObservableCollection<GraphViewModel>();
            graphDictionary = new Dictionary<int, GraphViewModel>();

            var files = Utilities.GetAllFilesOfExtension(pathVM.FolderPath)?.OrderBy(x => x);
            if (files == null)
                return;
            
            foreach (var graph in files)
            {
                var name = Path.GetFileNameWithoutExtension(graph);
                var uniqueName = Path.GetFullPath(graph);
                var graphVM = new GraphViewModel {GraphName = name, UniqueName = uniqueName };

                Graphs.Add(graphVM);
                graphDictionary[uniqueName.GetHashCode()] = graphVM;
            }
            
            NotificationMessage = String.Format(Properties.Resources.NotificationMsg, Graphs.Count.ToString());
            RaisePropertyChanged(nameof(Graphs));
        }

        private void TargetFolderChanged()
        {
            var log = GetLogFileInformation();

            // Do not enqueue the file if it is already in the log file
            if (resume && log != null)
            {
                Graphs.RemoveAll(x => log.Contains(x.UniqueName));
                graphDictionary = Graphs.ToDictionary(x => x.UniqueName.GetHashCode(), x => x);
            }

            NotificationMessage = String.Format(Properties.Resources.NotificationMsg, Graphs.Count.ToString());
            RaisePropertyChanged(nameof(Graphs));

        }

        private void OnCurrentWorkspaceCleared(IWorkspaceModel workspace)
        {
            CurrentWorkspace = viewLoadedParamsInstance.CurrentWorkspaceModel as HomeWorkspaceModel;
            if (CurrentWorkspace == null) return;

            //CurrentWorkspace.RunSettings.RunType = RunType.Automatic;
        }

        private void OnCurrentWorkspaceChanged(IWorkspaceModel workspace)
        {
            CurrentWorkspace = workspace as HomeWorkspaceModel;
            if (CurrentWorkspace == null) return;

            //CurrentWorkspace.RunSettings.RunType = RunType.Automatic;
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
            
            ResetUi();

            foreach (var file in Graphs.ToList().Select(x => x.UniqueName).ToList())
            {
                graphQueue.Enqueue(file);
            }
        }

        private string [] GetLogFileInformation()
        {
            var filePath = TargetPathViewModel.FolderPath + "\\log.txt";
            if (File.Exists(filePath))
            {
                return File.ReadAllLines(filePath);
            }

            return null;
        }

        private void OnDispatcherFinished(object sender, EventArgs e)
        {
            if (locked || graphQueue.Count > 0)
            {
                if (scheduler.HasPendingTasks) return;

                switch (phase)
                {
                    case (RunPhase.Render):
                        locked = true;
                        RenderGraph(graphQueue.Dequeue());
                        break;
                    case (RunPhase.Export):
                        ExportGraph();
                        break;
                    case (RunPhase.Save):
                        locked = false;
                        SaveGraph();
                        Iterate();
                        break;
                }
            }

            if (graphQueue.Count == 0 && !finished && !locked)
            {
                finished = true;
                InformFinish((progress).ToString());
            }
        }

        private void Iterate()
        {
            // 6 Update the UI
            NotificationMessage = String.Format(Properties.Resources.ProcessMsg, (progress + 1).ToString(), graphDictionary.Count.ToString());
            graphDictionary[Path.GetFullPath(CurrentWorkspace.FileName).GetHashCode()].Exported = true;

            EnterLog(CurrentWorkspace.FileName);

            progress++;
        }

        private void EnterLog(string entry)
        {
            sb.Append(entry);
            sb.Append(Environment.NewLine);
            
            File.AppendAllText(TargetPathViewModel.FolderPath + "\\log.txt", sb.ToString());
            sb.Clear();
        }

        private void RenderGraph(string file)
        {
            phase = RunPhase.Export;
            
            OpenDynamoGraph(file);
            
            if (CurrentWorkspace.RunSettings.RunType == RunType.Manual || !CurrentWorkspace.HasRunWithoutCrash)
            {
                // 2 Run 
                CurrentWorkspace.Run();
            }
        }

        private void ExportGraph()
        {
            phase = RunPhase.Save;

            // New method introduced in Helix3DViewModel
            if (Helix3DViewModel.HasRenderedGeometry)
            {
                ExportGraphAndBackground();
            }
            else
            {
                ExportGraphOnly();
            }
        }

        private void SaveGraph()
        {
            phase = RunPhase.Render;

            if (exportGraphWithGeometryBackgorund)
            {
                // 5 Save a combined image
                var graphName = GetImagePath(CurrentWorkspace.FileName);
                ExportCombinedImages(graphName);

            }
            else
            {
                // 5 Save a graph-only image
                var graphName = GetImagePath(CurrentWorkspace.FileName);
                ExportGraphOnlyImages(graphName);
            }
        }


        private void ExportGraphAndBackground()
        {
            exportGraphWithGeometryBackgorund = true;

            DynamoViewModel.BackgroundPreviewViewModel.ZoomToFitCommand.Execute(null);

            if (isZoomedOut)
            {
                DynamoViewModel.BackgroundPreviewViewModel.CanNavigateBackground = true;
                DynamoViewModel.ZoomOutCommand.Execute(null);
                DynamoViewModel.BackgroundPreviewViewModel.CanNavigateBackground = false;
            }

            // 4 Auto Layout Nodes
            DynamoViewModel.GraphAutoLayoutCommand.Execute(null);
        }

        private void ExportGraphOnly()
        {
            exportGraphWithGeometryBackgorund = false;

            // 4 Auto Layout Nodes
            DynamoViewModel.GraphAutoLayoutCommand.Execute(null);
        }


        private void ResetUi()
        {
            progress = 0;
            graphDictionary.Values.ToList().ForEach(x => x.Exported = false);
            finished = false;

            File.Delete(TargetPathViewModel.FolderPath + "\\log.txt");
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


        private void InformFinish(string count)
        {
            var successMessage = String.Format(Properties.Resources.FinishMsg, count);
            var owner = Window.GetWindow(viewLoadedParamsInstance.DynamoWindow);

            EnterLog(successMessage);

            MessageBoxService.Show(owner, successMessage, Properties.Resources.FinishMsgTitle, MessageBoxButton.OK, MessageBoxImage.Information);
        }


        private void ExportGraphOnlyImages(string graphName)
        {
            var pathForeground = Path.Combine(TargetPathViewModel.FolderPath, graphName + "_f.png");

            DynamoViewModel.SaveImageCommand.Execute(pathForeground);

            var finalImage = Utilities.PrepareImages(pathForeground);
            if (finalImage == null)
            {
                CleanUp(pathForeground);

                return;
            }
            Utilities.SaveBitmapToJpg(finalImage, TargetPathViewModel.FolderPath, graphName);

            CleanUp(pathForeground);
        }


        private void ExportCombinedImages(string graphName)
        {
            var pathForeground = Path.Combine(TargetPathViewModel.FolderPath, graphName + "_f.png");
            var pathBackground = Path.Combine(TargetPathViewModel.FolderPath, graphName + "_b.png");

            DynamoViewModel.Save3DImageCommand.Execute(pathBackground);
            DynamoViewModel.SaveImageCommand.Execute(pathForeground);

            var finalImage = Utilities.OverlayImages(pathBackground, pathForeground, 1.1);
            if (finalImage == null)
            {
                CleanUp(pathForeground);
                CleanUp(pathBackground);

                return;
            }
            Utilities.SaveBitmapToJpg(finalImage, TargetPathViewModel.FolderPath, graphName);

            CleanUp(pathForeground);
            CleanUp(pathBackground);
        }


        private void CleanUp(string image)
        {
            if(File.Exists(image))
                File.Delete(image);
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
            graphQueue.Clear();
            Reset();
        }

        private void Reset()
        {
            phase = RunPhase.Render;
            locked = false;
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
            dispatcher.Hooks.DispatcherInactive -= OnDispatcherFinished;

            CurrentWorkspace = null;
            DynamoViewModel = null;
            Helix3DViewModel = null;
            dispatcher = null;
            scheduler = null;
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

        /// <summary>
        /// Removes all items matching condition
        /// Source: https://stackoverflow.com/questions/5118513/removeall-for-observablecollections
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="coll"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        private static int RemoveAll<T>(ObservableCollection<T> coll, Func<T, bool> condition)
        {
            var itemsToRemove = coll.Where(condition).ToList();

            foreach (var itemToRemove in itemsToRemove)
            {
                coll.Remove(itemToRemove);
            }

            return itemsToRemove.Count;
        }

        #endregion
    }
}