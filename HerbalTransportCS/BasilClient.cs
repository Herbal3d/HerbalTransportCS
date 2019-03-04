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

namespace org.herbal3d.transport {
    public class BasilClient  {

        private readonly BasilConnection _basilConnection;
        private readonly TransportContext _context;

        public BasilClient(BasilConnection pBasilConnection, TransportContext pContext) {
            _basilConnection = pBasilConnection;
            _context = pContext;
        }

        public async Task<BasilMessage.BasilMessage> IdentifyDisplayableObjectAsync(
                        BasilType.AccessAuthorization pAuth,
                        BasilType.AssetInformation pAsset,
                        BasilType.AaBoundingBox pAabb) {
            var req = new BasilMessage.BasilMessage() {
                Op = (Int32)BasilMessage.BasilMessageOps.IdentifyDisplayableObjectReq,
                Auth = pAuth,
                AssetInfo = pAsset,
                Aabb = pAabb
            };
            return await _basilConnection.BasilClientProcessor.SendAndAwaitResponse(req);
        }

        public async Task<BasilMessage.BasilMessage> ForgetDisplayableObjectAsync(
                        BasilType.AccessAuthorization pAuth,
                        BasilType.ObjectIdentifier pId) {
            var req = new BasilMessage.BasilMessage() {
                Op = (Int32)BasilMessage.BasilMessageOps.ForgetDisplayableObjectReq,
                Auth = pAuth,
                ObjectId = pId
            };
            return await _basilConnection.BasilClientProcessor.SendAndAwaitResponse(req);
        }

        public async Task<BasilMessage.BasilMessage> CreateObjectInstanceAsync(
                        BasilType.AccessAuthorization pAuth,
                        BasilType.ObjectIdentifier pId,
                        BasilType.InstancePositionInfo pInstancePositionInfo) {
            var req = new BasilMessage.BasilMessage {
                Op = (Int32)BasilMessage.BasilMessageOps.CreateObjectInstanceReq,
                Auth = pAuth,
                ObjectId = pId,
                Pos = pInstancePositionInfo,
            };
            return await _basilConnection.BasilClientProcessor.SendAndAwaitResponse(req);
        }

        public async Task<BasilMessage.BasilMessage> CreateObjectInstanceAsync(
                        BasilType.AccessAuthorization pAuth,
                        BasilType.ObjectIdentifier pId,
                        BasilType.InstancePositionInfo pInstancePositionInfo,
                        Dictionary<string,string> pPropertyList) {
            var req = new BasilMessage.BasilMessage {
                Op = (Int32)BasilMessage.BasilMessageOps.CreateObjectInstanceReq,
                Auth = pAuth,
                ObjectId = pId,
                Pos = pInstancePositionInfo,
            };
            if (pPropertyList != null) {
                req.Properties.Add(pPropertyList);
            }
            return await _basilConnection.BasilClientProcessor.SendAndAwaitResponse(req);
        }

        public async Task<BasilMessage.BasilMessage> DeleteObjectInstanceAsync(
                        BasilType.AccessAuthorization pAuth,
                        BasilType.InstanceIdentifier pId) {
            var req = new BasilMessage.BasilMessage {
                Op = (Int32)BasilMessage.BasilMessageOps.DeleteObjectInstanceReq,
                Auth = pAuth,
                InstanceId = pId
            };
            return await _basilConnection.BasilClientProcessor.SendAndAwaitResponse(req);
        }

        public async Task<BasilMessage.BasilMessage> UpdateObjectPropertyAsync(
                        BasilType.AccessAuthorization pAuth,
                        BasilType.ObjectIdentifier pId,
                        Dictionary<string,string> pPropertyList) {
            var req = new BasilMessage.BasilMessage {
                Op = (Int32)BasilMessage.BasilMessageOps.UpdateObjectPropertyReq,
                Auth = pAuth,
                ObjectId = pId
            };
            req.Properties.Add(pPropertyList);
            return await _basilConnection.BasilClientProcessor.SendAndAwaitResponse(req);
        }

        public async Task<BasilMessage.BasilMessage> UpdateInstancePropertyAsync(
                        BasilType.AccessAuthorization pAuth,
                        BasilType.InstanceIdentifier pId,
                        Dictionary<string,string> pPropertyList) {
            var req = new BasilMessage.BasilMessage {
                Op = (Int32)BasilMessage.BasilMessageOps.UpdateInstancePropertyReq,
                Auth = pAuth,
                InstanceId = pId
            };
            req.Properties.Add(pPropertyList);
            return await _basilConnection.BasilClientProcessor.SendAndAwaitResponse(req);
        }
        public async Task<BasilMessage.BasilMessage> UpdateInstancePositionAsync(
                        BasilType.AccessAuthorization pAuth,
                        BasilType.InstanceIdentifier pId,
                        BasilType.InstancePositionInfo pInstancePositionInfo) {
            var req = new BasilMessage.BasilMessage {
                Op = (Int32)BasilMessage.BasilMessageOps.UpdateInstancePositionReq,
                Auth = pAuth,
                InstanceId = pId,
                Pos = pInstancePositionInfo
            };
            return await _basilConnection.BasilClientProcessor.SendAndAwaitResponse(req);
        }
        public async Task<BasilMessage.BasilMessage> RequestObjectPropertiesAsync(
                        BasilType.AccessAuthorization pAuth,
                        BasilType.ObjectIdentifier pId,
                        string pPropertyMatch) {
            var req = new BasilMessage.BasilMessage {
                Op = (Int32)BasilMessage.BasilMessageOps.RequestObjectPropertiesReq,
                Auth = pAuth,
                ObjectId = pId,
                Filter = pPropertyMatch
            };
            return await _basilConnection.BasilClientProcessor.SendAndAwaitResponse(req);
        }

        public async Task<BasilMessage.BasilMessage> RequestInstancePropertiesAsync(
                        BasilType.AccessAuthorization pAuth,
                        BasilType.InstanceIdentifier pId,
                        string pPropertyMatch) {
            var req = new BasilMessage.BasilMessage {
                Op = (Int32)BasilMessage.BasilMessageOps.RequestInstancePropertiesReq,
                Auth = pAuth,
                InstanceId = pId,
                Filter = pPropertyMatch
            };
            return await _basilConnection.BasilClientProcessor.SendAndAwaitResponse(req);
        }

        // RESOURCE MANAGEMENT =========================================

        // CONNECTION MANAGEMENT =======================================

        public async Task<BasilMessage.BasilMessage> CloseSessionAsync(
                        BasilType.AccessAuthorization pAuth,
                        string pReason) {
            var req = new BasilMessage.BasilMessage {
                Op = (Int32)BasilMessage.BasilMessageOps.CloseSessionReq,
                Auth = pAuth
            };
            req.OpParameters.Add("reason", pReason);
            return await _basilConnection.BasilClientProcessor.SendAndAwaitResponse(req);
        }

        public async Task<BasilMessage.BasilMessage> MakeConnectionAsync(
                        BasilType.AccessAuthorization pAuth,
                        Dictionary<string,string> pConnectionParams) {
            var req = new BasilMessage.BasilMessage {
                Op = (Int32)BasilMessage.BasilMessageOps.MakeConnectionReq,
                Auth = pAuth,
            };
            req.Properties.Add(pConnectionParams);
            return await _basilConnection.BasilClientProcessor.SendAndAwaitResponse(req);
        }
    }
}
