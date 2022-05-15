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

namespace org.herbal3d.b.protocol {

    /// <summary>
    /// The base properties of a BItem.
    /// These are usually read and cannot be written.
    /// </summary>
    public class AbBItem : AbilityBase {
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

        public AbBItem() : base() {
        }
    }
}
