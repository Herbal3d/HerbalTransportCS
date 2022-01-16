// Copyright (c) 2021 Robert Adams
//
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
using System.Threading.Tasks;

using org.herbal3d.b.protocol;
using org.herbal3d.cs.CommonUtil;
using org.herbal3d.OSAuth;

namespace org.herbal3d.transport {

    public delegate void ResolutionCallback(BMessage pMsg);
    public delegate void RejectionCallback(string pErrorMsg);
    public class RPCInfo {
        public DateTime timeRPCCreated;
        public ResolutionCallback resolve;
        public RejectionCallback reject;
        public string session;          // unique number used to link response
        public BasilConnection context; // A handle back to the connection instance
        // Task completion object that is holding the context for the reply.
        public TaskCompletionSource<BMessage> taskCompletion;
    }

    // Class of processors called when new messages are received.
    public class IncomingMessageProcessor {
        private readonly object _callersContext;
        public IncomingMessageProcessor(object pContext) {
            _callersContext = pContext;
        }
        // Default processing is to just return an error
        public delegate void ProcessOpCall(BMessage pMsg, BasilConnection pContext, BProtocol pProtocol);
        public virtual void Process(BMessage pMsg, BasilConnection pContext, BProtocol pProtocol) {
            BMessage resp = BasilConnection.MakeResponse(pMsg);
            resp.Exception = "Session is not open. AA";
            pProtocol.Send(resp);

                /*
                switch (pMsg.Op) {
                    case (uint)BMessageOps.CreateItemReq:
                        break;
                    case (uint)BMessageOps.DeleteItemReq:
                        break;
                    case (uint)BMessageOps.AddAbilityReq:
                        break;
                    case (uint)BMessageOps.RemoveAbilityReq:
                        break;
                    case (uint)BMessageOps.RequestPropertiesReq:
                        break;
                    case (uint)BMessageOps.UpdatePropertiesReq:
                        break;
                    case (uint)BMessageOps.OpenSessionReq:
                        break;
                    case (uint)BMessageOps.AliveCheckReq:
                        break;
                    default:
                        break;
                }
                */
        }
    }

    public class BasilConnection {
        private readonly ParamBlock _params;
        private readonly BProtocol _protocol;
        private readonly BLogger _log;

        private OSAuthToken _incomingAuth;
        private OSAuthToken _outgoingAuth;

        public readonly Dictionary<string, RPCInfo> _rpcSessions = new Dictionary<string, RPCInfo>();

        // When a non-RCP response message is received, this processor is called
        private IncomingMessageProcessor _processOp;

        public BasilConnection(BProtocol pProtocol, BLogger pLogger) {
            _log = pLogger;
            _protocol = pProtocol;
            _protocol.SetReceiveCallback(BasilConnection.ReceivedMsgProcessor, this);
            if (_log == null) {
                throw new Exception("BasilConnection.constructor: logger parameter null");
            }
            _processOp = new IncomingMessageProcessor(this);
        }

        // Once an OpenSession happens, the incoming and outgoing auths are created and set.
        public void SetAuthorizations(OSAuthToken pIncoming, OSAuthToken pOutgoing) {
            _incomingAuth = pIncoming;
            _outgoingAuth = pOutgoing;
        }

        // Caller sets the routines that do the processing
        public void SetOpProcessor(IncomingMessageProcessor pProcessor) {
            _processOp = pProcessor;
        }

        public void Start(ParamBlock pParams) {
            return;
        }

        public void Send(BMessage pMsg) {
            _protocol?.Send(pMsg);
        }

        public Task<BMessage> CreateItem(ParamBlock pProps) {
            BMessage bmsg = new BMessage() { Op = (uint)BMessageOps.CreateItemReq };
            if (_outgoingAuth != null) bmsg.Auth = _outgoingAuth.Token;
            bmsg.IProps = pProps == null ? new Dictionary<string, string>() : CreatePropertyList(pProps);
            return SendAndPromiseResponse(bmsg, this);
        }

        public Task<BMessage> DeleteItem(string pItemId, OSAuthToken pItemAuth) {
            BMessage bmsg = new BMessage() { Op = (uint)BMessageOps.DeleteItemReq };
            if (_outgoingAuth != null) bmsg.Auth = _outgoingAuth.Token;
            bmsg.IId = pItemId;
            bmsg.IProps = new Dictionary<string, string>();
            if (pItemAuth != null) bmsg.IAuth = pItemAuth.Token;
            return SendAndPromiseResponse(bmsg, this);
        }

        public Task<BMessage> AddAbility(string pItemId, ParamBlock pProps) {
            BMessage bmsg = new BMessage() { Op = (uint)BMessageOps.AddAbilityReq };
            if (_outgoingAuth != null) bmsg.Auth = _outgoingAuth.Token;
            bmsg.IId = pItemId;
            bmsg.IProps = pProps == null ? new Dictionary<string, string>() : CreatePropertyList(pProps);
            return SendAndPromiseResponse(bmsg, this);
        }

        public Task<BMessage> RemoveAbility(string pItemId, ParamBlock pProps) {
            BMessage bmsg = new BMessage() { Op = (uint)BMessageOps.RemoveAbilityReq };
            if (_outgoingAuth != null) bmsg.Auth = _outgoingAuth.Token;
            bmsg.IId = pItemId;
            bmsg.IProps = pProps == null ? new Dictionary<string, string>() : CreatePropertyList(pProps);
            return SendAndPromiseResponse(bmsg, this);
        }

