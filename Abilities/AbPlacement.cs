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

    // A BItem can be placed in the world. This encodes placement information.
    public class AbPlacement : AbilityBase {
        public static string PosProp = "pos";
        public static string RotProp = "rot";
        public static string ForProp = "for";

        public const string AbilityName = "Placement";
        public override string Name { get { return AbilityName; } }
        public double[] WorldPos {
            get { return P<double[]>(PosProp); }
            set { SetParam(PosProp, value); }
        }
        public static double[] GetWorldPos(BMessage pMsg) {
            pMsg.IProps.TryGetValue(PosProp, out var pos);
            return pos as double[];
        }
        public double[] WorldRot {
            get { return P<double[]>(RotProp); }
            set { SetParam(RotProp, value); }
        }
        public static double[] GetWorldRot(BMessage pMsg) {
            pMsg.IProps.TryGetValue(RotProp, out var rot);
            return rot as double[];
        }
        public int ForRef {
            get { return P<int>(ForProp); }
            set { SetParam(ForProp, value); }
        }
        public AbPlacement() : base() {
        }
    }
}
