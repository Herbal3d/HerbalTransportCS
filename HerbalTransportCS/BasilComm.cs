// Copyright (c) 2020 Robert Adams
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

using BasilMessage = org.herbal3d.basil.protocol.Message;
using BasilType = org.herbal3d.basil.protocol.BasilType;

namespace org.herbal3d.transport {
    public class BasilComm : MsgProcessor {

        // Must be set by eventually created SpaceServer for it to receive messages
        public ISpaceServer SpaceServerMsgHandler;

        public BasilComm(BasilConnection pConnection, TransportContext pContext) : base(pConnection, pContext) {

            // Add processors for message ops
            BasilConnection.Processors processors = new BasilConnection.Processors {
                { (Int32)BasilMessage.BasilMessageOps.CreateItemReq, this.ProcCreateItemReq },
                { (Int32)BasilMessage.BasilMessageOps.CreateItemResp, this.HandleResponse },
                { (Int32)BasilMessage.BasilMessageOps.DeleteItemReq, this.ProcDeleteItemReq },
                { (Int32)BasilMessage.BasilMessageOps.DeleteItemResp, this.HandleResponse },
                { (Int32)BasilMessage.BasilMessageOps.AddAbilityReq, this.ProcAddAbilityReq },
                { (Int32)BasilMessage.BasilMessageOps.AddAbilityResp, this.HandleResponse },
                { (Int32)BasilMessage.BasilMessageOps.RemoveAbilityReq, this.ProcRemoveAbilityReq },
                { (Int32)BasilMessage.BasilMessageOps.RemoveAbilityResp, this.HandleResponse },
                { (Int32)BasilMessage.BasilMessageOps.RequestPropertiesReq, this.ProcRequestPropertiesReq },
                { (Int32)BasilMessage.BasilMessageOps.RequestPropertiesResp, this.HandleResponse },
                { (Int32)BasilMessage.BasilMessageOps.UpdatePropertiesReq, this.ProcUpdatePropertiesReq },
                { (Int32)BasilMessage.BasilMessageOps.UpdatePropertiesResp, this.HandleResponse },
                { (Int32)BasilMessage.BasilMessageOps.OpenSessionReq, this.ProcOpenSessionReq },
                { (Int32)BasilMessage.BasilMessageOps.OpenSessionResp, this.HandleResponse },
                { (Int32)BasilMessage.BasilMessageOps.CloseSessionReq, this.ProcCloseSessionReq },
                { (Int32)BasilMessage.BasilMessageOps.CloseSessionResp, this.HandleResponse },
                { (Int32)BasilMessage.BasilMessageOps.MakeConnectionReq, this.ProcMakeConnectionReq },
                { (Int32)BasilMessage.BasilMessageOps.MakeConnectionResp, this.HandleResponse },
                { (Int32)BasilMessage.BasilMessageOps.AliveCheckReq, this.ProcAliveCheckReq },
                { (Int32)BasilMessage.BasilMessageOps.AliveCheckResp, this.HandleResponse }
            };
            Connection.AddMessageProcessors(processors);
        }