        public Task<BMessage> RequestProperties(string pItemId, string pFilter) {
            BMessage bmsg = new BMessage() { Op = (uint)BMessageOps.RequestPropertiesReq };
            if (_outgoingAuth != null) bmsg.Auth = _outgoingAuth.Token;
            bmsg.IId = pItemId;
            bmsg.IProps = pFilter == null ? new Dictionary<string, string>() : new Dictionary<string, string>() { { "Filter", pFilter } };
            return SendAndPromiseResponse(bmsg, this);
        }

        public Task<BMessage> UpdateProperties(string pItemId, ParamBlock pProps) {
            BMessage bmsg = new BMessage() { Op = (uint)BMessageOps.UpdatePropertiesReq };
            if (_outgoingAuth != null) bmsg.Auth = _outgoingAuth.Token;
            bmsg.IId = pItemId;
            bmsg.IProps = pProps == null ? new Dictionary<string, string>() : CreatePropertyList(pProps);
            return SendAndPromiseResponse(bmsg, this);
        }

        public Task<BMessage> MakeConnection(ParamBlock pProps) {
            BMessage bmsg = new BMessage() { Op = (uint)BMessageOps.MakeConnectionReq };
            if (_outgoingAuth != null) bmsg.Auth = _outgoingAuth.Token;
            bmsg.IProps = pProps == null ? new Dictionary<string, string>() : CreatePropertyList(pProps);
            return SendAndPromiseResponse(bmsg, this);
        }

        // Process a received message from the protocol processor.
        // The 'context' is the BasilConnection.
        // @param {BMessage} pMsg received BMessage
        // @param {object} pContext the BasilConnection that set up the processor
        // @param {BProtocol} pProtocol where the message came from
        private static void ReceivedMsgProcessor(BMessage pMsg, object pContext, BProtocol pProtocol) {
            BasilConnection context = pContext as BasilConnection;
            if (pMsg.RCode != null) {
                RPCInfo session;
                lock (context._rpcSessions) {
                    session = context._rpcSessions[pMsg.RCode];
                    if (session != null) {
                        context._rpcSessions.Remove(pMsg.RCode);
                    }
                }
                if (session != null) {
                    try {
                        session.taskCompletion.SetResult(pMsg);
                    }
                    catch (Exception e) {
                        string errMsg = String.Format("BasilConnection.Processor: exception setting result: {0}", e);
                        session.reject(errMsg);
                    }
                }
            }
            else {
                context._processOp.Process(pMsg, context, pProtocol);
            }
        }

        // Convert the passed set of parameters into the <string,string> structure that's
        //    send in the BMessage.
        private Dictionary<string, string> CreatePropertyList(ParamBlock pProps) {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            foreach (var kvp in pProps.Params) {
                ret.Add(kvp.Key, kvp.Value.ToString());
            }
            return ret;
        }

        // Source of random numbers used for RPC response codes.
        // Random so they are unguessable.
        private readonly Random _randomNumbers = new Random();

        // Send u
        private Task<BMessage> SendAndPromiseResponse(BMessage pReq, BasilConnection pContext) {
            // _log.Debug("{0} SendAndAwaitResponse", _logHeader);
            // Place structure in message that receiver will send back so we can match response.
            TaskCompletionSource<BMessage> tcs = new TaskCompletionSource<BMessage>();
            lock (_rpcSessions) {
                string thisSession = Util.RandomString(10);
                pReq.SCode = thisSession;
                _rpcSessions.Add(thisSession, new RPCInfo() {
                    session = thisSession,
                    context = pContext,
                    taskCompletion = tcs,
                    timeRPCCreated = DateTime.UtcNow,
                });
            }
            // _log.Debug("{0} SendAndAwaitResponse: Sending '{1}'", _logHeader, pReq);
            Send(pReq);
            return tcs.Task;
        }

        // Create a BMessage that is a response for a BMessage.
        // Fills the op for the response and the RPC response code.
        public static BMessage MakeResponse(BMessage pReq) {
            BMessage msg = new BMessage();
            msg.Op = (uint)BasilConnection.RespFromReq[(BMessageOps)pReq.Op];
            // Move the RPC id code sent to the RPC response code
            if (pReq.SCode is string && pReq.SCode.Length > 0) {
                msg.RCode = pReq.SCode;
            }
            return msg;
        }

        // Simple table that maps a message request op-code to a response op-code
        public static Dictionary<BMessageOps, BMessageOps> RespFromReq = new Dictionary<BMessageOps, BMessageOps>() {
            { BMessageOps.UnknownReq, BMessageOps.UnknownReq },
            { BMessageOps.CreateItemReq, BMessageOps.CreateItemResp },
            { BMessageOps.DeleteItemReq, BMessageOps.DeleteItemResp },
            { BMessageOps.AddAbilityReq, BMessageOps.AddAbilityResp },
            { BMessageOps.RemoveAbilityReq, BMessageOps.RemoveAbilityResp },
            { BMessageOps.RequestPropertiesReq, BMessageOps.RequestPropertiesResp },
            { BMessageOps.UpdatePropertiesReq, BMessageOps.UpdatePropertiesResp },

            { BMessageOps.OpenSessionReq, BMessageOps.OpenSessionResp },
            { BMessageOps.CloseSessionReq, BMessageOps.CloseSessionResp },
            { BMessageOps.MakeConnectionReq, BMessageOps.MakeConnectionResp },

            { BMessageOps.AliveCheckReq, BMessageOps.AliveCheckResp },
        };
    }
}
