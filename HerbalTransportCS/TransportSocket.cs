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
using System.Collections.Concurrent;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using Fleck;

namespace org.herbal3d.transport {
    // Wraps the socket connection and manages socket specific operations.
    public class TransportSocket : ITransportConnection {
        private static readonly string _logHeader = "[TransportSocket]";

        private Socket _connection = null;
        public readonly string Id;

        public TransportSocket(Socket pConnection, TransportContext pContext)
                                : base(pContext) {
            _connection = pConnection;

            /*
            _connection.OnOpen = Connection_OnOpen;
            _connection.OnClose = Connection_OnClose;
            _connection.OnMessage = msg => { Connection_OnMessage(msg); };
            _connection.OnBinary = msg => { Connection_OnBinary(msg); };
            _connection.OnError = except => { Connection_OnError(except); };

            Id = _connection.ConnectionInfo.Id.ToString();
            ConnectionName = _connection.ConnectionInfo.ClientIpAddress.ToString()
                            + ":"
                            + _connection.ConnectionInfo.ClientPort.ToString();
            */
        }

        public override void Start() {

            // Tasks to push and pull from the input and output queues.
            // These tasks are created here so the context will be this object instance rather than
            //     creating the tasks in the OnOpen event context.
            Task.Run(() => {
                while (!_context.Cancellation.IsCancellationRequested) {
                    byte[] msg = _receiveQueue.Take();
                    if (MsgHandler != null) {
                        MsgHandler.Receive(msg);
                    }
                }
            }, _context.Cancellation);
            Task.Run(() => {
                while (!_context.Cancellation.IsCancellationRequested) {
                    byte[] msg = _sendQueue.Take();
                    _connection.Send(msg);
                }
            }, _context.Cancellation);
        }

        public override void Disconnect() {
            if (IsConnected) {
                ConnectionState = ConnectionStates.CLOSING;
                this.TriggerDisconnect();
                _connection.Close();
            }
        }

        // A WebSocket connection has been made.
        // Initialized the message processors.
        private void Connection_OnOpen() {
            if (ConnectionState == ConnectionStates.INITIALIZING) {
                ConnectionState = ConnectionStates.OPEN;
                TriggerConnect();
            }
            else {
                ConnectionState = ConnectionStates.ERROR;
                _context.Log.ErrorFormat("{0} OnOpen event on {1} when connection not initializing",
                        _logHeader, ConnectionName);
            }
        }

        // The WebSocket connection is closed. Any application state is out-of-luck
        private void Connection_OnClose() {
            ConnectionState = ConnectionStates.CLOSED;
            TriggerDisconnect();
            if (MsgHandler != null) {
                MsgHandler.AbortConnection();
            }
        }

        private void Connection_OnMessage(string pMsg) {
            if (IsConnected) {
                MsgHandler.Receive(pMsg);
            }
        }

        private void Connection_OnBinary(byte [] pMsg) {
            if (IsConnected) {
                _receiveQueue.Add(pMsg);
            }
        }

        private void Connection_OnError(Exception pExcept) {
            ConnectionState = ConnectionStates.ERROR;
            _context.Log.ErrorFormat("{0} OnError event on {1}: {2}", _logHeader, ConnectionName, pExcept);
        }

    }
}
