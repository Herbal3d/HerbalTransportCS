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
using System.Linq;
using System.Threading.Tasks;

using Google.Protobuf;

using org.herbal3d.cs.CommonEntitiesUtil;

using BT = org.herbal3d.basil.protocol.BasilType;
using BM = org.herbal3d.basil.protocol.Message;

namespace org.herbal3d.transport {
    // This class is put on top if the transport (which moves byte arrays) and
    //    implements BasilMessage in and out.
    // This implements the simple RPC  mechanism in the Basil messages.
    // This implements a BasilMessage.Op decoder so methods can be registered
    //    to process different received operation codes.
    public class BasilConnection  : ITransportMsgReceiver, IDisposable {
        private static readonly string _logHeader = "[BasilConnection]";

        // Public connections to the outside world: transport connection and calls to server
        public ITransportConnection Transport;

        // Mapping of BasilMessage op's to and from code to string operation name
        public static Dictionary<UInt32, String> BasilMessageNameByOp = new Dictionary<uint, string>();
        public static Dictionary<string, UInt32> BasilMessageOpByName = new Dictionary<string, uint>();

        // The registered processors for received operation codes.
        // Register a ProcessMessage routine for each possible message 'op' received.
        //    The ProcessMessage routine does the operation and then returns a response
        //    message or null if no response.
        public delegate BM.BasilMessage ProcessMessage(BM.BasilMessage pMsg);
        public class Processors : Dictionary<UInt32, ProcessMessage> { };
        // The processors for received op codes. Added to by the *Processor classes.
        private readonly Processors _MsgProcessors = new Processors();

        // Handles to the various message processors. Held on to for later disposal.
        public readonly TransportContext Context; // Global constants

        // Per Basil connection RPC information.
        // One is kept for each outstanding RPC request. A later response will find
        //     this and add result message to 'taskCompletion'.
        public Dictionary<UInt32, SentRPC> OutstandingRPC = new Dictionary<UInt32, SentRPC>();
        public class SentRPC {
            public UInt32 session;          // unique number used to link response
            public UInt64 timeRPCCreated;   // when the RPC was initially sent
            public BasilConnection context; // A handle back to the connection instance
            // Task completion object that is holding the context for the reply.
            public TaskCompletionSource<BM.BasilMessage> taskCompletion;
        };

        // Source of random numbers used for RPC response codes.
        // Random so they are unguessable.
        private readonly Random _randomNumbers = new Random();

        // A socket connection has been made to a Basil Server.
        // Initialize message receivers and senders.
        public BasilConnection(ITransportConnection pConnection, TransportContext pContext) {
            Transport = pConnection;
            Context = pContext;

            // Build the tables of ops to names based on enum in Protobuf definitions
            BuildBasilMessageOps();
        }

        public void Dispose() {
            Shutdown();
        }

        public  void Shutdown() {
            if (Transport != null) {
                Transport.Disconnect();
                Transport = null;
            }
            _MsgProcessors.Clear();
            OutstandingRPC.Clear();
        }

        // Send a message and expect a RPC type response.
        public async Task<BM.BasilMessage> SendAndAwaitResponse(BM.BasilMessage pReq) {
            // _context.Log.DebugFormat("{0} SendAndAwaitResponse", _logHeader);
            // Place structure in message that receiver will send back so we can match response.
            TaskCompletionSource<BM.BasilMessage> tcs = new TaskCompletionSource<BM.BasilMessage>();
            lock (OutstandingRPC) {
                UInt32 thisSession = (UInt32)_randomNumbers.Next();
                pReq.ResponseCode = thisSession;
                OutstandingRPC.Add(thisSession, new BasilConnection.SentRPC() {
                    session = thisSession,
                    context = this,
                    taskCompletion = tcs,
                    timeRPCCreated = (ulong)DateTime.UtcNow.ToBinary(),
                });
            }
            // Context.Log.DebugFormat("{0} SendAndAwaitResponse: Sending '{1}'", _logHeader, pReq);
            Send(pReq);
            BM.BasilMessage resp = await tcs.Task;
            // Context.Log.DebugFormat("{0} SendAndAwaitResponse: Received '{1}'", _logHeader, resp);
            return resp;
        }

        // Construct enclosing stream message to send back to the Basil viewer.
        // Called with a constructed response message and the stream message with the request.
        // Add the response information to the response message so other side can match
        //     the response to the request.
        public void SendMessage(BM.BasilMessage pResponseMsg, BM.BasilMessage pReqMsg) {
            // string responseMsgName = _basilConnection.BasilMessageNameByOp[pResponseMsg.Op];
            // BasilTest.log.DebugFormat("{0} SendResponse: {1}", _logHeader, responseMsgName);

            if (pReqMsg != null && pReqMsg.ResponseCode != 0) {
                pResponseMsg.ResponseCode = pReqMsg.ResponseCode;
            }
            Send(pResponseMsg);
        }

