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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using Fleck;

using org.herbal3d.cs.CommonEntitiesUtil;

namespace org.herbal3d.transport {

    public class TransportContext {
        public IParameters Params;
        public BLogger Log;
        public CancellationTokenSource CancellationSource;
        public CancellationToken Cancellation;
    }

    public class ServerListener {
        private static readonly string _logHeader = "[ServerListener]";

        public event Action<ITransportConnection> OnConnect;
        public event Action<BasilConnection> OnBasilConnect;
        public event Action<BasilConnection> OnDisconnect;

        public TransportContext _context;

        private List<ITransportConnection> _transports = new List<ITransportConnection>();
        private Task _WSListenerTask;
        private Task _SocketListenerTask;

        // In pParams, expects: ConnectionURL, IsSecure, SecureConnectionURL, DisableNaglesAlgorithm
        public ServerListener(IParameters pParams, BLogger pLog) {
            _context = new TransportContext() {
                Params = pParams,
                Log = pLog
            };
            _context.Log.DebugFormat("{0} Initialization", _logHeader);
        }

        public void Start(CancellationTokenSource pCanceller) {
            _context.Log.DebugFormat("{0} Start", _logHeader);
            _context.CancellationSource = pCanceller;
            _context.Cancellation = pCanceller.Token;
            _WSListenerTask = StartWSListener();
            // _SocketListenerTask = StartSocketListener();
        }

        public void Cancel() {
            _context.Log.DebugFormat("{0} Cancel", _logHeader);
            if (_context != null && _context.CancellationSource != null) {
                _context.CancellationSource.Cancel();
                // _context.CancellationSource.Dispose();
                _context.CancellationSource = null;
            }
        }

        private Task StartWSListener() {
            return Task.Run(() => {
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
                        SetupServerBasilConnection(new TransportWS(socket, _context));
                    });
                    while (!_context.Cancellation.IsCancellationRequested) {
                        Task.Delay(100).Wait();
                    }
                }
                _context.Log.DebugFormat("{0} Exiting server listen task", _logHeader);
            }, _context.Cancellation);
        }

        // Given an accepted connection from a client, async setup the processing
        //    routines and call all the callbacks.
        private void SetupServerBasilConnection(ITransportConnection pTransport) {
            Task.Run(() => {
                lock (_transports) {
                    // When someone connects, set up BasilMessage processing
                    pTransport.OnConnect += transport => {
                        _context.Log.DebugFormat("{0} OnConnect event", _logHeader);
                        var basilConnection = new BasilConnection(pTransport, _context);
                        pTransport.MsgHandler = basilConnection;
                        TriggerConnect(transport);
                        TriggerBasilConnect(basilConnection);
                    };
                    pTransport.OnDisconnect += transport => {
                        _context.Log.DebugFormat("{0} OnDisconnect event", _logHeader);
                        lock (_transports) {
                            _context.Log.InfoFormat("{0} client disconnected", _logHeader);
                            _transports.Remove(transport);
                        }
                        TriggerDisconnect(pTransport.MsgHandler as BasilConnection);
                        pTransport.MsgHandler = null;
                    };

                    _transports.Add(pTransport);
                    pTransport.Start();
                };
            });
        }

        // A reset event used to lock and serialize the socket accepts
        private ManualResetEvent acceptDone = new ManualResetEvent(false);
        private Task StartSocketListener() {
            return Task.Run(() => {
                // Establish the local endpoint for the socket.  
                // The DNS name of the computer  
                // running the listener is "host.contoso.com".  
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);
                Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try {
                    listener.Bind(localEndPoint);
                    listener.Listen(100);
                    while (true) {
                        acceptDone.Reset();
                        listener.BeginAccept(new AsyncCallback(SocketAcceptCallback), listener);
                        // wait for one Accept to get into its callback before getting another
                        acceptDone.WaitOne();
                    }
                }
                catch (Exception e) {
                    _context.Log.ErrorFormat("{0} StartSocketListener exception: {1}", _logHeader, e);
                }
            }, _context.Cancellation);
        }

        private void SocketAcceptCallback(IAsyncResult pAR) {
            acceptDone.Set();
            // Get the socket that handles the client request.  
            Socket listener = (Socket)pAR.AsyncState;
            Socket clientSocket = listener.EndAccept(pAR);

            SetupServerBasilConnection(new TransportSocket(clientSocket, _context));
        }

        // The WebSocket connection is connected. Tell the listeners.
        private void TriggerConnect(ITransportConnection tConnection) {
            Action<ITransportConnection> actions = OnConnect;
            if (actions != null) {
                foreach (Action<ITransportConnection> action in actions.GetInvocationList()) {
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
        private void TriggerDisconnect(BasilConnection pReceiver) {
            Action<BasilConnection> actions = OnDisconnect;
            if (actions != null) {
                foreach (Action<BasilConnection> action in actions.GetInvocationList()) {
                    action(pReceiver);
                }
            }
        }

    }
}