        public Task<BasilMessage.BasilMessage> CreateItemAsync(
                        BasilType.Auth pAuth, BasilType.Props pProps, BasilType.Abilities pAbilities) {
            BasilMessage.BasilMessage ret = new BasilMessage.BasilMessage() {
                Op = (uint)BasilMessage.BasilMessageOps.CreateItemReq,
                SessionAuth = pAuth.ToString()
            };
            return this.SendAndAwaitResponse(ret);
        }
        private BasilMessage.BasilMessage ProcCreateItemReq(BasilMessage.BasilMessage pReq) {
            BasilMessage.BasilMessage ret = new BasilMessage.BasilMessage() {
                Op = (uint)BasilMessage.BasilMessageOps.CreateItemResp
            };
            return ret;
        }
        public Task<BasilMessage.BasilMessage> DeleteItemAsync(
                        BasilType.Auth pAuth, BasilType.ItemId pItem) {
            BasilMessage.BasilMessage ret = new BasilMessage.BasilMessage() {
                Op = (uint)BasilMessage.BasilMessageOps.DeleteItemReq,
                SessionAuth = pAuth.ToString()
            };
            return this.SendAndAwaitResponse(ret);
        }
        private BasilMessage.BasilMessage ProcDeleteItemReq(BasilMessage.BasilMessage pReq) {
            BasilMessage.BasilMessage ret = new BasilMessage.BasilMessage() {
                Op = (uint)BasilMessage.BasilMessageOps.DeleteItemResp
            };
            return ret;
        }
        public Task<BasilMessage.BasilMessage> AddAbilityAsync(
                        BasilType.Auth pAuth, BasilType.ItemId pId, BasilType.Abilities pAbilities) {
            BasilMessage.BasilMessage ret = new BasilMessage.BasilMessage() {
                Op = (uint)BasilMessage.BasilMessageOps.AddAbilityReq,
                SessionAuth = pAuth.ToString()
            };
            return this.SendAndAwaitResponse(ret);
        }
        private BasilMessage.BasilMessage ProcAddAbilityReq(BasilMessage.BasilMessage pReq) {
            BasilMessage.BasilMessage ret = new BasilMessage.BasilMessage() {
                Op = (uint)BasilMessage.BasilMessageOps.AddAbilityResp
            };
            return ret;
        }
        public Task<BasilMessage.BasilMessage> RemoveAbilityAsync(
                        BasilType.Auth pAuth, BasilType.ItemId pId, BasilType.Abilities pAbilities) {
            BasilMessage.BasilMessage ret = new BasilMessage.BasilMessage() {
                Op = (uint)BasilMessage.BasilMessageOps.RemoveAbilityReq,
                SessionAuth = pAuth.ToString()
            };
            return this.SendAndAwaitResponse(ret);
        }
        private BasilMessage.BasilMessage ProcRemoveAbilityReq(BasilMessage.BasilMessage pReq) {
            BasilMessage.BasilMessage ret = new BasilMessage.BasilMessage() {
                Op = (uint)BasilMessage.BasilMessageOps.RemoveAbilityResp
            };
            return ret;
        }
        public Task<BasilMessage.BasilMessage> RequestPropertiesAsync(
                        BasilType.Auth pAuth, BasilType.ItemId pId, BasilType.Props pProps) {
            BasilMessage.BasilMessage ret = new BasilMessage.BasilMessage() {
                Op = (uint)BasilMessage.BasilMessageOps.RequestPropertiesReq,
                SessionAuth = pAuth.ToString()
            };
            return this.SendAndAwaitResponse(ret);
        }
        private BasilMessage.BasilMessage ProcRequestPropertiesReq(BasilMessage.BasilMessage pReq) {
            BasilMessage.BasilMessage ret = new BasilMessage.BasilMessage() {
                Op = (uint)BasilMessage.BasilMessageOps.RequestPropertiesResp
            };
            return ret;
        }
        public Task<BasilMessage.BasilMessage> UpdatePropertiesAsync(
                        BasilType.Auth pAuth, BasilType.ItemId pId, BasilType.Props pProps) {
            BasilMessage.BasilMessage ret = new BasilMessage.BasilMessage() {
                Op = (uint)BasilMessage.BasilMessageOps.UpdatePropertiesReq,
                SessionAuth = pAuth.ToString()
            };
            return this.SendAndAwaitResponse(ret);
        }
        private BasilMessage.BasilMessage ProcUpdatePropertiesReq(BasilMessage.BasilMessage pReq) {
            BasilMessage.BasilMessage ret = new BasilMessage.BasilMessage() {
                Op = (uint)BasilMessage.BasilMessageOps.UpdatePropertiesResp
            };
            return ret;
        }
        public Task<BasilMessage.BasilMessage> OpenSessionAsync(
                        BasilType.Auth pAuth, BasilType.Props pProps) {
            BasilMessage.BasilMessage ret = new BasilMessage.BasilMessage() {
                Op = (uint)BasilMessage.BasilMessageOps.OpenSessionReq,
                SessionAuth = pAuth.ToString()
            };
            return this.SendAndAwaitResponse(ret);
        }
        private BasilMessage.BasilMessage ProcOpenSessionReq(BasilMessage.BasilMessage pReq) {
            BasilMessage.BasilMessage ret = new BasilMessage.BasilMessage() {
                Op = (uint)BasilMessage.BasilMessageOps.OpenSessionResp
            };

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
            return ret;
        }
        public Task<BasilMessage.BasilMessage> CloseSessionAsync(
                        BasilType.Auth pAuth, BasilType.Props pProps) {
            BasilMessage.BasilMessage ret = new BasilMessage.BasilMessage() {
                Op = (uint)BasilMessage.BasilMessageOps.CloseSessionReq,
                SessionAuth = pAuth.ToString()
            };
            return this.SendAndAwaitResponse(ret);
        }
        private BasilMessage.BasilMessage ProcCloseSessionReq(BasilMessage.BasilMessage pReq) {
            BasilMessage.BasilMessage pResp = new BasilMessage.BasilMessage() {
                Op = (Int32)BasilMessage.BasilMessageOps.CloseSessionResp
            };

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
            return ret;
        }
        public Task<BasilMessage.BasilMessage> MakeConnectionAsync(
                        BasilType.Auth pAuth, BasilType.Props pProps) {
            BasilMessage.BasilMessage ret = new BasilMessage.BasilMessage() {
                Op = (uint)BasilMessage.BasilMessageOps.MakeConnectionReq,
                SessionAuth = pAuth.ToString()
            };
            return this.SendAndAwaitResponse(ret);
        }
        private BasilMessage.BasilMessage ProcMakeConnectionReq(BasilMessage.BasilMessage pReq) {
            BasilMessage.BasilMessage ret = new BasilMessage.BasilMessage() {
                Op = (uint)BasilMessage.BasilMessageOps.MakeConnectionResp
            };
            return ret;
        }

