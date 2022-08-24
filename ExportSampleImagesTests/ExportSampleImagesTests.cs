using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Dynamo.Graph.Workspaces;
using DynamoCoreWpfTests.Utility;
using NUnit.Framework;
using SystemTestServices;
using ExportSampleImages;

namespace ExportSampleImagesTests
{
    public class ExportSampleImagesTests : SystemTestBase
    {
        protected override void GetLibrariesToPreload(List<string> libraries)
        {
            base.GetLibrariesToPreload(libraries);
        }

        internal ExportSampleImagesViewExtension GetExportSampleImagesViewExtension()
        {
            DispatcherUtil.DoEvents();
            var esiVE = GetViewExtensionsByType<ExportSampleImagesViewExtension>()
                .FirstOrDefault();
            return (ExportSampleImagesViewExtension) esiVE;
        }

        [Test, RequiresSTA]
        public void LoadGraphsFromFolderTest()
        {
            // Open test graph
            var testsDir = GetTestDirectory(ExecutingDirectory);

            // Get ESIViewExtension
            var esiVE = GetExportSampleImagesViewExtension();

            // Open ESI
            esiVE.exportSampleImagesMenuItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            esiVE.exportSampleImagesMenuItem.RaiseEvent(new RoutedEventArgs(MenuItem.CheckedEvent));
            DispatcherUtil.DoEvents();
            
            esiVE.ViewModel.SourcePathViewModel.FolderPath = testsDir;

            var filesInFolder = Directory.EnumerateFiles(testsDir, "*.dyn").Count();
            var viewExtensionGraphs = esiVE.ViewModel.Graphs.Count;

            // Assert the number of files in the tests folder is equal to the number of loaded graphs
            Assert.AreEqual(filesInFolder, viewExtensionGraphs);
        }

        [Test, RequiresSTA]
        public void ExportGraphsTest()
        {
            // Open test graph
            var testsDir = GetTestDirectory(ExecutingDirectory);
            var imagesDir = testsDir + "/images";

            // Get ESIViewExtension
            var esiVE = GetExportSampleImagesViewExtension();

            // Open ESI
            esiVE.exportSampleImagesMenuItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            esiVE.exportSampleImagesMenuItem.RaiseEvent(new RoutedEventArgs(MenuItem.CheckedEvent));
            DispatcherUtil.DoEvents();

            esiVE.ViewModel.SourcePathViewModel.FolderPath = testsDir;
            esiVE.ViewModel.TargetPathViewModel.FolderPath = imagesDir;

            esiVE.ViewModel.ExportGraphsCommand.Execute(null);
            DispatcherUtil.DoEvents();
            
            var graphsInput = Directory.EnumerateFiles(testsDir, "*.dyn").Count();
            var imagesOutput = Directory.EnumerateFiles(imagesDir, "*.png").Count();

            // Assert the number of files in the tests folder is equal to the number of loaded graphs
            Assert.AreEqual(graphsInput, imagesOutput);

            // Clean up
            DeleteFilesInFolder(imagesDir);
        }



        private void DeleteFilesInFolder(string folder)
        {
            System.IO.DirectoryInfo di = new DirectoryInfo(folder);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }
    }
}
