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
        public string session;          // unique number used to link response
        public BasilConnection context; // A handle back to the connection instance
        // Task completion object that is holding the context for the reply.
        public TaskCompletionSource<BMessage> taskCompletion;
    }
    public enum BConnectionStates {
        INITIALIZING,
        OPEN,
        CLOSING,
        ERROR,
        CLOSED
    };

    // Class used to process incoming connections.
    // Different processors create a subclass of this and over-ride "Process"
    //     with their processors.
    // This processor is added to the BasilConnection with "SetOpProcessor".
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

    // When the connection state changes, a function defined thus is called
    public delegate void BasilConnectionStateChange(BConnectionStates pNewState, BasilConnection pContext);

    public class BasilConnection {
        private readonly string _logHeader = "[BasilConnection]";
        private readonly BLogger _log;

        private BProtocol _protocol;
        private OSAuthToken _incomingAuth;
        private OSAuthToken _outgoingAuth;

        private BConnectionStates _state;

        // A unique identifier for this particular connection
        public string ConnectionID { get; private set; }

        public readonly Dictionary<string, RPCInfo> _rpcSessions = new Dictionary<string, RPCInfo>();

        // When a non-RCP response message is received, this processor is called
        private IncomingMessageProcessor _processOp;
        private BasilConnectionStateChange _processStateChange;

        // Create a BMessage based communication based on a BProtocol stream of messages.
        public BasilConnection(BProtocol pProtocol, BLogger pLogger) {
            _log = pLogger;
            _protocol = pProtocol;
            _protocol.SetReceiveCallback(BasilConnection.ReceivedMsgProcessor, this);
            if (_log == null) {
                throw new Exception("BasilConnection.constructor: logger parameter null");
            }

            // Make an identifer for thisc onnection
            ConnectionID = Util.RandomString(8);

            // Start with a default processor that returns errors
            // Someone will later over-ride this by calling "SetOpProcessor"
            _processOp = new IncomingMessageProcessor(this);
        }

        // Once an OpenSession happens, the incoming and outgoing auths are created and set.
        public void SetAuthorizations(OSAuthToken pIncoming, OSAuthToken pOutgoing) {
            _log.Debug("{0}: SetAuthorizations. id={1}, in={2}, out={3}",
                    "BasilConnection", ConnectionID, pIncoming.Token, pOutgoing.Token);
            _incomingAuth = pIncoming;
            _outgoingAuth = pOutgoing;
        }

        // Caller sets the routines that do the processing
        public void SetOpProcessor(IncomingMessageProcessor pProcessor, BasilConnectionStateChange pStateChange) {
            _processOp = pProcessor;
            _processStateChange = pStateChange;
        }

        public void Start() {
            // Watch the transport state and change our state to that
            _protocol.Transport.OnStateChange += watchTransportState;
            _protocol.Start();
            return;
        }

        // Called when need to disconnect and stop working
        public void Stop() {
            if (_protocol != null) {
                _protocol.Close();
                _protocol = null;
            }
        }

        // When transport state changes, my state changes
        private void watchTransportState(BTransport pXport, BTransportConnectionStates pNewState, object pContext) {
            BasilConnection me = pContext as BasilConnection;
            if (me != null) {
                me._log.Debug("BasilConnection.watchTransportState: newState = {0}", pNewState);
                switch (pNewState) {
                    case BTransportConnectionStates.OPEN: {
                        // If connection is OPEN, tell anyone who is waiting for READY
                        List<TaskCompletionSource<bool>> waiters;
                        lock (me._readyWaiters) {
                            waiters = me._readyWaiters;
                            me._readyWaiters.Clear();
                        }
                        if (waiters != null) {
                            foreach (var waiter in waiters) {
                                waiter.SetResult(true);
                            }
                            waiters.Clear();
                            waiters = null;
                        }
                        break;
                    }
                    case BTransportConnectionStates.CLOSED: {
                        break;
                    }
                    case BTransportConnectionStates.ERROR: {
                        break;
                    }
                    case BTransportConnectionStates.CLOSING: {
                        break;
                    }
                    default: {
                        break;
                    }
                }
                if (me._processStateChange != null) {
                    me._processStateChange(me.getState(), me);
                }
            }
        }

        // Send the packaged BMessage.
        // If there is no client Auth specified, it is added.
        // If optional parameters are specified, they are added to IProps.
        public void Send(BMessage pMsg, ParamBlock pParams = null) {
            if (pMsg.Auth == null && _outgoingAuth != null) {
                pMsg.Auth = _outgoingAuth.Token;
            }
            if (pParams != null) {
                pParams.CopyTo(pMsg.IProps);
            }
            _protocol?.Send(pMsg);
        }

        // Send a response to a received message.
        // The optional parameters are added to IProps before sending
        public void SendResponse(BMessage pReceived, ParamBlock pParams = null) {
            var resp = BasilConnection.MakeResponse(pReceived);
            Send(resp, pParams);
        }

        private Dictionary<BConnectionStates, BTransportConnectionStates> mapConnectionStateToTransportState
            = new Dictionary<BConnectionStates, BTransportConnectionStates>() {
                { BConnectionStates.INITIALIZING,  BTransportConnectionStates.INITIALIZING },
                { BConnectionStates.OPEN,          BTransportConnectionStates.OPEN },
                { BConnectionStates.CLOSING,       BTransportConnectionStates.CLOSING },
                { BConnectionStates.ERROR,         BTransportConnectionStates.ERROR },
                { BConnectionStates.CLOSED,        BTransportConnectionStates.CLOSED },
            };
        private Dictionary<BTransportConnectionStates, BConnectionStates> mapTransportStateToConnectionState 
            = new Dictionary<BTransportConnectionStates, BConnectionStates>() {
                { BTransportConnectionStates.INITIALIZING,  BConnectionStates.INITIALIZING },
                { BTransportConnectionStates.OPEN,          BConnectionStates.OPEN },
                { BTransportConnectionStates.CLOSING,       BConnectionStates.CLOSING },
                { BTransportConnectionStates.ERROR,         BConnectionStates.ERROR },
                { BTransportConnectionStates.CLOSED,        BConnectionStates.CLOSED },
            };
        // The BasilConnection state mirrors the state of the transport
        public BConnectionStates getState() {
            if (_protocol == null) {
                return BConnectionStates.CLOSED;
            }
            var transportState = _protocol.Transport.ConnectionState;
            this._state = mapTransportStateToConnectionState[transportState];
            return this._state;
        }

        private List<TaskCompletionSource<bool>> _readyWaiters = new List<TaskCompletionSource<bool>>();
        // Return a Task that is satisfied when this connection is Ready
        public Task<bool> WhenReady() {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            if (getState() == BConnectionStates.OPEN) {
                // If already OPEN, return the task in RunToCompletion state
                tcs.SetResult(true);
            }
            else {
                // if not OPEN, queue the waiting task (in case there are several waiters)
                lock (_readyWaiters) {
                    // Check again inside the lock to prevent race conditions
                    if (getState() == BConnectionStates.OPEN) {
                        tcs.SetResult(true);
                    }
                    else {
                        _readyWaiters.Add(tcs);
                    }
                }
            }
            return tcs.Task;
        }

        public Task<BMessage> CreateItem(ParamBlock pProps) {
            BMessage bmsg = new BMessage() { Op = (uint)BMessageOps.CreateItemReq };
            if (_outgoingAuth != null) bmsg.Auth = _outgoingAuth.Token;
            bmsg.IProps = pProps == null ? new Dictionary<string, object>() : CreatePropertyList(pProps);
            return SendAndPromiseResponse(bmsg, this);
        }

        public Task<BMessage> DeleteItem(string pItemId, OSAuthToken pItemAuth = null) {
            BMessage bmsg = new BMessage() { Op = (uint)BMessageOps.DeleteItemReq };
            if (_outgoingAuth != null) bmsg.Auth = _outgoingAuth.Token;
            bmsg.IId = pItemId;
            bmsg.IProps = new Dictionary<string, object>();
            if (pItemAuth != null) bmsg.IAuth = pItemAuth.Token;
            return SendAndPromiseResponse(bmsg, this);
        }

        public Task<BMessage> AddAbility(string pItemId, ParamBlock pProps) {
            BMessage bmsg = new BMessage() { Op = (uint)BMessageOps.AddAbilityReq };
            if (_outgoingAuth != null) bmsg.Auth = _outgoingAuth.Token;
            bmsg.IId = pItemId;
            bmsg.IProps = pProps == null ? new Dictionary<string, object>() : CreatePropertyList(pProps);
            return SendAndPromiseResponse(bmsg, this);
        }

        public Task<BMessage> RemoveAbility(string pItemId, ParamBlock pProps) {
            BMessage bmsg = new BMessage() { Op = (uint)BMessageOps.RemoveAbilityReq };
            if (_outgoingAuth != null) bmsg.Auth = _outgoingAuth.Token;
            bmsg.IId = pItemId;
            bmsg.IProps = pProps == null ? new Dictionary<string, object>() : CreatePropertyList(pProps);
            return SendAndPromiseResponse(bmsg, this);
        }

        public Task<BMessage> RequestProperties(string pItemId, string pFilter) {
            BMessage bmsg = new BMessage() { Op = (uint)BMessageOps.RequestPropertiesReq };
            if (_outgoingAuth != null) bmsg.Auth = _outgoingAuth.Token;
            bmsg.IId = pItemId;
            bmsg.IProps = pFilter == null ? new Dictionary<string, object>() : new Dictionary<string, object>() { { "Filter", pFilter } };
            return SendAndPromiseResponse(bmsg, this);
        }

        public Task<BMessage> UpdateProperties(string pItemId, ParamBlock pProps) {
            BMessage bmsg = new BMessage() { Op = (uint)BMessageOps.UpdatePropertiesReq };
            if (_outgoingAuth != null) bmsg.Auth = _outgoingAuth.Token;
            bmsg.IId = pItemId;
            bmsg.IProps = pProps == null ? new Dictionary<string, object>() : CreatePropertyList(pProps);
            return SendAndPromiseResponse(bmsg, this);
        }

        public Task<BMessage> MakeConnection(Dictionary<string,object> pProps) {
            BMessage bmsg = new BMessage() { Op = (uint)BMessageOps.MakeConnectionReq };
            if (_outgoingAuth != null) bmsg.Auth = _outgoingAuth.Token;
            bmsg.IProps = pProps == null ? new Dictionary<string, object>() : pProps;
            return SendAndPromiseResponse(bmsg, this);
        }
        public Task<BMessage> MakeConnection(ParamBlock pProps) {
            return MakeConnection(pProps.Params);
        }

        // Process a received message from the protocol processor.
        // The 'context' is the BasilConnection.
        // @param {BMessage} pMsg received BMessage
        // @param {object} pContext the BasilConnection that set up the processor
        // @param {BProtocol} pProtocol where the message came from
        private static void ReceivedMsgProcessor(BMessage pMsg, object pContext, BProtocol pProtocol) {
            BasilConnection context = pContext as BasilConnection;
            if (context != null) {
                if (context._incomingAuth != null) {
                    context._log.Debug("BasilConnection.RMP: id={0}, in={1}, out={2}",
                                context.ConnectionID,
                                context._incomingAuth.Token,
                                context._outgoingAuth.Token
                                );
                    
                    if (pMsg.Auth == null || context._incomingAuth.Token != pMsg.Auth) {
                        BMessage resp = BasilConnection.MakeResponse(pMsg);
                        resp.Exception = "Not authorized";
                        pProtocol.Send(resp);
                    }
                }
                if (pMsg.RCode != null) {
                    // Has an RCode so must be a response to a RPC
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
                            session.taskCompletion.SetException(new Exception(errMsg));
                        }
                    }
                }
                else {
                    context._processOp.Process(pMsg, context, pProtocol);
                }
            }
        }

        // Convert the passed set of parameters into the <string,string> structure that's
        //    send in the BMessage.
        private Dictionary<string, object> CreatePropertyList(ParamBlock pProps) {
            Dictionary<string, object> ret = new Dictionary<string, object>();
            foreach (var kvp in pProps.Params) {
                ret.Add(kvp.Key, kvp.Value);
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
            BMessageOps responseCode = BMessageOps.UnknownReq;
            BasilConnection.RespFromReq.TryGetValue((BMessageOps)pReq.Op, out responseCode);
            msg.Op = (uint)responseCode;
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
