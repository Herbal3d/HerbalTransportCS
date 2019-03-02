// Copyright (c) 2019 Robert Adams
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
using System.Linq;

namespace org.herbal3d.transport {
    public class BasilException : Exception {
        private readonly string _message;
        private readonly Dictionary<string, string> _reasonHints;

        public BasilException(string reason) {
            _message = reason;
            _reasonHints = null;
        }

        public BasilException(string reason, Dictionary<string, string> reasonHints) {
            _message = reason;
            _reasonHints = reasonHints;
        }

        public override string Message => _message;

        public override string ToString() {
            StringBuilder buff = new StringBuilder();
            buff.Append(_message);
            if (_reasonHints != null) {
                foreach (KeyValuePair<string,string> kvp in _reasonHints) {
                    buff.Append(String.Format(" {0}:{1}", kvp.Key, kvp.Value));
                }
            }
            return buff.ToString();
        }
    }
}
