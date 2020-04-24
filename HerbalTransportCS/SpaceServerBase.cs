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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using org.herbal3d.cs.CommonEntitiesUtil;

using BM = org.herbal3d.basil.protocol.Message;
using BT = org.herbal3d.basil.protocol.BasilType;
using HT = org.herbal3d.transport;
using org.herbal3d.OSAuth;
using System.Diagnostics;

namespace org.herbal3d.transport {
    // A base class for space server implementations.
    // The SpaceServerListener creates a new SpaceServer instance when a client
    //     connects.
    public abstract class SpaceServerBase {

        private static readonly string _logHeader = "[SpaceServerBase]";

        // Return a reference to the client communication instance for this SpaceServer
        public BasilConnection ClientConnection;    // Communication to Basil
        public BasilComm Client;                    // Function calls to Basil

        // A cancellation token for this session
        public CancellationTokenSource Canceller;

        // Server side authorization token for this session
        public OSAuthToken SessionAuth;
        // Authorization token to talk to the client
        public OSAuthToken ClientAuth;
        // Key to uniquify this session's communication
        public string SessionKey;

        // To uniquify the different space servers, they have names
        public string LayerName;

        // Once a connection is made, alive check processor is added
        public AliveCheck AliveChecker;

        public SpaceServerBase(CancellationTokenSource pCanceller,
                        BasilConnection pBasilConnection, string pLayerName) {
            Canceller = pCanceller;
            ClientConnection = pBasilConnection;
            LayerName = pLayerName;

            HT.BasilConnection.Processors processors = new HT.BasilConnection.Processors {
                { (Int32)BM.BasilMessageOps.OpenSessionReq, this.ProcOpenSessionReq },
                { (Int32)BM.BasilMessageOps.OpenSessionResp, ClientConnection.HandleResponse },
                { (Int32)BM.BasilMessageOps.CloseSessionReq, this.ProcCloseSessionReq },
                { (Int32)BM.BasilMessageOps.CloseSessionResp, ClientConnection.HandleResponse },
            };
            ClientConnection.AddMessageProcessors(processors);

            // The thing to call to make requests to the Basil server
            Client = new HT.BasilComm(ClientConnection);
        }

        public void Shutdown() {
            DoShutdownWork();
            if (Canceller != null) {
                Canceller.Cancel();
            }
            if (AliveChecker != null) {
                AliveChecker.Shutdown();
                AliveChecker = null;
            }
            if (Client != null) {
                Client = null;
            }
            if (ClientConnection != null) {
                ClientConnection.Shutdown();
                ClientConnection = null;
            }
        }
        protected abstract void DoShutdownWork();

