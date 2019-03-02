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

    public interface ISpaceServer {
        BasilMessage.BasilMessage OpenSession(BasilMessage.BasilMessage pReq);
        BasilMessage.BasilMessage CloseSession(BasilMessage.BasilMessage pReq);
        BasilMessage.BasilMessage CameraView(BasilMessage.BasilMessage pReq);
    }

    public class SpaceServerSample : ISpaceServer {

        private BasilConnection _basilConnection;

        public SpaceServerSample(BasilConnection pConnection) {
            _basilConnection = pConnection;
        }

        public BasilMessage.BasilMessage OpenSession(BasilMessage.BasilMessage pReq) {
            BasilMessage.BasilMessage respMsg = new BasilMessage.BasilMessage {
                Op = _basilConnection.BasilMessageOpByName["OpenSessionResp"]
            };
            MsgProcessor.MakeMessageAResponse(ref respMsg, pReq);
            return respMsg;
        }

        public BasilMessage.BasilMessage CloseSession(BasilMessage.BasilMessage pReq) {
            BasilMessage.BasilMessage respMsg = new BasilMessage.BasilMessage {
                Op = _basilConnection.BasilMessageOpByName["CloseSessionResp"]
            };
            MsgProcessor.MakeMessageAResponse(ref respMsg, pReq);
            return respMsg;
        }

        public BasilMessage.BasilMessage CameraView(BasilMessage.BasilMessage pReq) {
            BasilMessage.BasilMessage respMsg = new BasilMessage.BasilMessage {
                Op = _basilConnection.BasilMessageOpByName["CameraViewResp"]
            };
            MsgProcessor.MakeMessageAResponse(ref respMsg, pReq);
            return respMsg;
        }
    }
}
