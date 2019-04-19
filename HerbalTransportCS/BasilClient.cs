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
using System.IO;
using System.Threading.Tasks;

using BasilType = org.herbal3d.basil.protocol.BasilType;
using BasilMessage = org.herbal3d.basil.protocol.Message;
using Basil = org.herbal3d.basil.protocol.BasilServer;

namespace org.herbal3d.transport {
    public class BasilClient  {

        public readonly BasilConnection Connection;

        public BasilClient(BasilConnection pBasilConnection) {
            Connection = pBasilConnection;
        }

        public async Task<Basil.IdentifyDisplayableObjectResp> IdentifyDisplayableObjectAsync(
                        BasilType.AccessAuthorization pAuth,
                        BasilType.AssetInformation pAsset,
                        BasilType.AaBoundingBox pAabb) {
            var req = new BasilMessage.BasilMessage() {
                Op = (Int32)BasilMessage.BasilMessageOps.IdentifyDisplayableObjectReq,
                Auth = pAuth,
                AssetInfo = pAsset,
                Aabb = pAabb
            };
            BasilMessage.BasilMessage resp = await Connection.BasilClientProcessor.SendAndAwaitResponse(req);
            return new Basil.IdentifyDisplayableObjectResp() {
                Exception = resp.Exception,
                ObjectId = resp.ObjectId
            };
        }

        public async Task<Basil.ForgetDisplayableObjectResp> ForgetDisplayableObjectAsync(
                        BasilType.AccessAuthorization pAuth,
                        BasilType.ObjectIdentifier pId) {
            var req = new BasilMessage.BasilMessage() {
                Op = (Int32)BasilMessage.BasilMessageOps.ForgetDisplayableObjectReq,
                Auth = pAuth,
                ObjectId = pId
            };
            BasilMessage.BasilMessage resp = await Connection.BasilClientProcessor.SendAndAwaitResponse(req);
            return new Basil.ForgetDisplayableObjectResp() {
                Exception = resp.Exception
            };
        }

        public async Task<Basil.CreateObjectInstanceResp> CreateObjectInstanceAsync(
                        BasilType.AccessAuthorization pAuth,
                        BasilType.ObjectIdentifier pId,
                        BasilType.InstancePositionInfo pInstancePositionInfo) {
            var req = new BasilMessage.BasilMessage {
                Op = (Int32)BasilMessage.BasilMessageOps.CreateObjectInstanceReq,
                Auth = pAuth,
                ObjectId = pId,
                Pos = pInstancePositionInfo,
            };
            BasilMessage.BasilMessage resp = await Connection.BasilClientProcessor.SendAndAwaitResponse(req);
            return new Basil.CreateObjectInstanceResp() {
                Exception = resp.Exception,
                InstanceId = resp.InstanceId
            };
        }

        public async Task<Basil.DeleteObjectInstanceResp> DeleteObjectInstanceAsync(
                        BasilType.AccessAuthorization pAuth,
                        BasilType.InstanceIdentifier pId) {
            var req = new BasilMessage.BasilMessage {
                Op = (Int32)BasilMessage.BasilMessageOps.DeleteObjectInstanceReq,
                Auth = pAuth,
                InstanceId = pId
            };
            BasilMessage.BasilMessage resp = await Connection.BasilClientProcessor.SendAndAwaitResponse(req);
            return new Basil.DeleteObjectInstanceResp() {
                Exception = resp.Exception
            };
        }

        public async Task<Basil.UpdateInstancePositionResp> UpdateObjectPropertyAsync(
                        BasilType.AccessAuthorization pAuth,
                        BasilType.ObjectIdentifier pId,
                        Dictionary<string,string> pPropertyList) {
            var req = new BasilMessage.BasilMessage {
                Op = (Int32)BasilMessage.BasilMessageOps.UpdateObjectPropertyReq,
                Auth = pAuth,
                ObjectId = pId
            };
            req.Properties.Add(pPropertyList);
            BasilMessage.BasilMessage resp = await Connection.BasilClientProcessor.SendAndAwaitResponse(req);
            return new Basil.UpdateInstancePositionResp() {
                Exception = resp.Exception
            };
        }

