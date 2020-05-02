using System;

using Grasshopper.Kernel;
using WebSocketSharp;

namespace MNML
{
    public class WebSocketStateComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public WebSocketStateComponent()
            : base("WebSocket state", "WebSocket State",
            "Shows websocket's Ready State",
                   "MNML", "Communication")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Socket", "S", "WebSocketClient", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Socket Info", "I", "ReadyState", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            String status = "";
            WebSocket socket = null;
            if (!DA.GetData(0, ref socket)) return;
            switch (socket.ReadyState)
            {
                case WebSocketState.Closed:
                    status = "Closed";
                    break;
                case WebSocketState.Closing:
                    status = "Closing";
                    break;
                case WebSocketState.Open:
                    status = "Open";
                    break;
                case WebSocketState.Connecting:
                    status = "Connecting";
                    break;
            }
            DA.SetData(0, status);
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            Attributes.ExpireLayout();
            Attributes.PerformLayout();
            if (Params.Output[0].SourceCount == 0) {
                var panel = new Grasshopper.Kernel.Special.GH_Panel();
                panel.CreateAttributes();
                var pivotPoint = new System.Drawing.PointF();
                var bounds = Attributes.DocObject.Attributes.Bounds;
                pivotPoint.X = (float)bounds.Right + 50;
                pivotPoint.Y = (float)(bounds.Y + bounds.Height * 0.5);
                panel.Attributes.Pivot = pivotPoint;
                OnPingDocument().AddObject(panel, false);
                panel.AddSource(Params.Output[0]);
            }

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
            get { return new Guid("387f3635-a1da-43cc-8c67-1331884031e6"); }
        }
    }
}
