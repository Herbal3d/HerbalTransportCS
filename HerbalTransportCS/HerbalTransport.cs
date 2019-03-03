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
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using Fleck;

using org.herbal3d.cs.CommonEntitiesUtil;

namespace org.herbal3d.transport {

    // Called when a new basil client is connected an available
    public delegate void NewClient(BasilClient newClient);
    // Called when a SpaceServer is needed to take messages from a connection
    public delegate ISpaceServer SpaceServerForConnection(BasilConnection pConnection);

    // Structure passed around providing readonly handles to global things
    public class TransportContext {
        public IParameters Params;
        public BLogger Log;
        public CancellationTokenSource CancellationSource;
        public CancellationToken Cancellation;
    }

    public class HerbalTransport {
        private static readonly string _logHeader = "[HerbalTransport]";

        public TransportContext _context;

        private readonly ISpaceServer _spaceServer;
        private List<TransportConnection> _transports = new List<TransportConnection>();

        public HerbalTransport(ISpaceServer pSpaceServer, IParameters pParams, BLogger pLog) {
            _spaceServer = pSpaceServer;
            _context = new TransportContext() {
                Params = pParams,
                Log = pLog
            };
        }

        public void Start(CancellationTokenSource pCanceller) {
            _context.CancellationSource = pCanceller;
            _context.Cancellation = pCanceller.Token;
            StartServer(_context);
        }

        public void Cancel() {
            if (_context != null && _context.CancellationSource != null) {
                _context.CancellationSource.Cancel();
                // _context.CancellationSource.Dispose();
                _context.CancellationSource = null;
            }
        }

        private void StartServer(TransportContext pContext) {

            FleckLog.Level = LogLevel.Warn;
            List<TransportConnection> allClientConnections = new List<TransportConnection>();

            // For debugging, it is possible to set up a non-encrypted connection
            WebSocketServer server = null;
            if (pContext.Params.P<bool>("IsSecure")) {
                pContext.Log.DebugFormat("{0} Creating secure server", _logHeader);
                server = new WebSocketServer(pContext.Params.P<string>("SecureConnectionURL")) {
                    Certificate = new X509Certificate2(pContext.Params.P<string>("Certificate")),
                    EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12
                };
            }
            else {
                pContext.Log.DebugFormat("{0} Creating insecure server", _logHeader);
                server = new WebSocketServer(pContext.Params.P<string>("ConnectionURL"));
            }

            // Disable the ACK delay for better responsiveness
            if (pContext.Params.P<bool>("DisableNaglesAlgorithm")) {
                server.ListenerSocket.NoDelay = true;
            }

            // Loop around waiting for connections
            using (server) {
                server.Start(socket => {
                    pContext.Log.DebugFormat("{0} Received WebSocket connection", _logHeader);
                    lock (_transports) {
                        TransportConnection transportConnection = new TransportConnection(socket, pContext);
                        transportConnection.OnConnect += transport => {
                            var basilConnection = new BasilConnection(transport, _context);
                            var basilClient = new BasilClient(transportConnection.BasilMsgHandler, _context);
                            basilConnection.SpaceServiceProcessor.SetMsgHandler(_spaceServer);
                            basilConnection.BasilClientProcessor.SetMsgProcessor(basilClient);
                            // This is done last as it tells the SpaceServer that a connection is complete
                            _spaceServer.SetClientConnection(basilClient);
                        };
                        transportConnection.OnDisconnect += transport => {
                            lock (_transports) {
                                pContext.Log.InfoFormat("{0} client disconnected", _logHeader);
                                _transports.Remove(transport);
                            }
                        };
                        transportConnection.Start();
                        _transports.Add(transportConnection);
                    };

                });
                while (!pContext.Cancellation.IsCancellationRequested) {
                    Thread.Sleep(250);
                }
            }
        }

    }
}
