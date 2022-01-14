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
using System.Text.Json;
using System.Threading.Tasks;

using org.herbal3d.b.protocol;
using org.herbal3d.cs.CommonUtil;


namespace org.herbal3d.transport {
    /**
     * Receive a JSON transport body and pass a BMessage up the stack.
     */
    public class BProtocolJSON : BProtocol {
        public BProtocolJSON(ParamBlock pParams, BTransport pTransport) : base(pTransport) {
            pTransport.SetReceiveCallback(BProtocolJSON.ProcessReception, this);
        }

        public override void Close() {
            throw new NotImplementedException();
        }

        public override void Send(BMessage pData) {
            // convert the BMessage to JSON buffer
            string asJSON = JsonSerializer.Serialize(pData);
            _transport?.Send(Encoding.ASCII.GetBytes(asJSON));
        }

        public override void Start(ParamBlock pParams) {
            throw new NotImplementedException();
        }

        private static void ProcessReception(byte[] pData, object pContext, BTransport pTransport) {
            BProtocolJSON caller = (BProtocolJSON)pContext;
            BMessage bmsg;

            // convert received buffer from JSON into a BMessage
            try {
                bmsg = JsonSerializer.Deserialize<BMessage>(pData);
            }
            catch (Exception ee) {
                throw new Exception("Failure to parse incoming BMessage: {0}", ee);
            }

            // Give the buffer to our caller
            if (bmsg != null) {
                caller._receptionCallback?.Invoke(bmsg, caller._receptionCallbackContext, caller);
            }
        }
    }
}
