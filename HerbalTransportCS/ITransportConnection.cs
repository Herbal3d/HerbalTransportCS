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

namespace org.herbal3d.transport {
    public abstract class ITransportConnection {

        public event Action<ITransportConnection> OnConnect;
        public event Action<ITransportConnection> OnDisconnect;

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
                return (ConnectionState == ConnectionStates.OPEN && MsgHandler != null);
            }
        }
        public string ConnectionName = "UNKNOWN";

        protected TransportContext _context;
        protected BlockingCollection<byte[]> _receiveQueue;
        protected BlockingCollection<byte[]> _sendQueue;

        public ITransportMsgReceiver MsgHandler;

        public ITransportConnection(TransportContext pContext) {
            _context = pContext;

            _receiveQueue = new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>());
            _sendQueue = new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>());

            ConnectionState = ConnectionStates.INITIALIZING;
        }

        public abstract void Start();
        public abstract void Disconnect();

        // The WebSocket connection is connected. Tell the listeners.
        protected void TriggerConnect() {
            Action<ITransportConnection> actions = OnConnect;
            if (actions != null) {
                foreach (Action<ITransportConnection> action in actions.GetInvocationList()) {
                    action(this);
                }
            }
        }

        // The WebSocket connection is disconnected. Tell the listeners.
        protected void TriggerDisconnect() {
            Action<ITransportConnection> actions = OnDisconnect;
            if (actions != null) {
                foreach (Action<ITransportConnection> action in actions.GetInvocationList()) {
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
