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
using System.Threading.Tasks;

using Fleck;

namespace org.herbal3d.transport {
    // Wraps the socket connection and manages socket specific operations.
    public class TransportConnection {
        private static readonly string _logHeader = "[TransportConnection]";

        private IWebSocketConnection _connection = null;
        private BasilConnection _basilConnection = null;
        public readonly string Id;

        private BlockingCollection<byte[]> _receiveQueue;
        private BlockingCollection<byte[]> _sendQueue;

        public event Action<TransportConnection> OnDisconnect;
        public enum ConnectionStates {
            INITIALIZING,
            OPEN,
            CLOSING,
            ERROR,
            CLOSED
        };
        public ConnectionStates ConnectionState;
        public bool IsConnected {
            get {
                return (ConnectionState == ConnectionStates.OPEN && _basilConnection != null);
            }
        }
        public string ConnectionName = "UNKNOWN";

        private TransportContext _context;

        public TransportConnection(IWebSocketConnection pConnection, TransportContext pContext) {
            _connection = pConnection;
            _context = pContext;

            Id = _connection.ConnectionInfo.Id.ToString();
            ConnectionName = _connection.ConnectionInfo.ClientIpAddress.ToString()
                            + ":"
                            + _connection.ConnectionInfo.ClientPort.ToString();
            ConnectionState = ConnectionStates.INITIALIZING;

            _receiveQueue = new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>());
            _sendQueue = new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>());

            // Tasks to push and pull from the input and output queues.
            // These tasks are here so the context will be this object instance rather than
            //     creating the tasks in the OnOpen event context.
            Task.Run(() => {
                while (!_context.Cancellation.IsCancellationRequested) {
                    byte[] msg = _receiveQueue.Take();
                    _basilConnection.Receive(msg);
                }
            }, _context.Cancellation);
            Task.Run(() => {
                while (!_context.Cancellation.IsCancellationRequested) {
                    byte[] msg = _sendQueue.Take();
                    _connection.Send(msg);
                }
            }, _context.Cancellation);

            _connection.OnOpen = () => { Connection_OnOpen(); };
            _connection.OnClose = () => { Connection_OnClose(); };
            _connection.OnMessage = msg => { Connection_OnMessage(msg); };
            _connection.OnBinary = msg => { Connection_OnBinary(msg); };
            _connection.OnError = except => { Connection_OnError(except); };
        }

        public void Disconnect() {
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
                // Get the processor for the messages
                _basilConnection = new BasilConnection(this, _context);
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
            if (_basilConnection != null) {
                _basilConnection.AbortConnection();
            }
        }

        private void Connection_OnMessage(string pMsg) {
            if (IsConnected) {
                _basilConnection.Receive(pMsg);
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

        // The WebSocket connection is disconnected. Tell the listeners.
        private void TriggerDisconnect() {
            Action<TransportConnection> actions = OnDisconnect;
            if (actions != null) {
                foreach (Action<TransportConnection> action in actions.GetInvocationList()) {
                    action(this);
                }
            }
        }

        public void Send(byte[] pMsg) {
            if (IsConnected) {
                _sendQueue.Add(pMsg);
            }
        }
    }
}
