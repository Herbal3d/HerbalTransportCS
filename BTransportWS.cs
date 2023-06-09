// Copyright (c) 2021 Robert Adams
//
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
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using org.herbal3d.cs.CommonUtil;

using Fleck;

namespace org.herbal3d.transport {

    // Parameters used to listen for WebSocket connections
    public class BTransportWSParams : BTransportParams {
        // Usually run insecure with an nginx proxy in the front to eliminate all the SSL complexity
        public bool isSecure = false;
        public string bindHost = "0.0.0.0";
        public string certificate = null;
        public string externalURLTemplate = "ws://{0}:{1}/";
        public bool disableNaglesAlgorithm = false;
        public readonly string defaultProtocolPrefix = "ws:";
        public readonly string secureProtocolPrefix = "wss:";

        public BTransportWSParams(): base() {
            transport = BTransportWS.ID;
            protocol = "Basil-JSON";
            port = 11440;
        }

        public override string ExternalURL(string pExternalHostname) {
            // This is the URL the user must connect to.
            // The usual configuration is to have an nginx proxy in front of the web
            //     socket to handle the TLS certificate magic.
            // So the Fleck code below creates a "ws:" connection point but this advertizes
            //     an external "wss:" nginx URL.
            return String.Format(externalURLTemplate, pExternalHostname, port);
        }
    }

    public class BTransportWS : BTransport {

        private static readonly string _logHeader = "[BTransportWS]";

        // Named used to identify transport
        public static string ID = "WS";

        private readonly CancellationToken _overallCancellation;
        private WebSocketServer _server;
        private IWebSocketConnection _connection;

        private Task _inputQueueTask;
        private Task _outputQueueTask;

        /**
         * Transport for receiving and sending via WebSockets.
         * Receives a text or binary blob and passes it up the a BProtocol for translation.
         */
        public BTransportWS(IWebSocketConnection pSocket,
                            CancellationToken pCanceller,
                            BLogger pLogger): base(BTransportWS.ID, pLogger) {

            _connection = pSocket;
            _overallCancellation = pCanceller;

            _connection.OnOpen = Connection_OnOpen;
            _connection.OnClose = Connection_OnClose;
            _connection.OnMessage = msg => { Connection_OnMessage(msg); };
            _connection.OnBinary = msg => { Connection_OnBinary(msg); };
            _connection.OnError = except => { Connection_OnError(except); };
            ConnectionName = _connection.ConnectionInfo.ClientIpAddress.ToString()
                    + ":"
                    + _connection.ConnectionInfo.ClientPort.ToString();

            if (_overallCancellation == null) {
                throw new Exception("BTransportWS.constructor: OverallCancellation parameter null");
            }
            // _log.Debug("{0} Connection created {1}", _logHeader, ConnectionName); // DEBUG DEBUG
        }

        public override void Start() {
            base.Start();
            StartInputAndOutputQueueTasks();
        }

        public override void Close() {
            if (_connection != null) {
                _connection.Close();
                _connection = null;
            }
        }

        private void StartInputAndOutputQueueTasks() {
            // Tasks to push and pull from the input and output queues.
            BTransport hostingTransport = this;
            _inputQueueTask = Task.Run(() => {
                while (!_overallCancellation.IsCancellationRequested) {
                    byte[] msg = _receiveQueue.Take();
                    try {
                        if (_receptionCallback != null) {
                            // _log.Debug("{0} sending message to processor", _logHeader); // DEBUG DEBUG
                            // _log.Debug("{0}     xportT={1}, contextT={2}", _logHeader, hostingTransport.TransportType, _receptionCallbackContext.GetType().FullName); // DEBUG DEBUG
                            _receptionCallback(hostingTransport, msg, _receptionCallbackContext);
                        }
                        else {
                            _log.Debug("{0} message received with no processor", _logHeader);   // DEBUG DEBUG
                        }
                    }
                    catch (Exception ee) {
                        _log.Debug("{0} inputQueue: Exception: {1}", _logHeader, ee);
                    }
                }
            }, _overallCancellation);
            _outputQueueTask = Task.Run(() => {
                while (!_overallCancellation.IsCancellationRequested) {
                    byte[] msg = _sendQueue.Take();
                    _connection.Send(msg);
                }
            }, _overallCancellation);
        }

        // A WebSocket connection has been made.
        // Initialized the message processors.
        private void Connection_OnOpen() {
            _log.Debug("{0} Connection_OnOpen: connection state to OPEN", _logHeader);
            base.OnOpened();
        }

        // The WebSocket connection is closed. Any application state is out-of-luck
        private void Connection_OnClose() {
            // _log.Debug("{0} Connection_OnClose: connection state to CLOSED", _logHeader);
            base.OnClosed();
        }

        private void Connection_OnMessage(string pMsg) {
            if (IsConnected()) {
                // _log.Debug("{0} Connection_OnMessage: cn={1}", _logHeader, ConnectionName); // DEBUG DEBUG
                _receiveQueue.Add(Encoding.ASCII.GetBytes(pMsg));
            }
        }

