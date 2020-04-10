using System;

using Grasshopper.Kernel;
using WebSocketSharp;
using System.Timers;
using System.Text.RegularExpressions;

namespace MNML
{
    public class ghWebSocketComponent : GH_Component
    {

        public WebSocket socket;
        public String endpoint;
        private bool reconnect = false;
        private string runtimeMessage = null;
        private int reconnectionInterval = 1000;
        private DateTime lastPingTime;
        private TimeSpan pingPongInterval = new TimeSpan(0, 0, 5);
        private Timer pingCheckTimer = null;
        string latestMessage = null;

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
            pManager.AddTextParameter("Host", "H", "Hostname", GH_ParamAccess.item, "localhost");
            pManager.AddNumberParameter("Port", "P", "Port Number", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Interval", "I", "Reconnection Interval", GH_ParamAccess.item, 1000);

            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Socket", "S", "WebSocket Client", GH_ParamAccess.item);
            pManager.AddTextParameter("Message", "M", "Message", GH_ParamAccess.item);

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
            bool _reconnect = false;
            bool needsUpdate = false;

            if (latestMessage != null)
            {
                DA.SetData(1, latestMessage);
                latestMessage = null;
            }

            if (!DA.GetData(0, ref host)) return;
            if (!DA.GetData(1, ref port)) return;
            DA.GetData(2, ref reconnectionInterval);

            String _endpoint = String.Format("ws://{0}:{1}", host, Convert.ToInt32(port));

            needsUpdate = socket == null || _reconnect || (endpoint != _endpoint) || (socket != null && socket.ReadyState != WebSocketState.Open);
            
            if (endpoint != _endpoint)
            {
                endpoint = _endpoint;
            }

            reconnect = _reconnect;

            if (needsUpdate)
            {
                if (socket != null)
                {

                    if (socket.ReadyState == WebSocketState.Open)
                    {
                        socket.Close();
                    }
                    socket = null;
                }
                socket = Connect();
            }
            if (runtimeMessage !=null && runtimeMessage != "OPEN")
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, runtimeMessage);
            } else
            {
                ClearRuntimeMessages();
            }
            DA.SetData(0, socket);


        }

        private WebSocket Connect()
        {
            var _socket = new WebSocket(endpoint);
            _socket.OnOpen -= Socket_OnOpen;
            _socket.OnOpen += Socket_OnOpen;
            _socket.OnError -= Socket_OnError;
            _socket.OnError += Socket_OnError;
            _socket.OnClose -= Socket_OnClose;
            _socket.OnClose += Socket_OnClose;
            _socket.OnMessage -= Socket_OnMessage;
            _socket.OnMessage += Socket_OnMessage;
            _socket.Connect();
            return _socket;
        }

        private void Socket_OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Data.Contains("mnml:ping"))
            {
                lastPingTime = DateTime.Now;
                var delay = Int32.Parse(Regex.Match(e.Data, @"\d+").Value);
                pingPongInterval = new TimeSpan(0, 0, 0, 0, delay);
                (sender as WebSocket).Send("{\"action\":\"broadcast\", \"data\": \"mnml:pong\"}");
                if (pingCheckTimer != null)
                {
                    pingCheckTimer.Stop();
                    pingCheckTimer.Elapsed -= CheckIfPingIsReceived;
                    pingCheckTimer.Dispose();
                }
                pingCheckTimer = new Timer() { Interval = delay };
                pingCheckTimer.Elapsed += CheckIfPingIsReceived;
            } else
            {
                latestMessage = e.Data;
                OnPingDocument()?.ScheduleSolution(5, ScheduleCallback);
            }
        }

        private void ScheduleCallback(GH_Document document)
        {

            if (latestMessage == null) return;

            ExpireSolution(false);

        }

        private void CheckIfPingIsReceived(object sender, ElapsedEventArgs e)
        {
            var elapsed = DateTime.Now - lastPingTime;
            pingCheckTimer.Stop();
            pingCheckTimer.Elapsed -= CheckIfPingIsReceived;
            pingCheckTimer.Dispose();

            Console.WriteLine("Checking if we received new ping at" + DateTime.Now.ToString());

            if (elapsed.TotalMilliseconds > pingPongInterval.TotalMilliseconds * 1.5)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Socket May have died");
                Console.WriteLine("Socket may have died");
                OnPingDocument()?.ScheduleSolution(5, (GH_Document doc) =>
                {
                    if (socket.ReadyState == WebSocketState.Open)
                    {
                        socket.Close();
                    }
                    socket = null;
                    endpoint = null;
                    ExpireSolution(false);
                });
            }
        }


        private void Socket_OnClose(object sender, CloseEventArgs e)
        {
            runtimeMessage = "CLOSE " + e.Reason;
            OnPingDocument()?.ScheduleSolution(reconnectionInterval, (GH_Document doc) => {
                ExpireSolution(false);
            });
        }

        private void Socket_OnError(object sender, ErrorEventArgs e)
        {
            runtimeMessage = "Error " + e.Message;
            OnPingDocument()?.ScheduleSolution(reconnectionInterval, (GH_Document doc) => {
                ExpireSolution(false);
            });
        }

        private void Socket_OnOpen(object sender, EventArgs e)
        {
            if (runtimeMessage == "OPEN") return;
            runtimeMessage = "OPEN";
            var doc = OnPingDocument();
            doc?.ScheduleSolution(5, Callback);
        }

        private void Callback(GH_Document doc)
        {
            ExpireSolution(false);
        }


        public override void RemovedFromDocument(GH_Document document)
        {
            if (socket != null)
            {
                socket.OnOpen -= Socket_OnOpen;
                socket.OnError -= Socket_OnError;
                socket.OnClose -= Socket_OnClose;
                if (socket.ReadyState == WebSocketState.Open)
                {
                    socket.Close();
                }
                socket = null;
            }

            base.RemovedFromDocument(document);
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            Attributes.ExpireLayout();
            Attributes.PerformLayout();
            var component = new WebSocketStateComponent();
            var pivotPoint = new System.Drawing.PointF();
            var bounds = Attributes.DocObject.Attributes.Bounds;
            pivotPoint.X = (float)bounds.Right + 50;
            pivotPoint.Y = (float)(bounds.Y + bounds.Height * 0.5);
            component.Attributes.Pivot = pivotPoint;
            OnPingDocument().AddObject(component, false);
            component.Params.Input[0].AddSource(Params.Output[0]);
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
