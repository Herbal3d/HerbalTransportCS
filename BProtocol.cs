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
using System.Threading;
using System.Threading.Tasks;

using org.herbal3d.b.protocol;
using org.herbal3d.cs.CommonUtil;

namespace org.herbal3d.transport {

    // When a message is received, this is called
    public delegate void BProtocolReceptionCallback(BMessage pMsg, object pContext, BProtocol pProtocol);
    public delegate void BProtocolStateChangeCallback(BProtocol pProto, BTransportConnectionStates pNewState, object pContext);

    /**
     * Wrapper for a protocol handler. This sits in the stack and converts
     * received messages into BMessages. There will be version for JSON, FlatBuffer, etc.
     */
    public abstract class BProtocol {

        protected BProtocolReceptionCallback _receptionCallback;
        protected BProtocolStateChangeCallback _stateChangeCallback;
        protected object _receptionCallbackContext;

        public BTransport Transport;
        public string ProtocolType;
        public BLogger Log;

        public BProtocol(BTransport pTransport, string pType, BLogger pLogger) {
            Transport = pTransport;
            ProtocolType = pType;
            Log = pLogger;

            pTransport.OnStateChange += BProtocol.ProcessOnStateChange;
        }

        /*
         * Set the function called when a message is received.
         * The "context" is passed so the called function can have the class instance that asked for the message
         */
        public void SetReceiveCallback(BProtocolReceptionCallback pCallback,
                        BProtocolStateChangeCallback pSCCallback, object pContext) {
            _receptionCallback = pCallback;
            _stateChangeCallback = pSCCallback;
            _receptionCallbackContext = pContext;
        }

        public virtual void Start() {
            Transport.Start();
        }

        public virtual void Close() {
            if (Transport != null) {
                Transport.Close();
                Transport = null;
            }

        }

        public abstract void Send(BMessage pData);

        private static void ProcessOnStateChange(BTransport pTransport, BTransportConnectionStates pState, object pContext) {
            BProtocolJSON caller = pContext as BProtocolJSON;
            if (caller != null) {
                // caller.Log.Debug("BProtocolJSON.ProcessOnStateChange: {0}", pState);
                if (caller._stateChangeCallback != null) {
                    caller._stateChangeCallback(caller, pState, caller._receptionCallbackContext);
                }
            }
        }
    }
}
