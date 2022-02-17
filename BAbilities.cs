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
using org.herbal3d.cs.CommonUtil;
using org.herbal3d.OSAuth;

namespace org.herbal3d.b.protocol {

    /// <summary>
    /// These classes are used to specify and collect the parameters
    /// for abilities and making them available for message calls.
    /// AbilityList can collect multiple abiliities for sending
    /// together. So the usage is usually:
    /// <pre>
    ///     AbilityList params = new AbilityList();
    ///     params.Add(new AbilityInstance() {
    ///         pos = [ 1, 2, 3 ]
    ///     });
    ///     params.Add(new AbilityAssembly() {
    ///         AssetURL = "https://example.com/assetPlace/1234567890.gltf"
    ///     });
    ///     await pConnection.CreateItem(params);
    /// </pre>
    /// </summary>

    // Collect Abilities into a ParamBlock
    // This also creates an "Abilities" property with the names of the added abilities.
    public class AbilityList : ParamBlock {
        public static string AbilitiesProp = "abilities";

        public AbilityList() : base() {
        }
        public string Abilities {
            get { return P<string>(AbilitiesProp); }
            set { SetParam(AbilitiesProp, value); }
        }

        public ParamBlock Add(AbilityBase pAbil) {
            string abils = this.Abilities;
            // Add the new ability the the list of abilities
            if (abils != null) {
                abils += "," + pAbil.Name;
            }
            else {
                abils = pAbil.Name;
            }
            this.Abilities = abils;
            // Add the properties of the new ability
            foreach (var kvp in pAbil.Params) {
                this.Add(kvp.Key, kvp.Value);
            }
            return this;
        }
    }

    /// <summary>
    /// The base of the ability definitions.
    /// </summary>
    abstract public class AbilityBase: ParamBlock {
        public abstract string Name { get; }
    }

    /// <summary>
    /// The base properties of a BItem.
    /// These are usually read and cannot be written.
    /// </summary>
    public class AbilityBItem : AbilityBase {
        public static string IdProp = "id";
        public static string LayerProp = "layer";
        public static string StateProp = "state";

        public const string AbilityName = "BItem";
        public override string Name { get { return AbilityName; } }

        public string Id {
            get { return P<string>(IdProp); }
            set { SetParam(IdProp, value); }
        }
        public static string GetId(BMessage pMsg) {
            pMsg.IProps.TryGetValue(IdProp, out var id);
            return id as string;
        }
        public string Layer {
            get { return P<string>(LayerProp); }
            set { SetParam(LayerProp, value); }
        }
        public static string GetLayer(BMessage pMsg) {
            pMsg.IProps.TryGetValue(LayerProp, out var id);
            return id as string;
        }

        public string State {
            get { return P<string>(StateProp); }
            set { SetParam(StateProp, value); }
        }
        public static string GetState(BMessage pMsg) {
            pMsg.IProps.TryGetValue(StateProp, out var id);
            return id as string;
        }

        public AbilityBItem(): base() {
        }
    }

    public class AbilityAssembly : AbilityBase {
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

        public AbilityAssembly() : base() {
        }
    }
    public class AbilityInstance : AbilityBase {
        public static string RefItemProp = "refItem"; // either 'SELF' or id of BItem with the geometry
        public static string PosProp = "pos";
        public static string RotProp = "rot";
        public static string PosRefProp = "posRef";
        public static string RotRefProp = "rotRef";

        public const string AbilityName = "Instance";
        public override string Name { get { return AbilityName; } }
        public string RefItem {
            get { return P<string>(RefItemProp); }
            set { SetParam(RefItemProp, value); }
        }
        public static string GetRefItem(BMessage pMsg) {
            pMsg.IProps.TryGetValue(RefItemProp, out var refItem);
            return refItem as string;
        }
        public double[] Pos {
            get { return P<double[]>(PosProp); }
            set { SetParam(PosProp, value); }
        }
        public static double[] GetPos(BMessage pMsg) {
            pMsg.IProps.TryGetValue(PosProp, out var pos);
            return pos as double[];
        }
        public double[] Rot {
            get { return P<double[]>(RotProp); }
            set { SetParam(RotProp, value); }
        }
        public static double[] GetRot(BMessage pMsg) {
            pMsg.IProps.TryGetValue(RotProp, out var rot);
            return rot as double[];
        }
        public CoordSystem PosRef {
            get { return P<CoordSystem>(PosRefProp); }
            set { SetParam(PosRefProp, value); }
        }
        public static CoordSystem GetPosRef(BMessage pMsg) {
            pMsg.IProps.TryGetValue(PosRefProp, out var posRef);
            return (CoordSystem)posRef;
        }
        public RotationSystem RotRef {
            get { return P<RotationSystem>(RotRefProp); }
            set { SetParam(RotRefProp, value); }
        }
        public static RotationSystem GetRotRef(BMessage pMsg) {
            pMsg.IProps.TryGetValue(RotRefProp, out var rotRef);
            return (RotationSystem)rotRef;
        }
        public AbilityInstance() : base() {
        }
    }
    // Since these ability definitions mostly deal with how the ability
    //  variables show up in the BMesssage properties, this is a fake
    //  ability that encodes the BMessage properties for an OpenSession
    //  request.
    public class AbilityOpenSession : AbilityBase {
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
        public AbilityOpenSession() : base() {
        }
    }
}
