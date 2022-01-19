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
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
    public delegate void BTransportOnMsgCallback(BTransport pTransport, byte[] pMsg, object pContext);
    public delegate void BTransportOnCloseCallback(BTransport pTransport);
    public delegate void BTransportOnErrorCallback(BTransport pTransport);

    // When setup to listen for a network connection, this is called when a new connection is received
    public delegate void BTransportConnectionAcceptedProcessor(BTransport pTransport,
                                                    CancellationTokenSource pCanceller);

    public abstract class BTransport {
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
        public event BTransportOnMsgCallback OnMsg;
        public event BTransportOnCloseCallback OnClose;
        public event BTransportOnErrorCallback OnError;

        // The "OnMsg" callback has an extra parameter that the caller can
        //    use to keep context. This presumes only one caller.
        protected object _receptionCallbackContext;
        public object ReceptionCallbackContext {
            set { _receptionCallbackContext = value; }
        }

        protected BlockingCollection<byte[]> _receiveQueue;
        protected BlockingCollection<byte[]> _sendQueue;

        protected readonly BLogger _log;

        public string ConnectionName = "UNKNOWN";

        public BTransport(BLogger pLog) {
            _log = pLog;
            if (_log == null) {
                throw new Exception("BTransportWS.constructor: logger parameter null");
            }

            _connectionState = BTransportConnectionStates.INITIALIZING;
            _receiveQueue = new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>());
            _sendQueue = new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>());
        }

        public virtual void Start(ParamBlock pParams) {
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
        protected virtual void OnStateChanged() {
            BTransportOnStateChangeCallback cb = OnStateChange;
            cb?.Invoke(this, _connectionState, _receptionCallbackContext);
        }
        protected virtual void OnOpened() {
            BTransportOnOpenCallback cb = OnOpen;
            cb?.Invoke(this);
        }
        protected virtual void OnClosed() {
            BTransportOnCloseCallback cb = OnClose;
            cb?.Invoke(this);
        }
        protected virtual void OnMsged(byte[] pMsg) {
            BTransportOnMsgCallback cb = OnMsg;
            cb?.Invoke(this, pMsg, _receptionCallbackContext);
        }
        protected virtual void OnErrored() {
            BTransportOnErrorCallback cb = OnError;
            cb?.Invoke(this);
        }

    }
}
