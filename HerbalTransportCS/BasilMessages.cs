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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Herbal3d.HerbalTransportCS {

    public enum BasilMessageOps {
        UnknownReq = 0x00000,
        CreateItemReq               = 101,
        CreateItemResp              = 102,
        DeleteItemReq               = 103,
        DeleteItemResp              = 104,
        AddAbilityReq               = 105,
        AddAbilityResp              = 106,
        RemoveAbilityReq            = 107,
        RemoveAbilityResp           = 108,
        RequestPropertiesReq        = 109,
        RequestPropertiesResp       = 110,
        UpdatePropertiesReq         = 111,
        UpdatePropertiesResp        = 112,

        OpenSessionReq              = 201,
        OpenSessionResp             = 202,
        CloseSessionReq             = 203,
        CloseSessionResp            = 204,
        MakeConnectionReq           = 205,
        MakeConnectionResp          = 206,

        AliveCheckReq               = 301,
        AliveCheckResp              = 302,
    }

    public enum CoordSystem {
        WGS86       = 0,    // WGS84 earth coordinates
        CAMERA      = 1,    // Coordinates relative to camera position (-1..1 range, zero center)
        CAMERAABS   = 2,    // Absolute coordinates relative to the camera position (zero center)
        VIRTUAL     = 3,    // Zero based un-rooted coordinates
        MOON        = 4,    // Earth-moon coordinates
        MARS        = 5,    // Mars coordinates
        REL1        = 6,    // Mutually agreed base coordinates
        REL2        = 7,
        REL3        = 8
    }

    public enum RotationSystem {
        WORLDR      = 0,    // rotation is relative to world coordinates
        LOCALR      = 1,    // rotation is relative to referened object
        FORR        = 2,    // rotation is relative to current frame of reference
        CAMERAR     = 3     // rotation is relative to the camera direction
    }

    public class BasilMessage {
        // Header for tracking and response (RCP) linkage
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ulong resp;          // unique value to tie a response to a request
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string respKey;      // optional key to verify a response
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public uint sId;            // if there are multiple streams in one connection
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public uint pVer = 1;       // protocol version

        // Change ordering
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ulong chSeq;         // Change sequence number
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ulong chDate;        // Change date (UNIX UTC time)

        // Performance/metrics
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ulong queueTime;     // when message queued
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ulong sendTime;      // when the message was sent
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public uint transportClass; // priority class

        // Request trace information
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ulong traceId;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ulong parentSpanId;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ulong spanId;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool sampled;

        // The operation
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public uint op;             // the operation to perform
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string sAuth;        // authorization for the session
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string itemId;       // item referenced by this operation
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public uint itemIdN;        // item referenced by this operation
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string iAuth;        // authorization for the referenced item
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> iprops;    // item properties being modified
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> aprops;    // item properties being modified

        // A response includes exception information.
        // No error is 'exception' being undefined.
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string exception;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> exceptionHints;

        // Startup initialization for static tables
        static BasilMessage() {

            // Build NameToCode table from CodeToName table (saves keeping two tables in sync)
            foreach (var kvp in BasilMessageCodeToName) {
                BasilMessageNameToCode.Add(kvp.Value, kvp.Key);
            }
            
        }

        // Convenience table to map message operation codes to names
        public static Dictionary<uint, string> BasilMessageCodeToName = new Dictionary<uint, string>() {
            { (uint)BasilMessageOps.CreateItemReq, "CreateItemReq" },
            { (uint)BasilMessageOps.CreateItemResp, "CreateItemResp" },
            { (uint)BasilMessageOps.DeleteItemReq, "DeleteItemReq" },
            { (uint)BasilMessageOps.DeleteItemResp, "DeleteItemResp" },
            { (uint)BasilMessageOps.AddAbilityReq, "AddAbilityReq" },
            { (uint)BasilMessageOps.AddAbilityResp, "AddAbilityResp" },
            { (uint)BasilMessageOps.RemoveAbilityReq, "RemoveAbilityReq" },
            { (uint)BasilMessageOps.RemoveAbilityResp, "RemoveAbilityResp" },
            { (uint)BasilMessageOps.RequestPropertiesReq, "RequestPropertiesReq" },
            { (uint)BasilMessageOps.RequestPropertiesResp, "RequestPropertiesResp" },
            { (uint)BasilMessageOps.UpdatePropertiesReq, "UpdatePropertiesReq" },
            { (uint)BasilMessageOps.UpdatePropertiesResp, "UpdatePropertiesResp" },

            { (uint)BasilMessageOps.OpenSessionReq, "OpenSessionReq" },
            { (uint)BasilMessageOps.OpenSessionResp, "OpenSessionResp" },
            { (uint)BasilMessageOps.CloseSessionReq, "CloseSessionReq" },
            { (uint)BasilMessageOps.CloseSessionResp, "CloseSessionResp" },
            { (uint)BasilMessageOps.MakeConnectionReq, "MakeConnectionReq" },
            { (uint)BasilMessageOps.MakeConnectionResp, "MakeConnectionResp" },

            { (uint)BasilMessageOps.AliveCheckReq, "AliveCheckReq" },
            { (uint)BasilMessageOps.AliveCheckResp, "AliveCheckResp" },
        };

        // Convenience table to map message operation names to codes (build from CodeToName table)
        public static Dictionary<string, uint> BasilMessageNameToCode = new Dictionary<string, uint>();

    }
}
