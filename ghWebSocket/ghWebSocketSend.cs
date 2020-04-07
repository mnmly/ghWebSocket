using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using WebSocketSharp;
using System.Text;
using System.Text.Json;

namespace MNML
{
    public class ghWebSocketSend : GH_Component
    {
        string previousMessage = null;
        bool previousBroadcast = false;

        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ghWebSocketSend()
          : base("WebSocket Send", "WebSocket Send",
            "Send Message via WebSocket",
            "MNML", "Communication")
        {
            Params.ParameterSourcesChanged += ParamSourcesChanged;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Socket", "S", "Socket to send from", GH_ParamAccess.item);
            pManager.AddTextParameter("Message", "M", "Socket Message", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Broadcast", "B", "Broadcast message (action: broadcast)", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Force Send", "F", "Send Message forcefully", GH_ParamAccess.item, false);
            pManager[2].Optional = true;
            pManager[3].Optional = true; 
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Message", "M", "Socket Message", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            WebSocket socket = null;
            string message = null;
            bool broadcast = false;
            bool forceUpdate = false;
            bool isMessageString = false;
            bool needsUpdate = false;
            if (!DA.GetData(0, ref socket)) { return; }
            if (!DA.GetData(1, ref message)) { return; }
            DA.GetData(2, ref broadcast);
            DA.GetData(3, ref forceUpdate);

            JsonDocument doc = null;

            if (previousBroadcast != broadcast)
            {
                previousBroadcast = broadcast;
                needsUpdate = true;
            }
            if (previousMessage != message)
            {
                previousMessage = message;
                needsUpdate = true;
            }

            try
            {
                doc = JsonDocument.Parse(message);
            }
            catch (JsonException e)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, e.Message);
                isMessageString = true;
                doc = null;
            }
      
            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
                {
                    if (broadcast)
                    {
                        writer.WriteStartObject();
                        writer.WriteString("action", "broadcast");
                        writer.WritePropertyName("data");
                        if (isMessageString)
                        {
                            writer.WriteStringValue(message);
                        }
                        else
                        {
                            doc.WriteTo(writer);
                        }
                        writer.WriteEndObject();
                    } else
                    {
                        if (!isMessageString)
                        {
                            doc.WriteTo(writer);
                        }
                    }
                    writer.Flush();
                }
                if (doc != null)
                {
                    message = Encoding.UTF8.GetString(stream.ToArray());
                }
            }

            if (needsUpdate || forceUpdate)
            {
                socket.Send(message);
                DA.SetData(0, message);
            }
        }

        //This is for if any source connected, reconnected, removed, replacement 
        private void ParamSourcesChanged(Object sender, GH_ParamServerEventArgs e)
        {
            previousMessage = null;
            previousBroadcast = !previousBroadcast;
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
            get { return new Guid("6175312f-2616-4b41-9775-0e34ee320358"); }
        }
    }
}
