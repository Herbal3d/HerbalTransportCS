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
using System.Threading;
using System.Threading.Tasks;

using org.herbal3d.OSAuth;

using HT = org.herbal3d.transport;
using BM = org.herbal3d.basil.protocol.Message;
using BT = org.herbal3d.basil.protocol.BasilType;

namespace org.herbal3d.transport {
    public class AliveCheck : IDisposable {
        private readonly CancellationTokenSource _canceller;
        private HT.BasilConnection ClientConnection;
        public OSAuthToken ClientAuth;

        public AliveCheck(CancellationTokenSource pCanceller, HT.BasilConnection pBasilConnection) {

            _canceller = pCanceller;
            ClientConnection = pBasilConnection;

            HT.BasilConnection.Processors processors = new HT.BasilConnection.Processors {
                { (Int32)BM.BasilMessageOps.AliveCheckReq, this.ProcAliveCheckReq },
                { (Int32)BM.BasilMessageOps.AliveCheckResp, ClientConnection.HandleResponse }
            };
            ClientConnection.AddMessageProcessors(processors);
        }

        public void Shutdown() {
            if (ClientConnection != null) {
                ClientConnection.Shutdown();
                ClientConnection = null;
            }
        }

        public void Dispose() {
            Shutdown();
        }

        public async Task<BT.Props> AliveCheckAsync(OSAuthToken pAuth) {
            BM.BasilMessage req = MakeAliveCheckReq(pAuth);
            BM.BasilMessage resp = await ClientConnection.SendAndAwaitResponse(req);
            if (!String.IsNullOrEmpty(resp.Exception)) {
                throw new BasilException(resp.Exception);
            }
            return new BT.Props();
        }

        // Send an AliveCheck request without expecting a response
        public void AliveCheckNoResponse(OSAuthToken pAuth) {
            BM.BasilMessage req = MakeAliveCheckReq(pAuth);
            ClientConnection.SendMessage(req, null);
        }

        static UInt64 _AliveSequenceNumber = 1;
        private BM.BasilMessage MakeAliveCheckReq(OSAuthToken pAuth) {
            BM.BasilMessage ret = new BM.BasilMessage() {
                Op = (Int32)BM.BasilMessageOps.AliveCheckResp,
                SessionAuth = ClientAuth.ToString()
            };
            ret.IProps.Add("time", DateTime.UtcNow.ToString());
            ret.IProps.Add("sequenceNum", (_AliveSequenceNumber++).ToString());
            return ret;
        }

        private BM.BasilMessage ProcAliveCheckReq(BM.BasilMessage pReq) {
            BM.BasilMessage resp = new BM.BasilMessage() {
                Op = (uint)BM.BasilMessageOps.AliveCheckResp
            };
            resp.IProps.Add("time", DateTime.UtcNow.ToString());
            resp.IProps.Add("sequenceNum", (_AliveSequenceNumber++).ToString());
            if (pReq.IProps != null) {
                if (pReq.IProps.ContainsKey("time")) {
                    resp.IProps.Add("timeReceived", (string)pReq.IProps["time"]);
                }
                else {
                    resp.IProps.Add("timeReceived", "0");
                }
                if (pReq.IProps.ContainsKey("sequenceNum")) {
                    resp.IProps.Add("sequenceNumReceived", (string)pReq.IProps["sequenceNum"]);
                }
                else {
                    resp.IProps.Add("sequenceNumReceived", "0");
                }
            }
            return resp;
        }
    }
}
