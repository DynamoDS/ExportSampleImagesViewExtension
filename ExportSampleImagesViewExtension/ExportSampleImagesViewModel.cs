﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
using PathType = ExportSampleImagesViewExtension.Controls.PathType;
using Point = System.Windows.Point;

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

        /// <summary>
        /// Collection of graphs loaded for exporting
        /// </summary>
        public ObservableCollection<GraphViewModel> Graphs { get; set; } 

        private Dictionary<string, GraphViewModel> graphDictionary = new Dictionary<string, GraphViewModel>();

        public PathViewModel SourcePathViewModel { get; set; } 
        public PathViewModel TargetPathViewModel { get; set; } 
        public DelegateCommand OpenGraphsCommand { get; set; }

        #endregion

        #region Loading

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

            TargetPathViewModel = new PathViewModel { Type = PathType.Target, Owner = viewLoadedParamsInstance.DynamoWindow };
            SourcePathViewModel = new PathViewModel { Type = PathType.Source, Owner = viewLoadedParamsInstance.DynamoWindow };

            SourcePathViewModel.PropertyChanged += SourcePathPropertyChanged;

            //Graphs = new ObservableCollection<GraphViewModel>
            //{
            //    new GraphViewModel{ GraphName = "Test 1" },
            //    new GraphViewModel{ GraphName = "Test 2" },
            //    new GraphViewModel{ GraphName = "Test 3" }
            //};

            OpenGraphsCommand = new DelegateCommand(OpenGraphs);
        }

        private void SourcePathPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            var pathVM = sender as PathViewModel;
            if (pathVM == null || pathVM.Type != PathType.Source) return;

            if (propertyChangedEventArgs.PropertyName == nameof(PathViewModel.FolderPath))
            {
                SourceFolderChanged(pathVM);
            }
        }

        private void SourceFolderChanged(PathViewModel pathVM)
        {
            Graphs = new ObservableCollection<GraphViewModel>();
            graphDictionary = new Dictionary<string, GraphViewModel>();

            var files = Utilities.Utilities.GetAllFilesOfExtension(pathVM.FolderPath);
            if (files == null)
                return;

            foreach (var graph in files)
            {
                var name = Path.GetFileNameWithoutExtension(graph);
                var graphVM = new GraphViewModel { GraphName = name };

                Graphs.Add(graphVM);
                graphDictionary[name] = graphVM;
            }

            RaisePropertyChanged(nameof(Graphs));
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

        #endregion

        #region Methods

        private void OpenGraphs(object obj)
        {
            if (string.IsNullOrEmpty(SourcePathViewModel.FolderPath) || string.IsNullOrEmpty(TargetPathViewModel.FolderPath))
                return;

            var files = Utilities.Utilities.GetAllFilesOfExtension(SourcePathViewModel.FolderPath);
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
                var graphName = Path.GetFileNameWithoutExtension(CurrentWorkspace.FileName);
                ExportCombinedImages(graphName);

                // 4 Update the UI
                graphDictionary[graphName].Exported = true;

                DoEvents();
            }
        }

        private void ExportCombinedImages(string graphName)
        {
            var pathForeground = Path.Combine(TargetPathViewModel.FolderPath, graphName + "_f.png");
            var pathBackground = Path.Combine(TargetPathViewModel.FolderPath, graphName + "_b.png");

            this.DynamoViewModel.Save3DImageCommand.Execute(pathBackground);
            this.DynamoViewModel.SaveImageCommand.Execute(pathForeground);

            OverlayImages(graphName, pathBackground, pathForeground);
            //CleanUp(pathBackground, pathForeground);
        }

        private void CleanUp(string pathBackground, string pathForeground)
        {
            File.Delete(pathBackground);
            File.Delete(pathForeground);
        }

        private void OverlayImages(string name, string background, string foreground)
        {
            var baseImage = (Bitmap)Image.FromFile(background);
            var overlayImage = (Bitmap)Image.FromFile(foreground);
            overlayImage = Resize(baseImage, overlayImage);

            var finalImage = new Bitmap(baseImage.Width, baseImage.Height, PixelFormat.Format32bppArgb);
            finalImage.SetResolution(120, 120);
            var graphics = Graphics.FromImage(finalImage);

            graphics.CompositingMode = CompositingMode.SourceOver;

            graphics.DrawImage(baseImage, 0, 0);
            graphics.DrawImage(overlayImage, 
                Convert.ToInt32((baseImage.Width - overlayImage.Width) * (float)0.5), 
                Convert.ToInt32((baseImage.Height - overlayImage.Height) * (float)0.5));    // Center the overlaid image
            
            //save the final composite image to disk
            finalImage.Save(Path.Combine(TargetPathViewModel.FolderPath, name + ".png"), ImageFormat.Png);
        }

        private Bitmap Resize(Bitmap sourceImg, Bitmap targetImg)
        {
            var scaleFactor = Math.Min(sourceImg.Width / (float)targetImg.Width, sourceImg.Height / (float)targetImg.Height);
            var newWidth = (int)(targetImg.Width * scaleFactor);
            var newHeight = (int)(targetImg.Height * scaleFactor);
            var newImage = new Bitmap(newWidth, newHeight);
            
            using (var graphics = Graphics.FromImage(newImage))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(targetImg, new Rectangle( 0, 0, newWidth, newHeight));
                return newImage;
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
            var homespace = workspace as HomeWorkspaceModel;
            homespace.

            FileName += " Run completed?";
        }

        #endregion

        #region Closing
        /// <summary>
        /// Remove event handlers
        /// </summary>
        public void Dispose()
        {
            viewLoadedParamsInstance.CurrentWorkspaceChanged -= OnCurrentWorkspaceOpened;
            SourcePathViewModel.PropertyChanged -= SourcePathPropertyChanged;
        }
        #endregion

        #region Utility
        /// <summary>
        /// Force the Dispatcher to empty it's queue
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
        #endregion
    }
}
