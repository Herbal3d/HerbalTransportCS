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

        public event Action<TransportConnection> OnConnect;
        public event Action<BasilConnection> OnBasilConnect;
        public event Action<TransportConnection> OnDisconnect;

        public TransportContext _context;

        private readonly ISpaceServer _spaceServer;
        private List<TransportConnection> _transports = new List<TransportConnection>();
        private Task _serverTask;

        // In pParams, expects: ConnectionURL, IsSecure, SecureConnectionURL, DisableNaglesAlgorithm
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
            StartServer();
        }

        public void Cancel() {
            if (_context != null && _context.CancellationSource != null) {
                _context.CancellationSource.Cancel();
                // _context.CancellationSource.Dispose();
                _context.CancellationSource = null;
            }
        }

        private void StartServer() {
            _serverTask = Task.Run(() => {
                FleckLog.Level = LogLevel.Info;
                /*  Uncomment this if you want Fleck messages
                //  Haven't been able to get FleckLog.Level to set to anything other than 'Debug'
                FleckLog.Level = LogLevel.Debug;
                FleckLog.LogAction = (level, message, ex) => {
                    switch (level) {
                        case LogLevel.Debug:
                            _context.Log.DebugFormat(message, ex);
                            break;
                        case LogLevel.Error:
                            _context.Log.ErrorFormat(message, ex);
                            break;
                        case LogLevel.Warn:
                            _context.Log.ErrorFormat(message, ex);
                            break;
                        default:
                            _context.Log.InfoFormat(message, ex);
                            break;
                    }
                };
                */

                // For debugging, it is possible to set up a non-encrypted connection
                WebSocketServer server = null;
                if (_context.Params.P<bool>("IsSecure")) {
                    string connectionURL = _context.Params.P<string>("SecureConnectionURL");
                    _context.Log.DebugFormat("{0} Creating secure server on {1}", _logHeader, connectionURL);
                    server = new WebSocketServer(connectionURL) {
                        Certificate = new X509Certificate2(_context.Params.P<string>("Certificate")),
                        EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12
                    };
                }
                else {
                    string connectionURL = _context.Params.P<string>("ConnectionURL");
                    _context.Log.DebugFormat("{0} Creating insecure server on {1}", _logHeader, connectionURL);
                    server = new WebSocketServer(connectionURL);
                }

                // Disable the ACK delay for better responsiveness
                if (_context.Params.P<bool>("DisableNaglesAlgorithm")) {
                    _context.Log.DebugFormat("{0} Disabling Nagles algorightm", _logHeader);
                    server.ListenerSocket.NoDelay = true;
                }

                // Loop around waiting for connections
                using (server) {
                    server.Start(socket => {
                        _context.Log.DebugFormat("{0} Received WebSocket connection", _logHeader);
                        lock (_transports) {
                            TransportConnection transportConnection = new TransportConnection(socket, _context);
                            // When someone connects, set up BasilMessage processing
                            transportConnection.OnConnect += transport => {
                                _context.Log.DebugFormat("{0} OnConnect event", _logHeader);
                                var basilConnection = new BasilConnection(transportConnection, _context);
                                transportConnection.BasilMsgHandler = basilConnection;
                                TriggerConnect(transport);
                                TriggerBasilConnect(basilConnection);
                            };
                            transportConnection.OnDisconnect += transport => {
                                _context.Log.DebugFormat("{0} OnDisconnect event", _logHeader);
                                lock (_transports) {
                                    _context.Log.InfoFormat("{0} client disconnected", _logHeader);
                                    _transports.Remove(transport);
                                }
                                transportConnection.BasilMsgHandler = null;
                                TriggerDisconnect(transportConnection);
                            };

                            _transports.Add(transportConnection);
                            transportConnection.Start();
                        };
                    });
                    while (!_context.Cancellation.IsCancellationRequested) {
                        Task.Delay(250).Wait();
                    }
                }
                _context.Log.DebugFormat("{0} Exiting server listen task", _logHeader);
            }, _context.Cancellation);
        }

        // The WebSocket connection is connected. Tell the listeners.
        private void TriggerConnect(TransportConnection tConnection) {
            Action<TransportConnection> actions = OnConnect;
            if (actions != null) {
                foreach (Action<TransportConnection> action in actions.GetInvocationList()) {
                    action(tConnection);
                }
            }
        }

        // The WebSocket connection is connected. Tell the listeners.
        private void TriggerBasilConnect(BasilConnection bConnection) {
            Action<BasilConnection> actions = OnBasilConnect;
            if (actions != null) {
                foreach (Action<BasilConnection> action in actions.GetInvocationList()) {
                    action(bConnection);
                }
            }
        }

        // The WebSocket connection is disconnected. Tell the listeners.
        private void TriggerDisconnect(TransportConnection tConnection) {
            Action<TransportConnection> actions = OnDisconnect;
            if (actions != null) {
                foreach (Action<TransportConnection> action in actions.GetInvocationList()) {
                    action(tConnection);
                }
            }
        }

    }
}
