using Dynamo.Core;

namespace ExportSampleImages.Controls
{
    public class GraphViewModel : NotificationObject
    {
        private bool exported;
        private string graphName;

        /// <summary>
        ///     The name of the Graph
        /// </summary>
        public string GraphName
        {
            get => graphName;
            set
            {
                if (value != graphName)
                {
                    graphName = value;
                    RaisePropertyChanged(nameof(GraphName));
                }
            }
        }

        /// <summary>
        ///     Shows if the graph has been successfully exported
        /// </summary>
        public bool Exported
        {
            get => exported;
            set
            {
                if (value != exported)
                {
                    exported = value;
                    RaisePropertyChanged(nameof(Exported));
                }
            }
        }

        public string UniqueName { get; set; }
    }
}