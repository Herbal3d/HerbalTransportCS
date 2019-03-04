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

using BasilMessage = org.herbal3d.basil.protocol.Message;

namespace org.herbal3d.transport {
    // Messages we might receive from Basil.
    // Only responses from our requests to the server.
    public class BasilClientProcessor : MsgProcessor {

        public BasilClientProcessor(BasilConnection pConnection, TransportContext pContext)
                                    : base(pConnection, pContext) {

            // Add processors for message ops.
            // Since this is a client talking to a Basil server, everything is a response to our request.
            var processors = new BasilConnection.Processors {
                { (Int32)BasilMessage.BasilMessageOps.IdentifyDisplayableObjectResp, this.HandleResponse },
                { (Int32)BasilMessage.BasilMessageOps.ForgetDisplayableObjectResp, this.HandleResponse },
                { (Int32)BasilMessage.BasilMessageOps.CreateObjectInstanceResp, this.HandleResponse },
                { (Int32)BasilMessage.BasilMessageOps.DeleteObjectInstanceResp, this.HandleResponse },
                { (Int32)BasilMessage.BasilMessageOps.UpdateObjectPropertyResp, this.HandleResponse },
                { (Int32)BasilMessage.BasilMessageOps.UpdateInstancePropertyResp, this.HandleResponse },
                { (Int32)BasilMessage.BasilMessageOps.RequestObjectPropertiesResp, this.HandleResponse },
                { (Int32)BasilMessage.BasilMessageOps.RequestInstancePropertiesResp, this.HandleResponse },
                { (Int32)BasilMessage.BasilMessageOps.CloseSessionResp, this.HandleResponse },
                { (Int32)BasilMessage.BasilMessageOps.MakeConnectionResp, this.HandleResponse }
            };
            Connection.AddMessageProcessors(processors);
        }
    }
}
