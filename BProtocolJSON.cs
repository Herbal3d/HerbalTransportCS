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

using org.herbal3d.b.protocol;
using org.herbal3d.cs.CommonUtil;

using Newtonsoft.Json;

namespace org.herbal3d.transport {
    /**
     * Receive a JSON transport body and pass a BMessage up the stack.
     */

    public class BProtocolJSON : BProtocol {

        // format of the JSON that is sent.
        JsonSerializerSettings serializeSettings = new JsonSerializerSettings() {
            // Don't send objects that have a value 'null'
            NullValueHandling = NullValueHandling.Ignore
        };

        public BProtocolJSON(ParamBlock pParams,
                            BTransport pTransport,
                            BLogger pLogger) : base(pTransport, pLogger) {

            // set up to receive messages
            pTransport.OnMsg += BProtocolJSON.ProcessOnMsg;
            pTransport.ReceptionCallbackContext = this;
            pTransport.OnStateChange += BProtocolJSON.ProcessOnStateChange;
        }

        public override void Close() {
            throw new NotImplementedException();
        }

        public override void Send(BMessage pData) {
            // convert the BMessage to JSON buffer
            string asJSON = JsonConvert.SerializeObject(pData, serializeSettings);
            Log.Debug("BProtocolJSON.Send: Sending {0}", asJSON);
            Transport?.Send(Encoding.UTF8.GetBytes(asJSON));
        }

        public override void Start(ParamBlock pParams) {
            throw new NotImplementedException();
        }

        private static void ProcessOnStateChange(BTransport pTransport, BTransportConnectionStates pState, object pContext) {
            BProtocolJSON caller = pContext as BProtocolJSON;
            caller.Log.Debug("BProtocolJSON.ProcessOnStateChange: ");
        }

        private static void ProcessOnMsg(BTransport pTransport, byte[] pData, object pContext) {
            BProtocolJSON caller = pContext as BProtocolJSON;

            BMessage bmsg;

            // convert received buffer from JSON into a BMessage
            try {
                var stringData = System.Text.Encoding.UTF8.GetString(pData);
                bmsg = JsonConvert.DeserializeObject<BMessage>(stringData);
                caller.Log.Debug("BProtocolJSON.ProcessOnMsg: received bmsg={0}", bmsg.ToString());
            }
            catch (Exception ee) {
                throw new Exception(String.Format("Failure to parse incoming BMessage: {0}", ee.Message));
            }

            // Give the buffer to our caller
            if (bmsg != null) {
                caller._receptionCallback?.Invoke(bmsg, caller._receptionCallbackContext, caller);
            }
            else {
                caller.Log.Debug("BProtocolJSON.ProcessOnMsg: Received message but not parsed");
            }
        }
    }
}
