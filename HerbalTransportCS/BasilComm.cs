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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BM = org.herbal3d.basil.protocol.Message;
using BT = org.herbal3d.basil.protocol.BasilType;

using org.herbal3d.OSAuth;

namespace org.herbal3d.transport {
    public class BasilComm {

        public BasilConnection Connection;
        public OSAuthToken ClientAuth;

        public BasilComm(BasilConnection pConnection) {

            Connection = pConnection;

            // Add processors for message ops
            BasilConnection.Processors processors = new BasilConnection.Processors {
                { (Int32)BM.BasilMessageOps.CreateItemReq, this.ProcCreateItemReq },
                { (Int32)BM.BasilMessageOps.CreateItemResp, Connection.HandleResponse },
                { (Int32)BM.BasilMessageOps.DeleteItemReq, this.ProcDeleteItemReq },
                { (Int32)BM.BasilMessageOps.DeleteItemResp, Connection.HandleResponse },
                { (Int32)BM.BasilMessageOps.AddAbilityReq, this.ProcAddAbilityReq },
                { (Int32)BM.BasilMessageOps.AddAbilityResp, Connection.HandleResponse },
                { (Int32)BM.BasilMessageOps.RemoveAbilityReq, this.ProcRemoveAbilityReq },
                { (Int32)BM.BasilMessageOps.RemoveAbilityResp, Connection.HandleResponse },
                { (Int32)BM.BasilMessageOps.RequestPropertiesReq, this.ProcRequestPropertiesReq },
                { (Int32)BM.BasilMessageOps.RequestPropertiesResp, Connection.HandleResponse },
                { (Int32)BM.BasilMessageOps.UpdatePropertiesReq, this.ProcUpdatePropertiesReq },
                { (Int32)BM.BasilMessageOps.UpdatePropertiesResp, Connection.HandleResponse },
                { (Int32)BM.BasilMessageOps.MakeConnectionReq, this.ProcMakeConnectionReq },
                { (Int32)BM.BasilMessageOps.MakeConnectionResp, Connection.HandleResponse },
            };
            Connection.AddMessageProcessors(processors);
        }

