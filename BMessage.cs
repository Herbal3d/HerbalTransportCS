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

namespace org.herbal3d.b.protocol {
    public enum BMessageOps {
        UnknownReq = 0x00000,
        CreateItemReq = 101,
        CreateItemResp = 102,
        DeleteItemReq = 103,
        DeleteItemResp = 104,
        AddAbilityReq = 105,
        AddAbilityResp = 106,
        RemoveAbilityReq = 107,
        RemoveAbilityResp = 108,
        RequestPropertiesReq = 109,
        RequestPropertiesResp = 110,
        UpdatePropertiesReq = 111,
        UpdatePropertiesResp = 112,

        OpenSessionReq = 201,
        OpenSessionResp = 202,
        CloseSessionReq = 203,
        CloseSessionResp = 204,
        MakeConnectionReq = 205,
        MakeConnectionResp = 206,

        AliveCheckReq = 301,
        AliveCheckResp = 302,
    }

    public enum CoordSystem {
        WGS86 = 0,    // WGS84 earth coordinates
        CAMERA = 1,    // Coordinates relative to camera position (-1..1 range, zero center)
        CAMERAABS = 2,    // Absolute coordinates relative to the camera position (zero center)
        VIRTUAL = 3,    // Zero based un-rooted coordinates
        MOON = 4,    // Earth-moon coordinates
        MARS = 5,    // Mars coordinates
        REL1 = 6,    // Mutually agreed base coordinates
        REL2 = 7,
        REL3 = 8
    }

    public enum RotationSystem {
        WORLDR = 0,    // rotation is relative to world coordinates
        LOCALR = 1,    // rotation is relative to referened object
        FORR = 2,    // rotation is relative to current frame of reference
        CAMERAR = 3     // rotation is relative to the camera direction
    }

    // Positions can be updated in mass. This is the per-item update information.
    public class PositionBlock {
        public double[] Pos;
        public double[] Rot;
        public CoordSystem CS;
        public RotationSystem RS;
        public double[] Vel;
        public double[] Path;
        public string IId;
        public string Auth;
        public string IAuth;
    }

    public class BMessage {
        public BMessage(BMessageOps pOp = BMessageOps.UnknownReq) {
            Op = (uint)pOp;
            IProps = new Dictionary<string, string>();
        }
        // Header for tracking and response (RCP) linkage
        public string SCode;        // unique value to tie a response to a request
        public string RCode;        // responding code
        public string ResponseKey;  // optional key to verify a response
        public uint sId;            // if there are multiple streams in one connection
        public uint pVer = 1;       // protocol version

        // Performance/metrics
        public ulong QueueTime;     // when message queued
        public ulong SendTime;      // when the message was sent
        public uint TransportClass; // priority class

        // The operation
        public uint Op;             // the operation to perform
        public string IId;          // item referenced by this operation
        public Dictionary<string, string> IProps;       // authorization for the session
        public string Auth;         // authorization for the referenced item
        public string IAuth;        // authorization for the referenced item

        public PositionBlock[] Pos; // If multi-position update, new positions for items

        // A response includes exception information.
        // No error is 'exception' being undefined.
        public string Exception;
        public Dictionary<string, string> ExceptionHints;
    }
}