        public async Task<Basil.UpdateInstancePropertyResp> UpdateInstancePropertyAsync(
                        BasilType.AccessAuthorization pAuth,
                        BasilType.InstanceIdentifier pId,
                        Dictionary<string,string> pPropertyList) {
            var req = new BasilMessage.BasilMessage {
                Op = (Int32)BasilMessage.BasilMessageOps.UpdateInstancePropertyReq,
                Auth = pAuth,
                InstanceId = pId
            };
            req.Properties.Add(pPropertyList);
            BasilMessage.BasilMessage resp = await Connection.BasilClientProcessor.SendAndAwaitResponse(req);
            return new Basil.UpdateInstancePropertyResp() {
                Exception = resp.Exception
            };
        }
        public async Task<Basil.UpdateInstancePositionResp> UpdateInstancePositionAsync(
                        BasilType.AccessAuthorization pAuth,
                        BasilType.InstanceIdentifier pId,
                        BasilType.InstancePositionInfo pInstancePositionInfo) {
            var req = new BasilMessage.BasilMessage {
                Op = (Int32)BasilMessage.BasilMessageOps.UpdateInstancePositionReq,
                Auth = pAuth,
                InstanceId = pId,
                Pos = pInstancePositionInfo
            };
            BasilMessage.BasilMessage resp = await Connection.BasilClientProcessor.SendAndAwaitResponse(req);
            return new Basil.UpdateInstancePositionResp() {
                Exception = resp.Exception
            };
        }
        public async Task<Basil.RequestObjectPropertiesResp> RequestObjectPropertiesAsync(
                        BasilType.AccessAuthorization pAuth,
                        BasilType.ObjectIdentifier pId,
                        string pPropertyMatch) {
            var req = new BasilMessage.BasilMessage {
                Op = (Int32)BasilMessage.BasilMessageOps.RequestObjectPropertiesReq,
                Auth = pAuth,
                ObjectId = pId,
                Filter = pPropertyMatch
            };
            BasilMessage.BasilMessage resp = await Connection.BasilClientProcessor.SendAndAwaitResponse(req);
            var ret = new Basil.RequestObjectPropertiesResp() {
                Exception = resp.Exception
            };
            ret.Properties.Add(resp.Properties);
            return ret;
        }

        public async Task<Basil.RequestInstancePropertiesResp> RequestInstancePropertiesAsync(
                        BasilType.AccessAuthorization pAuth,
                        BasilType.InstanceIdentifier pId,
                        string pPropertyMatch) {
            var req = new BasilMessage.BasilMessage {
                Op = (Int32)BasilMessage.BasilMessageOps.RequestInstancePropertiesReq,
                Auth = pAuth,
                InstanceId = pId,
                Filter = pPropertyMatch
            };
            BasilMessage.BasilMessage resp = await Connection.BasilClientProcessor.SendAndAwaitResponse(req);
            var ret = new Basil.RequestInstancePropertiesResp() {
                Exception = resp.Exception
            };
            ret.Properties.Add(resp.Properties);
            return ret;
        }

        // RESOURCE MANAGEMENT =========================================

        // CONNECTION MANAGEMENT =======================================

        public async Task<Basil.CloseSessionResp> CloseSessionAsync(
                        BasilType.AccessAuthorization pAuth,
                        string pReason) {
            var req = new BasilMessage.BasilMessage {
                Op = (Int32)BasilMessage.BasilMessageOps.CloseSessionReq,
                Auth = pAuth
            };
            req.OpParameters.Add("reason", pReason);
            BasilMessage.BasilMessage resp = await Connection.BasilClientProcessor.SendAndAwaitResponse(req);
            return new Basil.CloseSessionResp() {
                Exception = resp.Exception
            };
        }

        public async Task<Basil.MakeConnectionResp> MakeConnectionAsync(
                        BasilType.AccessAuthorization pAuth,
                        Dictionary<string,string> pConnectionParams) {
            var req = new BasilMessage.BasilMessage {
                Op = (Int32)BasilMessage.BasilMessageOps.MakeConnectionReq,
                Auth = pAuth,
            };
            req.Properties.Add(pConnectionParams);
            BasilMessage.BasilMessage resp = await Connection.BasilClientProcessor.SendAndAwaitResponse(req);
            return new Basil.MakeConnectionResp() {
                Exception = resp.Exception
            };
        }
    }
}