        // Send OpenSession requeest async.
        // Returns the client's returned property list.
        // On error, property list contains a single "Exception" entry with the message.
        public virtual async  Task<BT.Props> OpenSessionAsync( OSAuthToken pAuth, BT.Props pProps) {
            BM.BasilMessage req = new BM.BasilMessage() {
                Op = (uint)BM.BasilMessageOps.OpenSessionReq,
                SessionAuth = pAuth.ToString()
            };
            BM.BasilMessage resp = await ClientConnection.SendAndAwaitResponse(req);
            BT.Props ret = new BT.Props();
            if (String.IsNullOrEmpty(resp.Exception)) {
                ret.Add("Exception", resp.Exception);
                if (resp.ExceptionHints != null && resp.ExceptionHints.Count > 0) {
                    // System.Text.JSON is only available in Core 3.1
                    // ret.Add("ExceptionHints", JSONstringify(resp.ExceptionHints);
                }
            }
            else {
                if (resp.IProps != null && resp.IProps.Count > 0) {
                    foreach (var kvp in resp.IProps) {
                        ret.Add(kvp.Key, kvp.Value);
                    }
                }
            }
            return ret;
        }
        // Do work for the open session.
        protected abstract void DoOpenSessionWork(BasilConnection pConnection, BasilComm pClient,
                                        BT.Props pParms);
        // OpenSession is the other side logging in. Verify the token that was sent
        protected abstract bool VerifyClientAuthentication(OSAuthToken pUserToken);
        // Process a received OpenSessionRequest.
        // Check the login validation and then collect and return the session auth info.
        protected virtual BM.BasilMessage ProcOpenSessionReq(BM.BasilMessage pReq) {
            BM.BasilMessage resp = new BM.BasilMessage() {
                Op = (uint)BM.BasilMessageOps.OpenSessionResp
            };

            if (pReq.IProps != null && pReq.IProps.Count > 0) {
                // DEBUG DEBUG
                ClientConnection.Context.Log.DebugFormat("{0} Received OpenSession.", _logHeader);
                foreach (var kvp in pReq.IProps) {
                    ClientConnection.Context.Log.DebugFormat("{0}       {1}: {2}", _logHeader, kvp.Key, kvp.Value);
                }
                // END DEBUG DEBUG

                // This connection gets a unique handle
                string connectionKey = Util.RandomString(10);

                if (VerifyClientAuthentication(OSAuthToken.FromString(pReq.SessionAuth))) {
                    // Create a key to uniquify this session
                    // Use the version sent by the client if it is supplied
                    SessionKey = null;
                    pReq.IProps.TryGetValue("SessionKey", out SessionKey);
                    if (String.IsNullOrEmpty(SessionKey)) {
                        SessionKey = Util.RandomString(10);
                    };

                    // The client should have given us some authorization for our requests to her.
                    // Collect auth information for accessing the client and build an OSAuthToken
                    //     to be used when sending messages to the client.
                    try {
                        pReq.IProps.TryGetValue("ClientAuth", out string clientAuth);
                        if (clientAuth != null) {
                            OSAuthToken clientToken = OSAuthToken.FromString(clientAuth);
                            clientToken.Srv = "client";
                            clientToken.Sid = SessionKey;
                            // This puts the token to send with requests in requesters
                            ClientAuth = clientToken;
                            Client.ClientAuth = clientToken;
                        }
                    }
                    catch (Exception e) {
                        ClientConnection.Context.Log.ErrorFormat("{0} Exception parsing client auth info: {1}", _logHeader, e);
                        resp.Exception = "Client authentication info misformed";
                        resp.ExceptionHints.Add("Exception", e.ToString());
                        resp.ExceptionHints.Add("ClientAuthInfo", pReq.SessionAuth);
                        ClientAuth = null;
                    }

                    if (ClientAuth != null) {
                        // Add a processor for the alive check messages
                        if (ClientConnection.Context.Params.P<bool>("ShouldAliveCheckSessions")) {
                            AliveChecker = new AliveCheck(Canceller, ClientConnection) {
                                ClientAuth = ClientAuth
                            };
                        };

                        SessionAuth = new OSAuthToken() {
                            Srv = LayerName,
                            Sid = SessionKey
                        };

                        // Return the authorization information to the client
                        resp.IProps.Add("SessionAuth", SessionAuth.ToString());
                        resp.IProps.Add("SessionKey", SessionKey);
                        resp.IProps.Add("ConnectionKey", connectionKey);
                        resp.IProps.Add("Services", "[]");  // kept for downward compatability

                        // Note: this is next work is done on the messaging thread so the response doesn't
                        //    go back before the connection is set up. The called routine should spool off
                        //    any long tasks.
                        DoOpenSessionWork(ClientConnection, Client, new BT.Props(pReq.IProps));
                    };
                }
                else {
                    resp.Exception = "Not authorized";
                };
            }
            else {
                resp.Exception = "Connection not initialized";
            }
            ClientConnection.MakeMessageAResponse(ref resp, pReq);
            return resp;
        }

        // Send CloseSession request. Return 'true' if successful.
        public virtual async Task<bool> CloseSessionAsync(OSAuthToken pAuth, BT.Props pProps) {
            DoCloseSessionWork();
            BM.BasilMessage req = new BM.BasilMessage() {
                Op = (uint)BM.BasilMessageOps.CloseSessionReq,
                SessionAuth = pAuth.ToString()
            };
            BM.BasilMessage resp = await ClientConnection.SendAndAwaitResponse(req);
            bool ret = ! String.IsNullOrEmpty(resp.Exception);
            return ret;
        }
        protected abstract void DoCloseSessionWork();
        // Received a CloseSession request. Shut things down.
        protected  BM.BasilMessage ProcCloseSessionReq(BM.BasilMessage pReq) {
            BM.BasilMessage pResp = new BM.BasilMessage() {
                Op = (Int32)BM.BasilMessageOps.CloseSessionResp
            };
            Task.Run(() => {
                Thread.Sleep(500);
                this.Shutdown();
            });
            ClientConnection.MakeMessageAResponse(ref pResp, pReq);
            return pResp;
        }

        // Look at the request and make sure it is not a test connection request.
        // Return 'true' if a test connection and update the response with error info.
        protected bool CheckIfTestConnection(BM.BasilMessage pReq, ref BM.BasilMessage pResp) {
            bool ret = false;
            if (pReq.IProps != null) {
                if (pReq.IProps.TryGetValue("TestConnection", out string testConnectionValue)) {
                    try {
                        if (bool.Parse(testConnectionValue)) {
                            // For some reason, a test connection is being made
                            pResp.Exception = "Cannot make test connection to SpaceServer";
                            ret = true;
                        }
                    }
                    catch (Exception e) {
                        pResp.Exception = "Unparsable value of Feature parameter TestConnection: " + testConnectionValue;
                        pResp.ExceptionHints.Add("ExceptionMessage", e.ToString());
                        ret = true;
                    }
                }
            }
            return ret;
        }

        // Validate that this request can be done.
        protected bool ValidateRequestAuth(string pAuth) {
            bool isAuthorized = false;
            try {
                if (pAuth != null) {
                        isAuthorized = SessionAuth.Matches(pAuth);
                }
            }
            catch (Exception e) {
                ClientConnection.Context.Log.ErrorFormat("{0} ValidateRequestAuth: Exception checking auth: {1}",
                            _logHeader, e);
            }
            return isAuthorized;
        }
    }
}
