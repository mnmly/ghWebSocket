using System;
using System.Collections.Generic;

using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using WebSocketSharp;

namespace MNML
{
    public class WebSocketReceiveComponent : GH_Component
    {

        WebSocket lastSocket = null;
        string latestMessage = null;

        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>

        public WebSocketReceiveComponent()
          : base("WebSocket Receive", "WebSocket Receive",
            "Receive Message via WebSocket",
            "MNML", "Communication")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Socket", "S", "Socket to recieve message", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Message", "M", "Message", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            WebSocket socket = null;
            if (!DA.GetData(0, ref socket)) return;
            lastSocket = socket;
            socket.OnMessage -= Socket_OnMessage;
            socket.OnMessage += Socket_OnMessage;
            if (latestMessage != null)
            {
                DA.SetData(0, latestMessage);
            }
        }

        private void Socket_OnMessage(object sender, MessageEventArgs e)
        {
            latestMessage = e.Data;
            OnPingDocument().ScheduleSolution(5, ScheduleCallback);
        }

        private void ScheduleCallback(GH_Document document)
        {

            if (latestMessage == null) return;

            ExpireSolution(false);

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
            get { return new Guid("01ad9809-0cc0-4aba-bb1a-46a1377ea332"); }
        }
    }
}
