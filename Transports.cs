// Copyright (c) 2021 Robert Adams
//
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
using System.Threading;
using System.Threading.Tasks;

using org.herbal3d.cs.CommonUtil;

namespace org.herbal3d.transport {
    public static class Transports {
        public delegate void CreateSpaceServerProcessor(BTransport pTransport,
                                                        CancellationTokenSource pCanceller,
                                                        ParamBlock pListenerParams);
        public static void AcceptConnection(ParamBlock pParams, CancellationTokenSource pCanceller,
                                        BLogger pDebugLogger, CreateSpaceServerProcessor pProcessor) {
        }
    }
}