        public async Task<BT.Props> CreateItemAsync(BT.Props pProps, BT.AbilityList pAbilities) {
            BM.BasilMessage req = new BM.BasilMessage() {
                Op = (uint)BM.BasilMessageOps.CreateItemReq,
                SessionAuth = ClientAuth.ToString()
            };
            AddPropsToIProps(pProps, ref req);
            AddAbilitiesToAProps(pAbilities, ref req);
            BM.BasilMessage resp = await Connection.SendAndAwaitResponse(req);
            return ReturnExceptionOrIProps(resp, "CreateItem");
        }
        private BM.BasilMessage ProcCreateItemReq(BM.BasilMessage pReq) {
            BM.BasilMessage resp = new BM.BasilMessage() {
                Op = (uint)BM.BasilMessageOps.CreateItemResp
            };
            return resp;
        }
        public async Task<BT.Props> DeleteItemAsync(BT.ItemId pItem) {
            BM.BasilMessage req = new BM.BasilMessage() {
                Op = (uint)BM.BasilMessageOps.DeleteItemReq,
                SessionAuth = ClientAuth.ToString(),
                ItemId = pItem.ToString()
            };
            BM.BasilMessage resp = await Connection.SendAndAwaitResponse(req);
            return ReturnExceptionOrIProps(resp, "DeleteItem");
        }
        private BM.BasilMessage ProcDeleteItemReq(BM.BasilMessage pReq) {
            BM.BasilMessage resp = new BM.BasilMessage() {
                Op = (uint)BM.BasilMessageOps.DeleteItemResp
            };
            return resp;
        }
        public async Task<BT.Props> AddAbilityAsync(BT.ItemId pId, BT.AbilityList pAbilities) {
            BM.BasilMessage req = new BM.BasilMessage() {
                Op = (uint)BM.BasilMessageOps.AddAbilityReq,
                SessionAuth = ClientAuth.ToString(),
                ItemId = pId.ToString()
            };
            AddAbilitiesToAProps(pAbilities, ref req);
            BM.BasilMessage resp = await Connection.SendAndAwaitResponse(req);
            return ReturnExceptionOrIProps(resp, "AddAbility");
        }
        private BM.BasilMessage ProcAddAbilityReq(BM.BasilMessage pReq) {
            BM.BasilMessage resp = new BM.BasilMessage() {
                Op = (uint)BM.BasilMessageOps.AddAbilityResp
            };
            return resp;
        }
        public async Task<BT.Props> RemoveAbilityAsync( BT.ItemId pId, BT.AbilityList pAbilities) {
            BM.BasilMessage req = new BM.BasilMessage() {
                Op = (uint)BM.BasilMessageOps.RemoveAbilityReq,
                ItemId = pId.ToString(),
                SessionAuth = ClientAuth.ToString()
            };
            AddAbilitiesToAProps(pAbilities, ref req);
            BM.BasilMessage resp = await Connection.SendAndAwaitResponse(req);
            return ReturnExceptionOrIProps(resp, "RemoveAbility");
        }
        private BM.BasilMessage ProcRemoveAbilityReq(BM.BasilMessage pReq) {
            throw new NotImplementedException("BasilComm.ProcRequestPropertiesReq");
            /*
            BM.BasilMessage resp = new BM.BasilMessage() {
                Op = (uint)BM.BasilMessageOps.RemoveAbilityResp
            };
            return resp;
            */
        }
        public async Task<BT.Props> RequestPropertiesAsync(BT.ItemId pId, BT.Props pProps) {
            BM.BasilMessage req = new BM.BasilMessage() {
                Op = (uint)BM.BasilMessageOps.RequestPropertiesReq,
                ItemId = pId.ToString(),
                SessionAuth = ClientAuth.ToString()
            };
            AddPropsToIProps(pProps, ref req);
            BM.BasilMessage resp = await Connection.SendAndAwaitResponse(req);
            return ReturnExceptionOrIProps(resp, "RequestProperties");
        }
        private BM.BasilMessage ProcRequestPropertiesReq(BM.BasilMessage pReq) {
            throw new NotImplementedException("BasilComm.ProcRequestPropertiesReq");
            /*
            BM.BasilMessage resp = new BM.BasilMessage() {
                Op = (uint)BM.BasilMessageOps.RequestPropertiesResp
            };
            return resp;
            */
        }
        public async Task<BT.Props> UpdatePropertiesAsync(BT.ItemId pId, BT.Props pProps) {
            BM.BasilMessage req = new BM.BasilMessage() {
                Op = (uint)BM.BasilMessageOps.UpdatePropertiesReq,
                SessionAuth = ClientAuth.ToString(),
                ItemId = pId.ToString()
            };
            AddPropsToIProps(pProps, ref req);
            BM.BasilMessage resp = await Connection.SendAndAwaitResponse(req);
            return ReturnExceptionOrIProps(resp, "UpdateProperties");
        }
        private BM.BasilMessage ProcUpdatePropertiesReq(BM.BasilMessage pReq) {
            throw new NotImplementedException("BasilComm.ProcUpdatePropertiesReq");
            /*
            BM.BasilMessage resp = new BM.BasilMessage() {
                Op = (uint)BM.BasilMessageOps.UpdatePropertiesResp
            };
            return resp;
            */
        }
        public async Task<BT.Props> MakeConnectionAsync(BT.Props pProps) {
            BM.BasilMessage req = new BM.BasilMessage() {
                Op = (uint)BM.BasilMessageOps.MakeConnectionReq,
                SessionAuth = ClientAuth.ToString()
            };
            AddPropsToIProps(pProps, ref req);
            BM.BasilMessage resp = await Connection.SendAndAwaitResponse(req);
            return ReturnExceptionOrIProps(resp, "MakeConnection");
        }
        private BM.BasilMessage ProcMakeConnectionReq(BM.BasilMessage pReq) {
            throw new NotImplementedException("BasilComm.ProcMakeConnection");
            /*
            BM.BasilMessage resp = new BM.BasilMessage() {
                Op = (uint)BM.BasilMessageOps.MakeConnectionResp
            };
            return resp;
            */
        }

        // Utility function to copy BT.Props into IProps in passed message
        private void AddPropsToIProps(BT.Props pProps, ref BM.BasilMessage pMsg) {
            if (pProps != null) {
                foreach (var kvp in pProps) {
                    pMsg.IProps.Add(kvp.Key, kvp.Value);
                }
            }
        }
        // Utility function that takes a response and build a Props structure
        //    that contains either Exception information or the contents of IProps.
        private BT.Props ReturnExceptionOrIProps(BM.BasilMessage pMsg, string pSource) {
            BT.Props ret = new BT.Props();

            if (!String.IsNullOrEmpty(pMsg.Exception)) {
                throw new BasilException(pSource + ": " +pMsg.Exception, pMsg.ExceptionHints);
            }
            else {
                if (pMsg.IProps != null && pMsg.IProps.Count > 0) {
                    ret.Add(pMsg.IProps);
                }
            }
            return ret;
        }
        // Utility function that adds the ability ParamBlocks to the AProps collection
        //     in the passed message.
        private void AddAbilitiesToAProps(BT.AbilityList pAbilities, ref BM.BasilMessage pMsg) {
            if (pAbilities != null && pAbilities.Count > 0) {
                foreach (var abil in pAbilities) {
                    BM.ParamBlock pBlock = new BM.ParamBlock() {
                        Ability = abil.AbilityCode
                    };
                    abil.AddToProps(pBlock.Props);
                    pMsg.AProps.Add(pBlock);
                }
            }
        }
    }
}
