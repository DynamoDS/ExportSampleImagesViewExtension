using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dynamo.Core;

namespace ExportSampleImagesViewExtension.Controls
{
    public class GraphViewModel : NotificationObject
    {
        private string graphName;
        /// <summary>
        /// The name of the Graph
        /// </summary>
        public string GraphName
        {
            get { return graphName; }
            set
            {
                if (value != graphName)
                {
                    graphName = value;
                    RaisePropertyChanged(nameof(GraphName));
                }
            }
        }

        private string exported;
        /// <summary>
        /// Shows if the graph has been successfully exported
        /// </summary>
        public string Exported
        {
            get { return exported; }
            set
            {
                if (value != exported)
                {
                    exported = value;
                    RaisePropertyChanged(nameof(Exported));
                }
            }
        }
    }
}
