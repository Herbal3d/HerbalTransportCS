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
    public class AbCamera : AbilityBase {
        public static string PosProp = "pos";
        public static string PosToProp = "posTo";
        public static string RotProp = "rot";
        public static string RotToProp = "rotTo";
        public static string ForProp = "for";
        public static string CameraIndexProp = "cameraIndex";
        public static string CameraModeProp = "cameraMode";
        public static string CameraLookAtProp = "cameraTarget";
        public static string CameraTargetAvatarIdProp = "cameraTargetAvatarId";
        public static string CameraDisplacementProp = "camerDisplacement";

        public enum CameraModes {
            Unknown = 0,
            FirstPerson = 1,
            ThirdPerson = 2,
            FreeLook = 3,
            Orbit = 4,
            Follow = 5
        };

        public const string AbilityName = "Camera";
        public override string Name { get { return AbilityName; } }

        public double[] Pos {
            get { return P<double[]>(AbCamera.PosProp); }
            set { SetParam(AbCamera.PosProp, value); }
        }
        public static double[] GetPos(BMessage pMsg) {
            pMsg.IProps.TryGetValue(PosProp, out var Pos);
            return Pos as double[];
        }
        public double[] PosTo {
            get { return P<double[]>(AbCamera.PosToProp); }
            set { SetParam(AbCamera.PosToProp, value); }
        }
        public static double[] GetPosTo(BMessage pMsg) {
            pMsg.IProps.TryGetValue(PosToProp, out var Pos);
            return Pos as double[];
        }
        public double[] Rot {
            get { return P<double[]>(AbCamera.RotProp); }
            set { SetParam(AbCamera.RotProp, value); }
        }
        public static double[] GetRot(BMessage pMsg) {
            pMsg.IProps.TryGetValue(RotProp, out var Rot);
            return Rot as double[];
        }
        public double[] RotTo {
            get { return P<double[]>(AbCamera.RotToProp); }
            set { SetParam(AbCamera.RotToProp, value); }
        }
        public static double[] GetRotTo(BMessage pMsg) {
            pMsg.IProps.TryGetValue(RotToProp, out var Rot);
            return Rot as double[];
        }
        public int For {
            get { return P<int>(ForProp); }
            set { SetParam(ForProp, value); }
        }
        public static int GetFor(BMessage pMsg) {
            pMsg.IProps.TryGetValue(ForProp, out var For);
            return (int)For;
        }

        public int CameraIndex {
            get { return P<int>(CameraIndexProp); }
            set { SetParam(CameraIndexProp, value); }
        }
        public static int GetCameraIndex(BMessage pMsg) {
            pMsg.IProps.TryGetValue(CameraIndexProp, out var CameraIndex);
            return (int)CameraIndex;
        }
        public CameraModes CameraMode {
            get { return P<CameraModes>(CameraModeProp); }
            set { SetParam(CameraModeProp, value); }
        }
        public static CameraModes GetCameraMode(BMessage pMsg) {
            pMsg.IProps.TryGetValue(CameraModeProp, out var CameraMode);
            return (CameraModes)CameraMode;
        }

        public double[] CameraLookAt {
            get { return P<double[]>(CameraLookAtProp); }
            set { SetParam(CameraLookAtProp, value); }
        }
        public static double[] GetCameraLookAt(BMessage pMsg) {
            pMsg.IProps.TryGetValue(CameraLookAtProp, out var CameraLookAt);
            return CameraLookAt as double[];
        }
        public string CamaraTargetAvatarId {
            get { return P<string>(CameraTargetAvatarIdProp); }
            set { SetParam(CameraTargetAvatarIdProp, value); }
        }
        public static string GetCamaraTargetAvatarId(BMessage pMsg) {
            pMsg.IProps.TryGetValue(CameraTargetAvatarIdProp, out var CamaraTargetAvatarId);
            return CamaraTargetAvatarId as string;
        }
        public double[] CameraDisplacement {
            get { return P<double[]>(CameraDisplacementProp); }
            set { SetParam(CameraDisplacementProp, value); }
        }
        public static double[] GetCameraDisplacement(BMessage pMsg) {
            pMsg.IProps.TryGetValue(CameraDisplacementProp, out var CameraDisplacement);
            return CameraDisplacement as double[];
        }
    }
}