        public Task<BasilMessage.BasilMessage> AliveCheckAsync( BasilType.Auth pAuth) {
            BasilMessage.BasilMessage req = MakeAliveCheckReq(pAuth);
            return this.SendAndAwaitResponse(req);
        }

        // Send an AliveCheck request without expecting a response
        public void AliveCheck(BasilType.Auth pAuth) {
            BasilMessage.BasilMessage req = MakeAliveCheckReq(pAuth);
            this.SendMessage(req, null);
        }

        static UInt64 _AliveSequenceNumber = 1;
        private BasilMessage.BasilMessage MakeAliveCheckReq( BasilType.Auth pAuth) {
            BasilMessage.BasilMessage ret = new BasilMessage.BasilMessage() {
                SessionAuth = pAuth.ToString()
            };
            ret.IProps.Add("time", DateTime.UtcNow.ToString());
            ret.IProps.Add("sequenceNum", (_AliveSequenceNumber++).ToString());
            return ret;
        }

        private BasilMessage.BasilMessage ProcAliveCheckReq( BasilMessage.BasilMessage pReq) {
            BasilMessage.BasilMessage ret = new BasilMessage.BasilMessage() {
                Op = (uint)BasilMessage.BasilMessageOps.AliveCheckResp
            };
            ret.IProps.Add("time", DateTime.UtcNow.ToString());
            ret.IProps.Add("sequenceNum", (_AliveSequenceNumber++).ToString());
            if (pReq.IProps != null) {
                if (pReq.IProps.ContainsKey("time")) {
                    ret.IProps.Add("timeReceived", (string)pReq.IProps["time"]);
                }
                else {
                    ret.IProps.Add("timeReceived", "0");
                }
                if (pReq.IProps.ContainsKey("sequenceNum")) {
                    ret.IProps.Add("sequenceNumReceived", (string)pReq.IProps["sequenceNum"]);
                }
                else {
                    ret.IProps.Add("sequenceNumReceived", "0");
                }
            }
            return ret;
        }
    }
}
