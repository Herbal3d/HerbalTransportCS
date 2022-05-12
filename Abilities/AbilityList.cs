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
            // Add the 'abilities' property to this block which will return the list of added abilities
            Add(AbilitiesProp, new string[] { });
        }

        // Will hold the names of the added abilities (for easy addition)
        List<string> _abilities = new List<string>();
        public ParamBlock Add(AbilityBase pAbil) {
            // Add the name of the ability to the list of abilities
            _abilities.Add(pAbil.Name);
            // Set the value of the parameter in the ParamBlock with the new list
            SetParam(AbilitiesProp, _abilities.ToArray());
            // Add the properties of the new ability
            foreach (var kvp in pAbil.Params) {
                Add(kvp.Key, kvp.Value);
            }
            return this;
        }
    }
}
