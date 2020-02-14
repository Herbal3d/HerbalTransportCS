// Copyright (c) 2019 Robert Adams
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
using System.Text;
using System.Reflection;
using System.Threading.Tasks;

using Google.Protobuf;

using BasilType = org.herbal3d.basil.protocol.BasilType;
using BasilMessage = org.herbal3d.basil.protocol.Message;

namespace org.herbal3d.transport {
    public abstract class MsgProcessor {

        private static readonly string _logHeader = "[MsgProcessor]";

        private readonly Random _randomNumbers = new Random();
        protected readonly TransportContext _context;

        public  BasilConnection Connection;

        public MsgProcessor(BasilConnection pConnection, TransportContext pContext) {
            Connection = pConnection;
            _context = pContext;
        }

        // Send a message and expect a RPC type response.
        public async Task<BasilMessage.BasilMessage> SendAndAwaitResponse(BasilMessage.BasilMessage pReq) {
            // _context.Log.DebugFormat("{0} SendAndAwaitResponse", _logHeader);
            // Place structure in message that receiver will send back so we can match response.
            UInt32 thisSession = (UInt32)_randomNumbers.Next();
            pReq.Response = new BasilType.BResponseRequest() {
                ResponseSession = thisSession
            };
            var tcs = new TaskCompletionSource<BasilMessage.BasilMessage>();
            lock (Connection.OutstandingRPC) {
                Connection.OutstandingRPC.Add(thisSession, new BasilConnection.SentRPC() {
                    session = thisSession,
                    context = this,
                    taskCompletion = tcs,
                    timeRPCCreated = (ulong)DateTime.UtcNow.ToBinary(),
                });
            }
            // _context.Log.DebugFormat("{0} SendAndAwaitResponse: Sending op={1}", _logHeader, pReq.Op);
            Connection.Send(pReq);
            BasilMessage.BasilMessage resp = await tcs.Task;
            // _context.Log.DebugFormat("{0} SendAndAwaitResponse: Response op={1}", _logHeader, resp.Op);
            if (resp.Exception != null) {
                throw new BasilException(resp.Exception.Reason, new Dictionary<string,string>(resp.Exception.Hints));
            }
            return resp;
        }

        // Construct enclosing stream message to send back to the Basil viewer.
        // Called with a constructed response message and the stream message with the request.
        // Add the response information to the response message so other side can match
        //     the response to the request.
        public void SendMessage(BasilMessage.BasilMessage pResponseMsg, BasilMessage.BasilMessage pReqMsg) {
            // string responseMsgName = _basilConnection.BasilMessageNameByOp[pResponseMsg.Op];
            // BasilTest.log.DebugFormat("{0} SendResponse: {1}", _logHeader, responseMsgName);

            if (pReqMsg != null && pReqMsg.ResponseCode != 0) {
                pResponseMsg.ResponseCode = pReqMsg.ResponseCode;
            }
            Connection.Send(pResponseMsg);
        }

        // Given a request messsage and a partial response message, add the response tagging formation
        //    to the response so the sender of the request can match the messages.
        public static void MakeMessageAResponse(ref BasilMessage.BasilMessage pResponseMsg,
                    BasilMessage.BasilMessage pRequestMsg) {
            if (pRequestMsg != null && pRequestMsg.ResponseCode != 0) {
                pResponseMsg.ResponseCode = pRequestMsg.ResponseCode;
            }
        }

        // Received a response type message.
        // Find the matching RPC call info and call the process waiting for the response.
        protected BasilMessage.BasilMessage HandleResponse(BasilMessage.BasilMessage pResponseMsg) {
            if (pResponseMsg.ResponseCode != 0) {
                // Look up the session this response corresponds to
                UInt32 sessionIndex = pResponseMsg.ResponseCode;
                BasilConnection.SentRPC session;
                TaskCompletionSource<BasilMessage.BasilMessage> responseTask = null;
                lock (Connection.OutstandingRPC) {
                    if (Connection.OutstandingRPC.ContainsKey(sessionIndex)) {
                        session = (BasilConnection.SentRPC)Connection.OutstandingRPC[sessionIndex];
                        Connection.OutstandingRPC.Remove(sessionIndex);
                        responseTask = session.taskCompletion;
                    }
                    else {
                        _context.Log.ErrorFormat("{0} missing RCP response key: {1}", _logHeader, sessionIndex);
                    }
                }
                if (responseTask != null) {
                    try {
                        // Setting the result will start up the process waiting on the task
                        responseTask.SetResult(pResponseMsg);
                    }
                    catch (Exception e) {
                        _context.Log.ErrorFormat("{0} Exception processing message: {1}",
                                        _logHeader, e);
                    }
                }
            }
            else {
                _context.Log.ErrorFormat("{0} ResponseReq.ResponseSession missing. Type={1}",
                                _logHeader, Connection.BasilMessageNameByOp[pResponseMsg.Op]);
            }
            return null;    // responses don't have a response
        }
    }
}