        // Given a request messsage and a partial response message, add the response tagging formation
        //    to the response so the sender of the request can match the messages.
        public void MakeMessageAResponse(ref BM.BasilMessage pResponseMsg,
                    BM.BasilMessage pRequestMsg) {
            if (pRequestMsg != null && pRequestMsg.ResponseCode != 0) {
                pResponseMsg.ResponseCode = pRequestMsg.ResponseCode;
            }
        }

        // Received a response type message.
        // Find the matching RPC call info and call the process waiting for the response.
        public BM.BasilMessage HandleResponse(BM.BasilMessage pResponseMsg) {
            if (pResponseMsg.ResponseCode != 0) {
                // Look up the session this response corresponds to
                UInt32 sessionIndex = pResponseMsg.ResponseCode;
                BasilConnection.SentRPC session;
                TaskCompletionSource<BM.BasilMessage> responseTask = null;
                lock (OutstandingRPC) {
                    if (OutstandingRPC.ContainsKey(sessionIndex)) {
                        session = (BasilConnection.SentRPC)OutstandingRPC[sessionIndex];
                        OutstandingRPC.Remove(sessionIndex);
                        responseTask = session.taskCompletion;
                    }
                    else {
                        Context.Log.ErrorFormat("{0} missing RCP response key: {1}", _logHeader, sessionIndex);
                    }
                }
                if (responseTask != null) {
                    try {
                        // Setting the result will start up the process waiting on the task
                        responseTask.SetResult(pResponseMsg);
                    }
                    catch (Exception e) {
                        Context.Log.ErrorFormat("{0} Exception processing message: {1}",
                                        _logHeader, e);
                    }
                }
            }
            else {
                Context.Log.ErrorFormat("{0} ResponseReq.ResponseSession missing. Type={1}",
                                _logHeader, BasilMessageNameByOp[pResponseMsg.Op]);
            }
            return null;    // responses don't have a response
        }

        // Add some message processors for received op codes.
        public void AddMessageProcessors(Processors pProcessors) {
            foreach (var processor in pProcessors) {
                _MsgProcessors.Add(processor.Key, processor.Value);
            }
        }

        public void ReplaceMessageProcessor(uint pOpCode, ProcessMessage pProcessor) {
            _MsgProcessors.Remove(pOpCode);
            _MsgProcessors.Add(pOpCode, pProcessor);
        }

        // This process shouldn't be receiving text message over the WebSocket
        // IMsgReceiver.Receive(string pMsg)
        public void Receive(string pMsg) {
            Context.Log.ErrorFormat("{0} Receive: received a text message: {1}", _logHeader, pMsg);
        }

        // Received a binary message. Find the processor and execute it.
        // IMsgReceiver.Receive(byte[] pMsg)
        public void Receive(byte[] pMsg) {
            BM.BasilMessage rcvdMsg = BM.BasilMessage.Parser.ParseFrom(pMsg);
            if (_MsgProcessors.ContainsKey(rcvdMsg.Op)) {
                try {
                    BM.BasilMessage reply = _MsgProcessors[rcvdMsg.Op](rcvdMsg);
                    // If processing the message generated a reply message, send it.
                    if (reply != null) {
                        this.Send(reply);
                    }
                }
                catch (Exception e) {
                    Context.Log.ErrorFormat("{0} Exception processing received message: {1}, e={2}",
                            _logHeader, BasilMessageNameByOp[rcvdMsg.Op], e);
                }
            }
            else {
                Context.Log.ErrorFormat("{0} Receive: received an unknown message op: {1}", _logHeader, rcvdMsg);
            }
        }

        // Send the message!!
        public void Send(BM.BasilMessage pMsg) {
            Transport.Send(pMsg.ToByteArray());
        }

        public void Send(byte[] pMsg) {
            Transport.Send(pMsg);
        }

        // IMsgReceiver.AbortConnection()
        public void AbortConnection() {
        }

        // Loop over the Protobuf op enum and build a name to op and an op to name map.
        private readonly static Object _buildLockObject = new object();
        private static void BuildBasilMessageOps() {
            lock (BasilConnection._buildLockObject) {
                if (BasilConnection.BasilMessageOpByName == null || BasilConnection.BasilMessageOpByName.Count == 0) {
                    Type enumType = typeof(BM.BasilMessageOps);
                    foreach (BM.BasilMessageOps op in (BM.BasilMessageOps[])Enum.GetValues(enumType)) {
                        string opName = Enum.GetName(enumType, op);
                        BasilConnection.BasilMessageOpByName.Add(opName, (UInt32)op);
                        BasilConnection.BasilMessageNameByOp.Add((UInt32)op, opName);
                    }
                }
            }
        }
    }
}
