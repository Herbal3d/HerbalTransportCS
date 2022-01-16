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

    public class BTransportWS : BTransport {

        private static readonly string _logHeader = "[BTransportWS]";

        private readonly CancellationToken _overallCancellation;
        private WebSocketServer _server;
        private IWebSocketConnection _connection;

        /**
         * Transport for receiving and sending via WebSockets.
         * Receives a text or binary blob and passes it up the a BProtocol for translation.
         */
        public BTransportWS(IWebSocketConnection pSocket, CancellationToken pCanceller, BLogger pLogger): base(pLogger) {
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
            StartInputAndOutputQueueTasks();
        }

        public override void Close() {
            throw new NotImplementedException();
        }

        private void StartInputAndOutputQueueTasks() {
            // Tasks to push and pull from the input and output queues.
            // These tasks are created here so the context will be this object instance rather than
            //     creating the tasks in the OnOpen event context.
            Task.Run(() => {
                while (!_overallCancellation.IsCancellationRequested) {
                    byte[] msg = _receiveQueue.Take();
                    base.OnMsged(msg);
                }
            }, _overallCancellation);
            Task.Run(() => {
                while (!_overallCancellation.IsCancellationRequested) {
                    byte[] msg = _sendQueue.Take();
                    _connection.Send(msg);
                }
            }, _overallCancellation);
        }

        // A WebSocket connection has been made.
        // Initialized the message processors.
        private void Connection_OnOpen() {
            if (ConnectionState == BTransportConnectionStates.INITIALIZING) {
                ConnectionState = BTransportConnectionStates.OPEN;
                base.OnOpened();
                _log.Debug("{0} Connection_OnOpen: connection state to OPEN");
            }
            else {
                ConnectionState = BTransportConnectionStates.ERROR;
                base.OnStateChanged();
                _log.Error("{0} OnOpen event on {1} when connection not initializing",
                        _logHeader, ConnectionName);
            }
        }

        // The WebSocket connection is closed. Any application state is out-of-luck
        private void Connection_OnClose() {
            ConnectionState = BTransportConnectionStates.CLOSED;
            base.OnClosed();
            _log.Debug("{0} Connection_OnClose: connection state to CLOSED");
        }

        private void Connection_OnMessage(string pMsg) {
            if (IsConnected()) {
                _receiveQueue.Add(Encoding.ASCII.GetBytes(pMsg));
            }
        }

        private void Connection_OnBinary(byte [] pMsg) {
            if (IsConnected()) {
                _receiveQueue.Add(pMsg);
            }
        }

        private void Connection_OnError(Exception pExcept) {
            ConnectionState = BTransportConnectionStates.ERROR;
            _log.Error("{0} OnError event on {1}: {2}", _logHeader, ConnectionName, pExcept);
        }
            
        // This starts a connection listener for the passed "ConnectionURL".
        // For each connection received, a new BTransportWS is created and
        //    passed to the BTransportConnectionAcceptedProcesssor.
        public static Task ConnectionListener(ParamBlock pParams,
                            BTransportConnectionAcceptedProcessor pProcessor,
                            CancellationTokenSource pCancellerSource,
                            BLogger pLogger) {
            ParamBlock _params = new ParamBlock(null, pParams,
                    new ParamBlock(new Dictionary<string, object>() {
                        {  "ConnectionURL",          "" },
                        {  "IsSecure",               false},
                        {  "SecureConnectionURL",    ""},
                        {  "Certificate",            null},
                        {  "DisableNaglesAlgorithm", true},
                        {  "ExternalAccessHostname", ""}
                    }));


            return Task.Run(() => {
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

                // For debugging, it is possible to set up a non-encrypted connection
                string transportURL = _params.P<string>("ConnectionURL");
                if (transportURL.StartsWith("wss:")) {
                    pLogger.Debug("{0} Creating secure server on {1}", _logHeader, transportURL);
                    _server = new WebSocketServer(transportURL) {
                        Certificate = new X509Certificate2(_params.P<string>("Certificate")),
                        EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12
                    };
                }
                else {
                    pLogger.Debug("{0} Creating insecure server on {1}", _logHeader, transportURL);
                    _server = new WebSocketServer(transportURL);
                }

                // Disable the ACK delay for better responsiveness
                if (_params.P<bool>("DisableNaglesAlgorithm")) {
                    pLogger.Debug("{0} Disabling Nagles algorightm", _logHeader);
                    _server.ListenerSocket.NoDelay = true;
                }

                _server.Start(socket => {
                    // Context.Log.DebugFormat("{0} Received WebSocket connection", _logHeader);
                    CancellationTokenSource _connectionCanceller = new CancellationTokenSource();
                    CancellationToken _connectionCancellerToken = _connectionCanceller.Token;
                    BTransportWS xport = new BTransportWS(socket, _connectionCancellerToken, pLogger);
                    pProcessor(xport, _connectionCanceller, _params);
                });
            }, pCancellerSource.Token);
        }

    }
}
