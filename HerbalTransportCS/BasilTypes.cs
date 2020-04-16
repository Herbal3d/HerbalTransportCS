// Copyright (c) 2020 Robert Adams
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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace org.herbal3d.basil.protocol.BasilType {

    public class ItemId {
        public ItemId(string pId) { Id = pId; }
        public string Id;
        public override string ToString() { return Id; }
    }

    /*  Just use OSAuthToken
    public class Auth {
        public Auth(string pToken) { AuthToken = pToken; }
        public string AuthToken;
        public override string ToString() { return AuthToken; }
    }
    */

    public class Props : Dictionary<string, string>, IDictionary<string,string> {
        public Props() { }
        public Props(IEnumerable<KeyValuePair<string,string>> pOther) {
            foreach (var kvp in pOther) {
                this.Add(kvp.Key, kvp.Value);
            }
        }
        public Props Add(IEnumerable<KeyValuePair<string, string>> pOther) {
            foreach (var kvp in pOther) {
                this.Add(kvp.Key, kvp.Value);
            }
            return this;
        }
    }
    public class ParamBlock {
        public string Ability;
        public Props Props;
    }

    public class AbilityList : List<AbilityBase> {
        public static Dictionary<string, string> AbilityNames = new Dictionary<string, string> {
            { "AbilityDisplayable", "DISP" },
            { "AbilityInstance", "INST" },
            { "AbilityCamera", "CAM" }
        };
    }

    public class PositionBlock {
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning restore IDE0067 // parameter is never used
        public  double[] Pos;
        public double[] Rot;
        public int PosRef;
        public int RotRef;
        public double[] Vel;
        public double[] Path;
        public Int32 ItemIdN;
        public string ItemId;
        public string SessionAuth;
        public string ItemAuth;
#pragma warning restore IDE0067 // parameter is never used
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning restore IDE0051 // Remove unused private members

        public static Dictionary<string, int> PosRefCode = new Dictionary<string, int> {
            { "WSG86", 0 },
            { "CAMERA", 1 },
            { "CAMERAABS", 2 },
            { "VIRTUAL", 3 },
            { "MOON", 4 },
            { "MARS", 5 },
            { "REL1", 6 },
            { "REL2", 7 },
            { "REL3", 8 }
        };
        public static Dictionary<string, int> RotRefCode = new Dictionary<string, int> {
            { "WORLDR", 0 },
            { "LOCALR", 1 },
            { "FORR", 2 },
            { "CAMERAR", 3 }
        };
    }

    // =============================================================================
    // The following 'ability' definitions are to capture the parameters and defintions
    // of the abilities used so they are easier to create and manipulate.
    public abstract class AbilityBase {
        // Each Ability has a code name
        public string AbilityCode = "XXXX";

        // Each property of sub-classed abilities is defined by a name and getters and setters
        protected   struct PropDefn {
            public delegate string GetStringFunct();
            public delegate double[] GetDoubleFunct();
            public delegate void SetStringFunct(string xx);
            public delegate void SetDoubleFunct(double[] dd);
            public  PropDefn(string pN, GetStringFunct pGS, GetDoubleFunct pGD, SetStringFunct pSS, SetDoubleFunct pSD) {
                PropName = pN;
                GetStringValue = pGS;
                GetDoubleValue = pGD;
                SetStringValue = pSS;
                SetDoubleValue = pSD;
            }
            public  string PropName;
            public  GetStringFunct GetStringValue;
            public  GetDoubleFunct  GetDoubleValue;
            public  SetStringFunct SetStringValue;
            public  SetDoubleFunct SetDoubleValue;
        };
        protected  Dictionary<string, PropDefn> AbilityProps = new Dictionary<string, PropDefn>();
        protected void AddToProps(IDictionary<string,string> pProps, Dictionary<string,PropDefn> pDefn) {
            foreach (var kvp in pDefn) {
                string val = kvp.Value.GetStringValue();
                if (!String.IsNullOrEmpty(val)) {
                    pProps.Add(kvp.Value.PropName, val);
                }
            }
        }
        protected void LoadFromProps(IDictionary<string,string> pProps, Dictionary<string,PropDefn> pDefn) {
            foreach (var kvp in pProps) {
                if (kvp.Value != null) {
                    if (pDefn.TryGetValue(kvp.Key.ToLower(), out PropDefn defn) ) {
                        defn.SetStringValue(kvp.Value);
                    }
                }
            }
        }
        // Must be overridden by sub-classes
        public void AddToProps(IDictionary<string, string> pProps) {
            AddToProps(pProps, AbilityProps);
        }
        // Must be overridden by sub-classes
        public void LoadFromProps(IDictionary<string, string> pProps) {
            AddToProps(pProps, AbilityProps);
        }
        // Return a ParamBlock for this Ability
        public static string VectorToString(double[] pVect) {
            string ret = null;
            if (pVect != null && pVect.Length >= 3) {
                ret = String.Format("[{0},{1},{2}]", pVect[0], pVect[1], pVect[2]);
            }
            return ret;
        }
        public static double[] VectorFromString(string pStr) {
            return DoublesFromString(pStr, 3);
        }
        public static string RotationToString(double[] pVect) {
            string ret = null;
            if (pVect != null && pVect.Length >= 4) {
                ret = String.Format("[{0},{1},{2},{3}]", pVect[0], pVect[1], pVect[2], pVect[3]);
            }
            return ret;
        }
        public static double[] RotationFromString(string pStr) {
            return DoublesFromString(pStr, 4);
        }
        public static string AabbToString(double[] pVect) {
            string ret = null;
            if (pVect != null && pVect.Length >= 6) {
                ret = String.Format("[{0},{1},{2},{3},{4},{5}]",
                    pVect[0], pVect[1], pVect[2], pVect[3], pVect[4], pVect[5]);
            }
            return ret;
        }
        public static double[] AabbFromString(string pStr) {
            return DoublesFromString(pStr, 6);
        }
        private static double[] DoublesFromString(string pStr, int pLen) {
            double[] ret = null;
            try {
                if (pStr != null && pStr.Trim().StartsWith("[")) {
                    string[] pieces = pStr.Replace("[", "").Replace("]", "").Split(',');
                    if (pieces.Length == pLen) {
                        ret = pieces.Select(nn => { return Double.Parse(nn); }).ToArray();
                    }
                }
            }
            catch (Exception) {
                ret = null;
            }
            return ret;
        }
    }
    public class AbilityDisplayable : AbilityBase {
        public string Code = "DISP";
        public string ItemId;           // ID of Item this is associated with
        public string DisplayableType;  // Type of displayable
        public double[] Aabb;       // six doubles make up two vectors defining corners
        public string DisplayableUrl;          // URL to the displayable information
        public string DisplayableAuth;         // Premission token for access URL
        public string LoaderType;   // Loader to use

        // For documentation, the list of loaders
        public List<string> LoaderTypes = new List<string>() {
            "GLTF",
            "Collada",
            "FBX",
            "OBJ",
            "BVH"
        };

        // For documentation, the list of available types
        public List<string> DisplayableTypes = new List<string>() {
            "MeshSet"
        };

        public AbilityDisplayable()  {
            AbilityCode = Code;
            InitTable();
        }
        private void InitTable() {
            AbilityProps.Add("itemid", new PropDefn("ItemId",
                                                    () => { return ItemId; },
                                                    null,
                                                    x => { ItemId = x; },
                                                    null));
            AbilityProps.Add("displayabletype", new PropDefn("DisplayableType",
                                                    () => { return DisplayableType; },
                                                    null,
                                                    x => { DisplayableType = x; },
                                                    null));
            AbilityProps.Add("displayableaabb", new PropDefn("DisplayableAabb",
                                                    () => { return AabbToString(Aabb); },
                                                    () => { return Aabb; },
                                                    x => { Aabb = AabbFromString(x); },
                                                    x => { Aabb = x; }));
            AbilityProps.Add("displayableurl", new PropDefn("DisplayableUrl",
                                                    () => { return DisplayableUrl; },
                                                    null,
                                                    x => { DisplayableUrl = x; },
                                                    null));
            AbilityProps.Add("displayableauth", new PropDefn("DisplayableAuth",
                                                    () => { return DisplayableAuth; },
                                                    null,
                                                    x => { DisplayableAuth = x; },
                                                    null));
            AbilityProps.Add("loadertype", new PropDefn("LoaderType",
                                                    () => { return LoaderType; },
                                                    null,
                                                    x => { LoaderType = x; },
                                                    null));
        }
        public AbilityDisplayable(IDictionary<string, string> pProps) {
            AbilityCode = Code;
            InitTable();
            this.LoadFromProps(pProps);
        }
    }

    public class AbilityInstance : AbilityBase {
        public string Code = "INST";

        public ItemId DisplayableItemId = new ItemId(null);
        public double[] Pos = new double[] { 0,0,0 };
        public double[] Rot = new double[] { 0,0,0,1 };
        public int PosRef = PositionBlock.PosRefCode["WSG86"];
        public int RotRef = PositionBlock.RotRefCode["WORLDR"];

        public AbilityInstance() {
            AbilityCode = Code;
            InitTable();
        }
        private void InitTable() {
            AbilityProps.Add("displayableItemId", new PropDefn("DisplayableItemId",
                                                    () => { return DisplayableItemId.Id; },
                                                    null ,
                                                    x => { DisplayableItemId = new ItemId(x); },
                                                    null ));
            AbilityProps.Add("pos", new PropDefn("Pos",
                                                    () => { return VectorToString(Pos); },
                                                    () => { return Pos; },
                                                    x => { Pos = VectorFromString(x); },
                                                    x => { Pos = x; }));
            AbilityProps.Add("rot", new PropDefn("Rot",
                                                    () => { return RotationToString(Rot); },
                                                    () => { return Rot; },
                                                    x => { Rot = RotationFromString(x); },
                                                    x => { Rot = x; }));
            AbilityProps.Add("posref", new PropDefn("PosRef",
                                                    () => { return PosRef.ToString(); },
                                                    null,
                                                    x => { PosRef = Int32.Parse(x); },
                                                    null));
            AbilityProps.Add("rotref", new PropDefn("RotRef",
                                                    () => { return RotRef.ToString(); },
                                                    null,
                                                    x => { RotRef = Int32.Parse(x); },
                                                    null));
        }
        public AbilityInstance(IDictionary<string, string> pProps) {
            AbilityCode = Code;
            InitTable();
            this.LoadFromProps(pProps);
        }
    }

    public class AbilityCamera : AbilityBase {
        public string Code = "CAM";

        public double[] Pos = new double[] { 0,0,0 };
        public double[] Rot = new double[] { 0,0,0,1 };
        public int PosSystem = PositionBlock.PosRefCode["WSG86"];
        public int RotSystem = PositionBlock.RotRefCode["WORLDR"];

        public AbilityCamera() {
            AbilityCode = Code;
            InitTable();
        }
        private void InitTable() {
            AbilityProps.Add("pos", new PropDefn("Pos", () => { return AabbToString(Pos); }, () => { return Pos; },
                                                    x => { Pos = AabbFromString(x); }, x => { Pos = x; }));
            AbilityProps.Add("rot", new PropDefn("Rot", () => { return AabbToString(Rot); }, () => { return Rot; },
                                                    x => { Rot = AabbFromString(x); }, x => { Rot = x; }));
            AbilityProps.Add("posref", new PropDefn("PosRef", () => { return PosSystem.ToString(); }, null,
                                                    x => { PosSystem = Int32.Parse(x); }, null));
            AbilityProps.Add("rotref", new PropDefn("RotRef", () => { return RotSystem.ToString(); }, null,
                                                    x => { RotSystem = Int32.Parse(x); }, null));
        }
        public AbilityCamera(IDictionary<string, string> pProps) {
            AbilityCode = Code;
            InitTable();
            this.LoadFromProps(pProps);
        }
    }

}
