// Copyright (c) 2019 Robert Adams
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using org.herbal3d.cs.CommonEntitiesUtil;
using org.herbal3d.OSAuth;

using HT = org.herbal3d.transport;
using BM = org.herbal3d.basil.protocol.Message;
using BT = org.herbal3d.basil.protocol.BasilType;

namespace org.herbal3d.transport {
    // Base class for SpaceServer layers.
    // Each SpaceServer layer listens for a connection, accepts the connect, and then
    //    processes the SpaceServer messages (MakeConnection, ...). The process is to
    //    have a 'listener' for the connection that, once receives, spins off a 
    //    layer processor for that client connection. For instance, SpaceServerStaticListener
    //    listens for clients and when connections are made, uses InstanceFactory to 
    //    create a SpaceServerStatic for handling the client interaction.
    public class SpaceServerListener {
        private static string _logHeader = "[SpaceServerListener]";

        // Canceller for the layer service. Other cancellers are created for each client.
        protected readonly CancellationTokenSource _canceller;

        protected readonly BLogger _log;

        // The URL for external clients to connect to this layer
        public string RemoteConnectionURL;

        // Each of the connections to a SpaceServer is a 'Session'
        protected List<HT.SpaceServerBase> Sessions;

        protected HT.ServerListener _transport;

        protected CreateSpaceServerProcessor _createProcessor;

        public delegate HT.SpaceServerBase CreateSpaceServerProcessor(CancellationTokenSource pCanceller,
                                                                        HT.BasilConnection pConnection);

        public SpaceServerListener(ParamBlock pParams, CancellationTokenSource pCanceller,
                                        BLogger pDebugLogger, CreateSpaceServerProcessor pProcessor) {
            _canceller = pCanceller;
            _log = pDebugLogger;
            _createProcessor = pProcessor;

            // Make sure we have all the parameters and set defaults
            ParamBlock completeParams = new ParamBlock(null, pParams,
                new ParamBlock(new Dictionary<string, object>() {
                    { "IsSecure", false },
                    { "ConnectionURL", "" },        // null or empty means no WS connection
                    { "SecureConnectionURL", "ws://localhost:9999" },
                    { "SocketConnectionURL", "" },  // null or empty means no socket connection
                    { "DisableNaglesAlgorithm", true },
                    { "ExternalAccessHostname", "" }
                })
            );

            // DEBUG DEBUG
            _log.DebugFormat("{0} Create BasilTestLayer", _logHeader);
            foreach (var kvp in completeParams.Params) {
                _log.DebugFormat("     {0} -> {1}", kvp.Key, kvp.Value);
            }
            // END DEBUG DEBUG

            // The Basil servers we have connected to
            Sessions = new List<HT.SpaceServerBase>();

            try {
                _log.DebugFormat("{0} Initializing transport", _logHeader);
                _transport = new HT.ServerListener(completeParams, _log);
                _transport.OnBasilConnect += Event_NewBasilConnection;
                _transport.OnDisconnect += Event_DisconnectBasilConnection;
                _transport.Start(_canceller);
            }
            catch (Exception e) {
                _log.ErrorFormat("{0} Exception creating transport: {1}", _logHeader, e);
            }

            // Build a URI for external hosts to access this layer
            UriBuilder connectionUri = new UriBuilder(completeParams.P<string>("ConnectionURL")) {
                Host = completeParams.P<string>("ExternalAccessHostname")
            };
            RemoteConnectionURL = connectionUri.ToString();
        }

        // Process a new Basil connection.
        private void Event_NewBasilConnection(HT.BasilConnection pBasilConnection) {
            _log.DebugFormat("{0} Event_NewBasilConnection", _logHeader);
            // Cancellation token for this client connection
            CancellationTokenSource sessionCanceller = new CancellationTokenSource();
            // Create the processor for the SpacceServer commands
            Sessions.Add(_createProcessor(sessionCanceller, pBasilConnection));
        }

        // Create an instance of the underlying class.
        // Each child class will override this method with proper logic to create the SpaceServer session
        protected virtual HT.SpaceServerBase InstanceFactory(CancellationTokenSource pCanceller,
                        HT.BasilConnection pConnection) {
            throw new NotImplementedException();
        }

        // One of the Basil servers has disconnected
        private void Event_DisconnectBasilConnection(HT.BasilConnection pBasilConnection) {
            _log.DebugFormat("{0} Event_DisconnectBasilConnection", _logHeader);

            // Find the client that is disconnected
            try {
                HT.SpaceServerBase disconnectedClient = Sessions.Where(c => c.ClientConnection == pBasilConnection).First();
                if (disconnectedClient != null) {
                    Sessions.Remove(disconnectedClient);
                    // Pass the error to the individual client
                    disconnectedClient.Shutdown();
                }
            }
            catch (InvalidOperationException ie) {
                var xx = ie; // get rid of the unreferenced var warning
                _log.ErrorFormat("{0} Event_DisconnectBasilConnection: did not find the closed client", _logHeader);
            }
            catch (Exception e) {
                _log.ErrorFormat("{0} Event_DisconnectBasilConnection: exception disconnecting client", _logHeader, e);
            }
        }

        // Stop what this instance is doing
        public virtual void Shutdown() {
            _canceller.Cancel();
        }

    }
}
