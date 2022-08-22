using System;
using System.Windows;
using System.Windows.Controls;
using Dynamo.Graph.Workspaces;
using Dynamo.Wpf.Extensions;

namespace ExportSampleImages
{
    public class ExportSampleImagesViewExtension : ViewExtensionBase, IViewExtension
    {
        private MenuItem exportSampleImagesMenuItem;

        internal ExportSampleImagesView View;
        private ViewLoadedParams viewLoadedParamsReference;
        internal ExportSampleImagesViewModel ViewModel;

        public ExportSampleImagesViewExtension()
        {
            InitializeViewExtension();
        }

        /// <summary>
        ///     Extension Name
        /// </summary>
        //public override string Name => Properties.Resources.ExtensionName;
        public override string Name => "Export Sample Images";

        /// <summary>
        ///     GUID of the extension
        /// </summary>
        public override string UniqueId => "C1C9802B-5136-46F5-8BFC-F44BEE4DD283";

        public override void Dispose()
        {
            // Cleanup
            if (exportSampleImagesMenuItem != null)
            {
                exportSampleImagesMenuItem.Checked -= MenuItemCheckHandler;
                exportSampleImagesMenuItem.Unchecked -= MenuItemUnCheckHandler;
            }

            ViewModel?.Dispose();
            View = null;
            ViewModel = null;
        }

        public override void Loaded(ViewLoadedParams viewLoadedParams)
        {
            if (viewLoadedParams == null) throw new ArgumentNullException(nameof(viewLoadedParams));

            viewLoadedParamsReference = viewLoadedParams;

            // Add a button to Dynamo View menu to manually show the window
            exportSampleImagesMenuItem = new MenuItem {Header = "Show Export Sample Images", IsCheckable = true};
            exportSampleImagesMenuItem.Checked += MenuItemCheckHandler;
            exportSampleImagesMenuItem.Unchecked += MenuItemUnCheckHandler;

            viewLoadedParamsReference.AddExtensionMenuItem(exportSampleImagesMenuItem);
        }

        public void Shutdown()
        {
            Dispose();
        }

        private void InitializeViewExtension()
        {
            ViewModel = new ExportSampleImagesViewModel(viewLoadedParamsReference);
            View = new ExportSampleImagesView(ViewModel);
        }

        private void ViewLoadedParamsReferenceOnCurrentWorkspaceChanged(IWorkspaceModel workspace)
        {
            if (ViewModel == null) return;
            if (workspace as HomeWorkspaceModel == null) return;

            ViewModel.CurrentWorkspace = workspace as HomeWorkspaceModel;
        }

        private void MenuItemCheckHandler(object sender, RoutedEventArgs e)
        {
            AddToSidebar();
        }

        private void MenuItemUnCheckHandler(object sender, RoutedEventArgs e)
        {
            viewLoadedParamsReference.CloseExtensioninInSideBar(this);
        }

        private void AddToSidebar()
        {
            InitializeViewExtension();

            viewLoadedParamsReference?.AddToExtensionsSideBar(this, View);
        }

        public override void Closed()
        {
            if (exportSampleImagesMenuItem != null) exportSampleImagesMenuItem.IsChecked = false;
        }
    }
}