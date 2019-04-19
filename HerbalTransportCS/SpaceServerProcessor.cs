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
using SpaceServer = org.herbal3d.basil.protocol.SpaceServer;

namespace org.herbal3d.transport {
    public interface ISpaceServer {
        SpaceServer.OpenSessionResp OpenSession(SpaceServer.OpenSessionReq pReq);
        SpaceServer.CloseSessionResp CloseSession(SpaceServer.CloseSessionReq pReq);
        SpaceServer.CameraViewResp CameraView(SpaceServer.CameraViewReq pReq);
    }

    // Message Basil might send to us as a SpaceServer.
    public class SpaceServerProcessor : MsgProcessor {
        // private static readonly string _logHeader = "[SpaceServerProcessor]";

        // Must be set by eventually created SpaceServer for it to receive messages
        public ISpaceServer SpaceServerMsgHandler;

        public SpaceServerProcessor(BasilConnection pConnection, TransportContext pContext)
                            : base(pConnection, pContext) {
            // Add processors for message ops
            BasilConnection.Processors processors = new BasilConnection.Processors {
                { (Int32)BasilMessage.BasilMessageOps.OpenSessionReq, this.WrapOpenSession },
                { (Int32)BasilMessage.BasilMessageOps.CloseSessionReq, this.WrapCloseSession },
                { (Int32)BasilMessage.BasilMessageOps.CameraViewReq, this.WrapCameraView }
            };
            Connection.AddMessageProcessors(processors);
        }

        // Wrap the SpaceServer functions to hide the BasilMessage streaming hack (needed
        //    for WebSockets) so that the Ragu internal code looks more like an RPC
        //    processor for the defined message types.

        private BasilMessage.BasilMessage WrapOpenSession(BasilMessage.BasilMessage pReq) {
            _context.Log.DebugFormat("[SpaceServerProcessor] WrapOpenSession");
            BasilMessage.BasilMessage pResp = new BasilMessage.BasilMessage();

            SpaceServer.OpenSessionReq ssReq = new SpaceServer.OpenSessionReq() {
                Auth = pReq.Auth,
            };
            if (pReq.Properties != null && pReq.Properties.Count > 0) {
                ssReq.Features.Add(pReq.Properties);
            }
            if (SpaceServerMsgHandler != null) {
                SpaceServer.OpenSessionResp ssResp = SpaceServerMsgHandler.OpenSession(ssReq);
                pResp.Exception = ssResp.Exception;
                if (ssResp.Properties != null && ssResp.Properties.Count > 0) {
                    pResp.Properties.Add(ssResp.Properties);
                }
            }
            else {
                pResp.Exception = new BasilType.BasilException() {
                    Reason = "Connection not initialized"
                };
            }
            MsgProcessor.MakeMessageAResponse(ref pResp, pReq);
            return pResp;
        }

        private BasilMessage.BasilMessage WrapCloseSession(BasilMessage.BasilMessage pReq) {
            BasilMessage.BasilMessage pResp = new BasilMessage.BasilMessage();

            SpaceServer.CloseSessionReq ssReq = new SpaceServer.CloseSessionReq() {
                Auth = pReq.Auth
            };
            if (pReq.Properties != null && pReq.Properties.ContainsKey("Reason")) {
                ssReq.Reason = pReq.Properties["Reason"];
            }
            if (SpaceServerMsgHandler != null) {
                SpaceServer.CloseSessionResp ssResp = SpaceServerMsgHandler.CloseSession(ssReq);
                pResp.Exception = ssResp.Exception;
            }
            else {
                pResp.Exception = new BasilType.BasilException() {
                    Reason = "Connection not initialized"
                };
            }
            MsgProcessor.MakeMessageAResponse(ref pResp, pReq);
            return pResp;
        }

        private BasilMessage.BasilMessage WrapCameraView(BasilMessage.BasilMessage pReq) {
            BasilMessage.BasilMessage pResp = new BasilMessage.BasilMessage();

            SpaceServer.CameraViewReq ssReq = new SpaceServer.CameraViewReq() {
                Auth = pReq.Auth
            };
            if (SpaceServerMsgHandler != null) {
                SpaceServer.CameraViewResp ssResp = SpaceServerMsgHandler.CameraView(ssReq);
                pResp.Exception = ssResp.Exception;
            }
            else {
                pResp.Exception = new BasilType.BasilException() {
                    Reason = "Connection not initialized"
                };
            }
            MsgProcessor.MakeMessageAResponse(ref pResp, pReq);
            return pResp;
        }
    }
}
