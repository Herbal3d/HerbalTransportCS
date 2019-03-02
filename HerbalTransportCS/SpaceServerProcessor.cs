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
    // Message Basil might send to us as a SpaceServer.
    public class SpaceServerProcessor : MsgProcessor {
        // private static readonly string _logHeader = "[SpaceServerProcessor]";
        public readonly ISpaceServer Server;

        public SpaceServerProcessor(BasilConnection pConnection, TransportContext pContext)
                            : base(pConnection, pContext) {
            // Add processors for message ops
            Server = _context.NeedSpaceServer(pConnection);
            BasilConnection.Processors processors = new BasilConnection.Processors {
                { (Int32)BasilMessage.BasilMessageOps.OpenSessionReq, Server.OpenSession },
                { (Int32)BasilMessage.BasilMessageOps.CloseSessionReq, Server.CloseSession },
                { (Int32)BasilMessage.BasilMessageOps.CameraViewReq, Server.CameraView }
            };
            Connection.AddMessageProcessors(processors);
        }
    }
}
