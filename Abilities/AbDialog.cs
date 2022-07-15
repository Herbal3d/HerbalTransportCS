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
    public class AbDialog : AbilityBase {
        public static string DialogUrlProp = "dialogUrl";
        public static string DialogNameProp = "dialogName";
        public static string DialogPlacementProp = "dialogPlacement";

        public const string AbilityName = "Dialog";
        public override string Name { get { return AbilityName; } }
        public string DialogUrl {
            get { return P<string>(DialogUrlProp); }
            set { SetParam(DialogUrlProp, value); }
        }
        public static string GetDialogUrl(BMessage pMsg) {
            pMsg.IProps.TryGetValue(DialogUrlProp, out var DialogUrl);
            return DialogUrl as string;
        }
        public string DialogName {
            get { return P<string>(DialogNameProp); }
            set { SetParam(DialogNameProp, value); }
        }
        public static string GetDialogName(BMessage pMsg) {
            pMsg.IProps.TryGetValue(DialogNameProp, out var DialogName);
            return DialogName as string;
        }
        public string DialogPlacement {
            get { return P<string>(DialogPlacementProp); }
            set { SetParam(DialogPlacementProp, value); }
        }
        public static string GetDialogPlacement(BMessage pMsg) {
            pMsg.IProps.TryGetValue(DialogPlacementProp, out var DialogPlacement);
            return DialogPlacement as string;
        }

    }
}