        private void Connection_OnBinary(byte [] pMsg) {
            if (IsConnected()) {
                // _log.Debug("{0} Connection_OnBinary: cn={1}", _logHeader, ConnectionName);  // DEBUG DEBUG
                _receiveQueue.Add(pMsg);
            }
        }

        private void Connection_OnError(Exception pExcept) {
            base.OnErrored();
            _log.Error("{0} OnError event on {1}: {2}", _logHeader, ConnectionName, pExcept);
        }
            

        /*
        // This starts a connection listener for the passed "ConnectionURL".
        // For each connection received, a new BTransportWS is created and
        //    passed to the BTransportConnectionAcceptedProcesssor.
        public static Task ConnectionListener(
                            BTransportWSParams param,
                            BTransportConnectionAcceptedProcessor connectionProcessor,
                            CancellationTokenSource cancellerSource,
                            BLogger logger
                            ) {

            // Do the listening async
            return Task.Run(() => {

                BTransportParams _params = param;
                BTransportConnectionAcceptedProcessor _connectionProcessor = connectionProcessor;

                try {
                    WebSocketServer _server;

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
                    /*

                    // Build up the connection string needed for WS listen binding
                    // Note: this is different than external connection URL as it needs the transport
                    //     refix, the bind host and the port.
                    string connectionURL = (param.isSecure ? param.secureProtocolPrefix : param.defaultProtocolPrefix)
                            + "//" + param.bindHost + ":" + param.port.ToString();

                    // For debugging, it is possible to set up a non-encrypted connection
                    if (param.isSecure) {
                        logger.Debug("{0} Creating secure server on {1}", _logHeader, connectionURL);
                        _server = new WebSocketServer(connectionURL) {
                            Certificate = (X509Certificate2)X509Certificate2.CreateFromCertFile(param.certificate),
                            EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12
                        };
                    }
                    else {
                        logger.Debug("{0} Creating insecure server on {1}", _logHeader, connectionURL);
                        _server = new WebSocketServer(connectionURL);
                    }

                    // Disable the ACK delay for better responsiveness
                    if (param.disableNaglesAlgorithm) {
                        _server.ListenerSocket.NoDelay = true;
                    }

                    _server.Start(socket => {
                        // logger.Debug("{0} Received WebSocket connection for port {1}", _logHeader, _params.port);
                        CancellationTokenSource _connectionCanceller = new CancellationTokenSource();
                        CancellationToken _connectionCancellerToken = _connectionCanceller.Token;
                        BTransportWS xport = new BTransportWS(socket, _connectionCancellerToken, logger);
                        _connectionProcessor(xport, _connectionCanceller);
                    });
                }
                catch (Exception ex) {
                    logger.Error("{0} Exception starting listener: {1}", _logHeader, ex);
                }
            }, cancellerSource.Token);
        }
        */

    }

    // Since we can't create a WebSocket processor until we have the socket, this
    //    creates a listener that will create a processor for each connection.
    public class BTransportWSConnectionListener {

        public static string _logheader = "[BTransportWSConnectionListener]";

        public BTransportWSConnectionListener(
                        BTransportWSParams param,
                        BTransportConnectionAcceptedProcessor connectionProcessor,
                        CancellationTokenSource cancellerSource,
                        BLogger logger
                        ) {

            BTransportParams _params = param;
            BTransportConnectionAcceptedProcessor _connectionProcessor = connectionProcessor;

            // Do the listening async
            Task.Run(() => {

                try {
                    WebSocketServer _server;

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

                    // Build up the connection string needed for WS listen binding
                    // Note: this is different than external connection URL as it needs the transport
                    //     refix, the bind host and the port.
                    string connectionURL = (param.isSecure ? param.secureProtocolPrefix : param.defaultProtocolPrefix)
                            + "//" + param.bindHost + ":" + param.port.ToString();

                    // For debugging, it is possible to set up a non-encrypted connection
                    if (param.isSecure) {
                        logger.Debug("{0} Creating secure server on {1}", BTransportWSConnectionListener._logheader, connectionURL);
                        _server = new WebSocketServer(connectionURL) {
                            Certificate = (X509Certificate2)X509Certificate2.CreateFromCertFile(param.certificate),
                            EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12
                        };
                    }
                    else {
                        logger.Debug("{0} Creating insecure server on {1}", BTransportWSConnectionListener._logheader, connectionURL);
                        _server = new WebSocketServer(connectionURL);
                    }

                    // Disable the ACK delay for better responsiveness
                    if (param.disableNaglesAlgorithm) {
                        _server.ListenerSocket.NoDelay = true;
                    }

                    _server.Start(socket => {
                        // logger.Debug("{0} Received WebSocket connection for port {1}", _logHeader, _params.port);
                        CancellationTokenSource _connectionCanceller = new CancellationTokenSource();
                        CancellationToken _connectionCancellerToken = _connectionCanceller.Token;
                        BTransportWS xport = new BTransportWS(socket, _connectionCancellerToken, logger);
                        _connectionProcessor(xport, _connectionCanceller);
                    });
                }
                catch (Exception ex) {
                    logger.Error("{0} Exception starting listener: {1}", BTransportWSConnectionListener._logheader, ex);
                }
            }, cancellerSource.Token);
        }
    }
}
