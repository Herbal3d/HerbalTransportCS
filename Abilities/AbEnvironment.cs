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
    public class AbEnvironment : AbilityBase {
        public static string SolarAzimuthProp = "solarAzimuth";
        public static string SkyTurbidityProp = "skyTurbidity";
        public static string SkyRayleighProp = "skyRayleigh";

        public const string AbilityName = "Environ";
        public override string Name { get { return AbilityName; } }
        public int SolarAzimuth {
            get { return P<int>(SolarAzimuthProp); }
            set { SetParam(SolarAzimuthProp, value); }
        }
        public static int GetSolarAzimuth(BMessage pMsg) {
            pMsg.IProps.TryGetValue(SolarAzimuthProp, out var SolarAzimuth);
            return (int)SolarAzimuth;
        }

        public int SkyTurbidity {
            get { return P<int>(SkyTurbidityProp); }
            set { SetParam(SkyTurbidityProp, value); }
        }
        public static int GetSkyTurbidity(BMessage pMsg) {
            pMsg.IProps.TryGetValue(SkyTurbidityProp, out var SkyTurbidity);
            return (int)SkyTurbidity;
        }

        public int SkyRayleigh {
            get { return P<int>(SkyRayleighProp); }
            set { SetParam(SkyRayleighProp, value); }
        }
        public static int GetSkyRayleigh(BMessage pMsg) {
            pMsg.IProps.TryGetValue(SkyRayleighProp, out var SkyRayleigh);
            return (int)SkyRayleigh;
        }

    }
}
