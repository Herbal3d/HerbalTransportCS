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

    // Since these ability definitions mostly deal with how the ability
    //  variables show up in the BMesssage properties, this is a fake
    //  ability that encodes the BMessage properties for an OpenSession
    //  request.
    public class AbOpenSession : AbilityBase {
        public static string ClientAuthProp = "clientAuth";
        public static string ServerVersionProp = "serverVersion";
        public static string ServerAuthProp = "serverAuth";

        public const string AbilityName = "OpenSession";
        public override string Name { get { return AbilityName; } }
        public string ClientAuth {
            get { return P<string>(ClientAuthProp); }
            set { SetParam(ClientAuthProp, value); }
        }
        public static OSAuthToken GetClientAuth(BMessage pMsg) {
            if (pMsg.IProps.TryGetValue(ClientAuthProp, out var assetAuth)) {
                if (assetAuth.GetType() == typeof(OSAuthToken)) {
                    return assetAuth as OSAuthToken;
                }
                else {
                    if (assetAuth.GetType() == typeof(string)) {
                        return OSAuthToken.FromString(assetAuth as string);
                    }
                }
            };
            return null;
        }
        public string ServerVersion {
            get { return P<string>(ServerVersionProp); }
            set { SetParam(ServerVersionProp, value); }
        }
        public static string GetServerVersion(BMessage pMsg) {
            pMsg.IProps.TryGetValue(ServerVersionProp, out var serverVersion);
            return serverVersion as string;
        }
        public string ServerAuth {
            get { return P<string>(ServerAuthProp); }
            set { SetParam(ServerAuthProp, value); }
        }
        public static string GetServerAuth(BMessage pMsg) {
            pMsg.IProps.TryGetValue(ServerAuthProp, out var serverAuth);
            return serverAuth as string;
        }
        public AbOpenSession() : base() {
        }
    }
}
