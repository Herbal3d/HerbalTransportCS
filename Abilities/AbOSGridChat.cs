// Copyright (c) 2022 Robert Adams
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
using org.herbal3d.b.protocol;
using org.herbal3d.cs.CommonUtil;
using org.herbal3d.OSAuth;

namespace org.herbal3d.b.protocol {

    public class AbOSGridChat : AbilityBase {
        public static string OSChatFromAgentIdProp = "OSChatFromAgentId";
        public static string OSChatFromAgentNameProp = "OSChatFromAgentName";
        public static string OSChatToAgentIdProp = "OSChatToAgentId";
        public static string OSChatDialogProp = "OSChatDialog";
        public static string OSChatFromGroupProp = "OSChatFromGroup";
        public static string OSChatMessageProp = "OSChatMessage";
        public static string OSChatImSessionIdProp = "OSChatImSessionId";
        public static string OSChatOfflineProp = "OSChatOffline";
        public static string OSChatPositionProp = "OSChatPosition";
        public static string OSChatParentEstateIdProp = "OSChatParentEstateId";
        public static string OSChatRegionIdProp = "OSChatRegionId";
        public static string OSChatTimestampProp = "OSChatTimestamp";
        public static string OSChatUnhandledProp = "OSChatUnhandled";

        public const string AbilityName = "OSGridChat";
        public override string Name { get { return AbilityName; } }

        public string OSChatFromAgentId {
            get { return P<string>(OSChatFromAgentIdProp); }
            set { SetParam(OSChatFromAgentIdProp, value); }
        }
        public string OSChatFromAgentName {
            get { return P<string>(OSChatFromAgentNameProp); }
            set { SetParam(OSChatFromAgentNameProp, value); }
        }
        public string OSChatToAgentId {
            get { return P<string>(OSChatToAgentIdProp); }
            set { SetParam(OSChatToAgentIdProp, value); }
        }
        public byte OSChatDialog {
            get { return P<byte>(OSChatDialogProp); }
            set { SetParam(OSChatDialogProp, value); }
        }
        public bool OSChatFromGroup {
            get { return P<bool>(OSChatFromGroupProp); }
            set { SetParam(OSChatFromGroupProp, value); }
        }
        public string OSChatMessage {
            get { return P<string>(OSChatMessageProp); }
            set { SetParam(OSChatMessageProp, value); }
        }
        public string OSChatImSessionId {
            get { return P<string>(OSChatImSessionIdProp); }
            set { SetParam(OSChatImSessionIdProp, value); }
        }
        public byte OSChatOffline {
            get { return P<byte>(OSChatOfflineProp); }
            set { SetParam(OSChatOfflineProp, value); }
        }
        public string OSChatPosition {
            get { return P<string>(OSChatPositionProp); }
            set { SetParam(OSChatPositionProp, value); }
        }
        public string OSChatParentEstateId {
            get { return P<string>(OSChatParentEstateIdProp); }
            set { SetParam(OSChatParentEstateIdProp, value); }
        }
        public string OSChatRegionId {
            get { return P<string>(OSChatRegionIdProp); }
            set { SetParam(OSChatRegionIdProp, value); }
        }
        public uint OSChatTimestamp {
            get { return P<uint>(OSChatTimestampProp); }
            set { SetParam(OSChatTimestampProp, value); }
        }
        public bool OSChatUnhandled {
            get { return P<bool>(OSChatUnhandledProp); }
            set { SetParam(OSChatUnhandledProp, value); }
        }
    }
}

