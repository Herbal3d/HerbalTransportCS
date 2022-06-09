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

    public class AbAssembly : AbilityBase {
        public static string AssetURLProp = "assetUrl";
        public static string AssetLoaderProp = "assetLoader";
        public static string AssetAuthProp = "assetAuth";
        public static string AssetRepresentationProp = "assetRepresentation";

        public const string AbilityName = "Assembly";
        public override string Name { get { return AbilityName; } }
        public string AssetURL {
            get { return P<string>(AssetURLProp); }
            set { SetParam(AssetURLProp, value); }
        }
        public static string GetAssetURL(BMessage pMsg) {
            pMsg.IProps.TryGetValue(AssetURLProp, out var assetURL);
            return assetURL as string;
        }
        public string[] AssetLoaderValues = new string[] { "gltf" };
        public string AssetLoader {
            get { return P<string>(AssetLoaderProp); }
            set { SetParam(AssetLoaderProp, value); }
        }
        public static string GetAssetLoader(BMessage pMsg) {
            pMsg.IProps.TryGetValue(AssetLoaderProp, out var assetLoader);
            return assetLoader as string;
        }
        public string AssetAuth {
            get { return P<string>(AssetAuthProp); }
            set { SetParam(AssetAuthProp, value); }
        }
        public static OSAuthToken GetAssetAuth(BMessage pMsg) {
            if (pMsg.IProps.TryGetValue(AssetAuthProp, out var assetAuth)) {
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
        public object AssetRepresentation {
            get { return GetValue(AssetRepresentationProp); }
            set { SetParam(AssetRepresentationProp, value); }
        }
        public static object GetAssetRepresentation(BMessage pMsg) {
            pMsg.IProps.TryGetValue(AssetRepresentationProp, out var assetRep);
            return assetRep;
        }

        public AbAssembly() : base() {
        }
    }
}
