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
using System.Collections.Concurrent;
using System.Threading;

using org.herbal3d.cs.CommonUtil;

namespace org.herbal3d.transport {


    public enum BTransportConnectionStates {
        INITIALIZING,
        OPEN,
        CLOSING,
        ERROR,
        CLOSED
    };

    public delegate void BTransportOnStateChangeCallback(BTransport pTransport, BTransportConnectionStates pNewState, object pContext);
    public delegate void BTransportOnOpenCallback(BTransport pTransport);
    public delegate void BTransportOnCloseCallback(BTransport pTransport);
    public delegate void BTransportOnErrorCallback(BTransport pTransport);

    public delegate void BTransportReceptionCallback(BTransport pTransport, byte[] pMsg, object pContext);

    // When setup to listen for a network connection, this is called when a new connection is received
    public delegate void BTransportConnectionAcceptedProcessor(BTransport pTransport,
                                                    CancellationTokenSource pCanceller);

    // Parent class of parameter blocks for the different types of transport
    public abstract class BTransportParams {
        public bool preferred = false;
        public string transport = "UNKNOWN";
        public string protocol = "UNKNOWN";
        public string host = "UNKNOWN";
        public int port = 11440;
    }

    public abstract class BTransport {
        private static readonly string _logHeader = "[BTransport]";

        // The transport has a state.
        // Note that changing the state will invoke "OnStateChange".
        private BTransportConnectionStates _connectionState;
        public BTransportConnectionStates ConnectionState {
            get { return _connectionState; }
            set {
                if (_connectionState != value) {
                    _connectionState = value;
                    OnStateChanged();
                }
            }
        }

        public event BTransportOnStateChangeCallback OnStateChange;
        public event BTransportOnOpenCallback OnOpen;
        public event BTransportOnCloseCallback OnClose;
        public event BTransportOnErrorCallback OnError;

        // callback and context when a message is received
        protected BTransportReceptionCallback _receptionCallback;
        protected object _receptionCallbackContext;

        protected BlockingCollection<byte[]> _receiveQueue;
        protected BlockingCollection<byte[]> _sendQueue;

        protected readonly BLogger _log;

        public string TransportType = "UNKNOWN";
        public string ConnectionName = "UNKNOWN";

        public BTransport(string pType, BLogger pLog) {
            TransportType = pType;
            _log = pLog;
            if (_log == null) {
                throw new Exception("BTransportWS.constructor: logger parameter null");
            }

            _connectionState = BTransportConnectionStates.INITIALIZING;
            _receiveQueue = new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>());
            _sendQueue = new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>());
        }

        /*
         * Set the function called when a message is received.
         * The "context" is passed so the called function can have the class instance that asked for the message
         */
        public void SetReceiveCallback(BTransportReceptionCallback pCallback, object pContext) {
            // _log.Debug("{0} SetReceiveCallback. contextT={1}", _logHeader, pContext.GetType().FullName);
            _receptionCallback = pCallback;
            _receptionCallbackContext = pContext;
        }

        public virtual void Start() {
        }

        public abstract void Close();

        public virtual bool IsConnected() {
            return ConnectionState == BTransportConnectionStates.OPEN;
        }

        public void Send(byte[] pMsg) {
            if (IsConnected()) {
                _sendQueue.Add(pMsg);
            }
        }

        // The following functions invoke the event listeners.
        // This seems to be the .NET pattern for how you do events.
        BTransportConnectionStates _prevState = BTransportConnectionStates.INITIALIZING;
        protected virtual void OnStateChanged() {
            if (_connectionState != _prevState) {
                _log.Debug("{0} OnStateChanged {1}, for type {2} name {3}",
                        _logHeader, _connectionState, TransportType, ConnectionName);
                _prevState = _connectionState;
                BTransportOnStateChangeCallback cb = OnStateChange;
                cb?.Invoke(this, _connectionState, _receptionCallbackContext);
            }
        }
        protected virtual void OnOpened() {
            ConnectionState = BTransportConnectionStates.OPEN;
            BTransportOnOpenCallback cb = OnOpen;
            cb?.Invoke(this);
            OnStateChanged();
        }
        protected virtual void OnClosed() {
            ConnectionState = BTransportConnectionStates.CLOSED;
            BTransportOnCloseCallback cb = OnClose;
            cb?.Invoke(this);
            OnStateChanged();
        }
        protected virtual void OnErrored() {
            ConnectionState = BTransportConnectionStates.ERROR;
            BTransportOnErrorCallback cb = OnError;
            cb?.Invoke(this);
            OnStateChanged();
        }

    }
}
