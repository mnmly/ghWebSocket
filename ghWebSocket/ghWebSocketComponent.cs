using System;

using Grasshopper.Kernel;
using WebSocketSharp;

namespace MNML
{
    public class ghWebSocketComponent : GH_Component
    {

        public WebSocket socket;
        public String endpoint;

        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ghWebSocketComponent()
            : base("WebSocket Client", "WebSocket Client",
            "Construct Websocket Client",
            "MNML", "Communication")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Host", "H", "Hostname", GH_ParamAccess.item);
            pManager.AddNumberParameter("Port", "P", "Port Number", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Socket", "S", "WebSocket Client", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            String host = "";
            double port = 0.0;

            if (!DA.GetData(0, ref host)) return;
            if (!DA.GetData(1, ref port)) return;

            String _endpoint = String.Format("ws://{0}:{1}", host, Convert.ToInt32(port));
            if (endpoint != _endpoint)
            {
                endpoint = _endpoint;
                if (socket != null) { socket.Dispose(); }
                socket = new WebSocket(endpoint);
            }
            if (socket.ReadyState != WebSocketState.Open)
            {
                socket.Connect();
            }
            DA.SetData(0, socket);
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            socket = null;
            base.RemovedFromDocument(document);
        }

        /// <summary>
        /// The Exposure property controls where in the panel a component icon 
        /// will appear. There are seven possible locations (primary to septenary), 
        /// each of which can be combined with the GH_Exposure.obscure flag, which 
        /// ensures the component will only be visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("d974ec1c-46ba-4206-a569-21a842901a8d"); }
        }
    }
}
