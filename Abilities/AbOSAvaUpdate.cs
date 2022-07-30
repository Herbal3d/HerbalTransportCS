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

    public class AbOSAvaUpdate : AbilityBase {
        public const string ControlFlagProp = "osau_controlflag";
        public const string BodyRotProp = "osau_bodyrot";
        public const string HeadRotProp = "osau_headrot";
        public const string FarProp = "osau_far";

        public const string MoveToProp = "osau_moveto";

        public const string AbilityName = "OSAvaUpdate";
        public override string Name { get { return AbilityName; } }

        public enum OSAvaUpdateMoveAction {
            None            = 0,
            WalkForward     = 0x0001,
            WalkBackward    = 0x0002,
            TurnLeft        = 0x0004,
            TurnRight       = 0x0008,
            MoveUp          = 0x0010,
            MoveDown        = 0x0020,
            Fly             = 0x0040,
            Stand           = 0x0080,
        };

        public uint ControlFlag {
            get { return P<uint>(ControlFlagProp); }
            set { SetParam(ControlFlagProp, value); }
        }
        public static uint GetControlFlag(BMessage pMsg) {
            pMsg.IProps.TryGetValue(ControlFlagProp, out var controlFlag);
            return (uint)controlFlag;
        }
        public double[] BodyRot {
            get { return P<double[]>(BodyRotProp); }
            set { SetParam(BodyRotProp, value); }
        }
        public static double[] GetBodyRot(BMessage pMsg) {
            pMsg.IProps.TryGetValue(BodyRotProp, out var bodyRot);
            return (double[])bodyRot;
        }
        public double[] HeadRot {
            get { return P<double[]>(HeadRotProp); }
            set { SetParam(HeadRotProp, value); }
        }
        public static double[] GetHeadRot(BMessage pMsg) {
            pMsg.IProps.TryGetValue(HeadRotProp, out var headRot);
            return (double[])headRot;
        }
        public float Far {
            get { return P<float>(FarProp); }
            set { SetParam(FarProp, value); }
        }
        public static float GetFar(BMessage pMsg) {
            pMsg.IProps.TryGetValue(FarProp, out var far);
            return (float)far;
        }
        public double[] MoveTo {
            get { return P<double[]>(MoveToProp); }
            set { SetParam(MoveToProp, value); }
        }
        public static double[] GetMoveTo(BMessage pMsg) {
            pMsg.IProps.TryGetValue(MoveToProp, out var MoveTo);
            return (double[])MoveTo;
        }
    }
}
