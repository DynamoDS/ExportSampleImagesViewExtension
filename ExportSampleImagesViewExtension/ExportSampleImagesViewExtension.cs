using Dynamo.Wpf.Extensions;
using System;
using System.Windows;
using System.Windows.Controls;
using Dynamo.Controls;
using Dynamo.Graph.Workspaces;
using Dynamo.ViewModels;

namespace ExportSampleImagesViewExtension
{
    public class ExportSampleImagesViewExtension : ViewExtensionBase, IViewExtension
    {
        private ViewLoadedParams viewLoadedParamsReference;
        private MenuItem exportSampleImagesMenuItem;
        
        internal ExportSampleImagesView View;
        internal ExportSampleImagesViewModel ViewModel;

        /// <summary>
        /// Extension Name
        /// </summary>
        //public override string Name => Properties.Resources.ExtensionName;
        public override string Name => "Export Sample Images";

        /// <summary>
        /// GUID of the extension
        /// </summary>
        public override string UniqueId => "C1C9802B-5136-46F5-8BFC-F44BEE4DD283";

        public override void Dispose()
        {
            // Cleanup
            if (this.exportSampleImagesMenuItem != null)
            {
                this.exportSampleImagesMenuItem.Checked -= MenuItemCheckHandler;
                this.exportSampleImagesMenuItem.Unchecked -= MenuItemUnCheckHandler;
            }
            
            this.ViewModel?.Dispose();
            this.View = null;
            this.ViewModel = null;
        }
        
        public ExportSampleImagesViewExtension()
        {
            InitializeViewExtension();
        }

        private void InitializeViewExtension()
        {
            this.ViewModel = new ExportSampleImagesViewModel(this.viewLoadedParamsReference);
            this.View = new ExportSampleImagesView(this.ViewModel);
        }

        public override void Loaded(ViewLoadedParams viewLoadedParams)
        {
            if (viewLoadedParams == null) throw new ArgumentNullException(nameof(viewLoadedParams));

            this.viewLoadedParamsReference = viewLoadedParams;

            // Add a button to Dynamo View menu to manually show the window
            this.exportSampleImagesMenuItem = new MenuItem { Header = "Show Export Sample Images", IsCheckable = true };
            this.exportSampleImagesMenuItem.Checked += MenuItemCheckHandler;
            this.exportSampleImagesMenuItem.Unchecked += MenuItemUnCheckHandler;

            this.viewLoadedParamsReference.AddExtensionMenuItem(this.exportSampleImagesMenuItem);
        }

        private void ViewLoadedParamsReferenceOnCurrentWorkspaceChanged(IWorkspaceModel workspace)
        {
            if (this.ViewModel == null) return;
            if (workspace as HomeWorkspaceModel == null) return;

            this.ViewModel.CurrentWorkspace = workspace as HomeWorkspaceModel;
        }

        private void MenuItemCheckHandler(object sender, RoutedEventArgs e)
        {
            AddToSidebar();
        }

        private void MenuItemUnCheckHandler(object sender, RoutedEventArgs e)
        {
            this.viewLoadedParamsReference.CloseExtensioninInSideBar(this);
        }

        private void AddToSidebar()
        {
            InitializeViewExtension();

            this.viewLoadedParamsReference?.AddToExtensionsSideBar(this, this.View);
        }

        public void Shutdown()
        {
            this.Dispose();
        }
        
        public override void Closed()
        {
            if (this.exportSampleImagesMenuItem != null)
            {
                this.exportSampleImagesMenuItem.IsChecked = false;
            }
        }
    }
}
