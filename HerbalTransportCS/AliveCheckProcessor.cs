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
using System.Text;
using System.Threading.Tasks;

using BasilType = org.herbal3d.basil.protocol.BasilType;
using BasilMessage = org.herbal3d.basil.protocol.Message;

namespace org.herbal3d.transport {
    // Message processor for the keep alive handshake.
    public class AliveCheckProcessor : MsgProcessor {

        private int _AliveSequenceNumber = 111;

        public AliveCheckProcessor(BasilConnection pConnection, TransportContext pContext)
                            : base(pConnection, pContext) {
            // Add processors for message ops
            BasilConnection.Processors processors = new BasilConnection.Processors {
                { (Int32)BasilMessage.BasilMessageOps.AliveCheckReq, this.ProcAliveCheckReq },
                { (Int32)BasilMessage.BasilMessageOps.AliveCheckResp, this.HandleResponse }
            };
            Connection.AddMessageProcessors(processors);
        }

        public async Task<BasilMessage.BasilMessage> AliveCheckAsync(
                        BasilType.AccessAuthorization pAuth) {
            BasilMessage.BasilMessage req = MakeAliveCheckReq(pAuth);
            return await this.SendAndAwaitResponse(req);
        }

        // Send an AliveCheck request without expecting a response
        public void AliveCheck(BasilType.AccessAuthorization pAuth) {
            BasilMessage.BasilMessage req = MakeAliveCheckReq(pAuth);
            this.SendMessage(req, null);
        }

        private BasilMessage.BasilMessage MakeAliveCheckReq(
                        BasilType.AccessAuthorization pAuth) {
            BasilMessage.BasilMessage ret = new BasilMessage.BasilMessage() {
                Auth = pAuth
            };
            ret.OpParameters.Add("time", DateTime.UtcNow.ToString());
            ret.OpParameters.Add("sequenceNum", (_AliveSequenceNumber++).ToString());
            return ret;
        }

        private BasilMessage.BasilMessage ProcAliveCheckReq(
                        BasilMessage.BasilMessage pReq) {
            BasilMessage.BasilMessage ret = new BasilMessage.BasilMessage() {
                Op = Connection.BasilMessageOpByName["AliveCheckResp"]
            };
            ret.OpParameters.Add("time", DateTime.UtcNow.ToString());
            ret.OpParameters.Add("sequenceNum", (_AliveSequenceNumber++).ToString());
            if (pReq.OpParameters != null) {
                if (pReq.OpParameters.ContainsKey("time")) {
                    ret.OpParameters.Add("timeReceived", (string)pReq.OpParameters["time"]);
                }
                else {
                    ret.OpParameters.Add("timeReceived", "0");
                }
                if (pReq.OpParameters.ContainsKey("sequenceNum")) {
                    ret.OpParameters.Add("sequenceNumReceived", (string)pReq.OpParameters["sequenceNum"]);
                }
                else {
                    ret.OpParameters.Add("sequenceNumReceived", "0");
                }
            }
            return ret;
        }
    }
}
