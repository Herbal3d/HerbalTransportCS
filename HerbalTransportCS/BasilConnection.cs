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

using BasilMessage = org.herbal3d.basil.protocol.Message;

namespace org.herbal3d.transport {
    // A connection to a SpaceServer from a Basil Viewer.
    // Accept the OpenConnection from client then start the processing of messages.
    public class BasilConnection  : IDisposable {
        private static readonly string _logHeader = "[BasilConnection]";

        // Public connections to the outside world: transport connection and calls to server
        public TransportConnection Transport;

        // Mapping of BasilMessage op's to and from code to string operation name
        public Dictionary<Int32, String> BasilMessageNameByOp = new Dictionary<int, string>();
        public Dictionary<string, Int32> BasilMessageOpByName = new Dictionary<string, int>();

        // The registered processors for received operation codes.
        // Register a ProcessMessage routine for each possible message 'op' received.
        //    The ProcessMessage routine does the operation and then returns a response
        //    message or null if no response.
        public delegate BasilMessage.BasilMessage ProcessMessage(BasilMessage.BasilMessage pMsg);
        public class Processors : Dictionary<Int32, ProcessMessage> { };
        // The processors for received op codes. Added to by the *Processor classes.
        private Processors _MsgProcessors = new Processors();

        // Handles to the various message processors. Held on to for later disposal.
        public AliveCheckProcessor AliveCheckProcessor;
        public SpaceServerProcessor SpaceServiceProcessor;
        public BasilClientProcessor BasilClientProcessor;

        TransportContext _context; // Global constants

        // Per Basil connection RPC information.
        // One is kept for each outstanding RPC request. A later response will find
        //     this and add result message to 'taskCompletion'.
        public Dictionary<UInt32, SentRPC> OutstandingRPC = new Dictionary<UInt32, SentRPC>();
        public class SentRPC {
            public UInt32 session;          // unique number used to link response
            public MsgProcessor context;    // for debugging, the active message processor
            public UInt64 timeRPCCreated;   // when the RPC was initially sent
            // Task completion object that is holding the context for the reply.
            public TaskCompletionSource<BasilMessage.BasilMessage> taskCompletion;
        };

        // A socket connection has been made to a Basil Server.
        // Initialize message receivers and senders.
        public BasilConnection(ISpaceServer pSpaceServer,
                        TransportConnection pConnection, TransportContext pContext) {
            Transport = pConnection;
            _context = pContext;

            // Build the tables of ops to names based on enum in Protobuf definitions
            this.BuildBasilMessageOps();

            // Processors for received messages
            AliveCheckProcessor = new AliveCheckProcessor(this, _context);
            SpaceServiceProcessor = new SpaceServerProcessor(this, pSpaceServer, _context);
            BasilClientProcessor = new BasilClientProcessor(this, _context);
        }

        public void Dispose() {
            if (Transport != null) {
                Transport.Disconnect();
                Transport = null;
                _MsgProcessors.Clear();
                OutstandingRPC.Clear();
                AliveCheckProcessor = null;
                SpaceServiceProcessor = null;
                BasilClientProcessor = null;
            }
            throw new NotImplementedException();
        }

        // Add some message processors for received op codes.
        public void AddMessageProcessors(Processors pProcessors) {
            foreach (var processor in pProcessors) {
                _MsgProcessors.Add(processor.Key, processor.Value);
            }
        }

        public void ReplaceMessageProcessor(int pOpCode, ProcessMessage pProcessor) {
            _MsgProcessors.Remove(pOpCode);
            _MsgProcessors.Add(pOpCode, pProcessor);
        }

        // This process shouldn't be receiving text message over the WebSocket
        public void Receive(string pMsg) {
            _context.Log.ErrorFormat("{0} Receive: received a text message: {1}", _logHeader, pMsg);
        }

        // Received a binary message. Find the processor and execute it.
        public void Receive(byte[] pMsg) {
            BasilMessage.BasilMessage rcvdMsg = BasilMessage.BasilMessage.Parser.ParseFrom(pMsg);
            if (_MsgProcessors.ContainsKey(rcvdMsg.Op)) {
                try {
                    BasilMessage.BasilMessage reply = _MsgProcessors[rcvdMsg.Op](rcvdMsg);
                    // If processing the message generated a reply message, send it.
                    if (reply != null) {
                        this.Send(reply);
                    }
                }
                catch (Exception e) {
                    _context.Log.ErrorFormat("{0} Exception processing received message: {1}, e={2}",
                            _logHeader, BasilMessageNameByOp[rcvdMsg.Op], e);
                }
            }
            else {
                _context.Log.ErrorFormat("{0} Receive: received an unknown message op: {1}", _logHeader, rcvdMsg);
            }
        }

        // Send the message!!
        public void Send(BasilMessage.BasilMessage pMsg) {
            Transport.Send(pMsg.ToByteArray());
        }

        // 
        public void AbortConnection() {
        }

        // Loop over the Protobuf op enum and build a name to op and an op to name map.
        private void BuildBasilMessageOps() {
            Type enumType = typeof(BasilMessage.BasilMessageOps);
            foreach (BasilMessage.BasilMessageOps op in (BasilMessage.BasilMessageOps[])Enum.GetValues(enumType)) {
                string opName = Enum.GetName(enumType, op);
                this.BasilMessageOpByName.Add(opName, (Int32)op);
                this.BasilMessageNameByOp.Add((Int32)op, opName);
            }
        }
    }
}
