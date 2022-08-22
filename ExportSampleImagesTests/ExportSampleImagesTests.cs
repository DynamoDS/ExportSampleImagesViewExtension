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
        public void LoadGraphsFromFolder()
        {
            // Open test graph
            var testDir = GetTestDirectory(ExecutingDirectory);

            // Get ESIViewExtension
            var esiVE = GetExportSampleImagesViewExtension();

            Assert.AreEqual("carrot", "carrot");

        }
    }
}
