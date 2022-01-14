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
using System.Text;
using System.Threading.Tasks;

using org.herbal3d.cs.CommonUtil;

namespace org.herbal3d.transport {
    public class Comm {
        public static BasilConnection MakeConnection(ParamBlock pParams) {
            ParamBlock _params = new ParamBlock(null, pParams,
                    new ParamBlock(new Dictionary<string, object>() {
                        // Admin
                        { "logger", null },
                        { "cancellationToken", null },

                        // Transport
                        { "transport", "WS" },
                        { "transporturl", null },
                        { "disableNaglesAlgorithm", true },
                        { "certificate", null },

                        // Protocol
                        { "protocol", "Basil-JSON" },

                        // BasilConnection
                        { "service", "SpaceServer" },
                        { "incomingAuth", null },
                        { "outgoingAuth", null }
                    }));
            try {
                BTransport xport = Comm.TransportFactory(_params);
                BProtocol proto = Comm.ProtocolFactory(_params, xport);
                BasilConnection conn = Comm.BasilConnectionFactory(_params, proto);

                xport.Start(_params);
                proto.Start(_params);
                conn.Start(_params);

                return conn;
            }
            catch (Exception ee) {
                throw ee;
            }
        }

        public static BTransport TransportFactory(ParamBlock pParams) {
            ParamBlock _params = new ParamBlock(null, pParams,
                    new ParamBlock(new Dictionary<string, object>() {
                        { "transporturl", null },
                    }));
            BTransport xport = null;
            string xportName = _params.P<string>("transport");
            switch (xportName) {
                case "TCP":
                    // xport = new BTransportTCP(_params);
                    break;
                case "WS":
                    xport = new BTransportWS(_params);
                    break;
                default:
                    _params.P<BLogger>("logger")?.Error("Comm.TransportFactory: unknown transport type {0}", xportName);
                    break;
            };
            if (xport != null) {
                return xport;
            }
            throw new Exception("Comm.TransportFactory: creation of transport failed");
        }

        public static BProtocol ProtocolFactory(ParamBlock pParams, BTransport pTransport) {
            ParamBlock _params = new ParamBlock(null, pParams,
                    new ParamBlock(new Dictionary<string, object>() {
                        { "protocol", "Basil-JSON" }
                    }));
            BProtocol proto = null;
            string protocolName = _params.P<string>("protocol");
            switch (protocolName) {
                case "Basil-JSON":
                    proto = new BProtocolJSON(_params, pTransport);
                    break;
                case "Basil-PB":
                    // proto = new BProtocolPB(_params, pTransport);
                    break;
                case "Basil-FB":
                    // proto = new BProtocolFB(_params, pTransport);
                    break;
                default:
                    _params.P<BLogger>("logger")?.Error("Comm.ProtocolFactory: unknown protocol type {0}", protocolName);
                    break;
            }
            if (proto != null) {
                return proto;
            }
            throw new Exception("Comm.ProtocolFactory: creation of transport failed");
        }

        public static BasilConnection BasilConnectionFactory(ParamBlock pParams, BProtocol pProtocol) {
            BasilConnection conn = new BasilConnection(pParams, pProtocol);
            return conn;
        }
    }
}
