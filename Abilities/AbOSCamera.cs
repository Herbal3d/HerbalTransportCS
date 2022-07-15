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

    public class AbOSCamera : AbilityBase {
        public static string OSCameraModeProp = "OSCameraMode";
        public static string OSCameraDisplacementProp = "OSCameraDisplacement";

        public enum OSCameraModes {
            First = 1,
            Third = 2,
            Orbit = 3
        };

        public const string AbilityName = "OSCamera";
        public override string Name { get { return AbilityName; } }

        public OSCameraModes OSCameraMode {
            get { return P<OSCameraModes>(OSCameraModeProp); }
            set { SetParam(OSCameraModeProp, value); }
        }
        public static OSCameraModes GetOSCameraMode(BMessage pMsg) {
            pMsg.IProps.TryGetValue(OSCameraModeProp, out var OSCameraMode);
            return (OSCameraModes)OSCameraMode;
        }

        public double[] OSCameraDisplacement {
            get { return P<double[]>(OSCameraDisplacementProp); }
            set { SetParam(OSCameraDisplacementProp, value); }
        }
        public static double[] GetOSCameraDisplacement(BMessage pMsg) {
            pMsg.IProps.TryGetValue(OSCameraDisplacementProp, out var OSCameraDisplacement);
            return (double[])OSCameraDisplacement;
        }

    }
}
