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
using System.Text;

using org.herbal3d.b.protocol;
using org.herbal3d.cs.CommonUtil;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace org.herbal3d.transport {
    /**
     * Receive a JSON transport body and pass a BMessage up the stack.
     */

    public class BProtocolJSON : BProtocol {

        public BProtocolJSON(ParamBlock pParams,
                            BTransport pTransport,
                            BLogger pLogger) : base(pTransport, "JSON", pLogger) {

            // set up to receive messages
            pTransport.SetReceiveCallback(ProcessOnMsg, this);
        }

        public override void Send(BMessage pData) {
            // convert the BMessage to JSON buffer
            string asJSON = JsonConvert.SerializeObject(pData);
            Log.Debug("BProtocolJSON.Send: Sending {0}", asJSON);
            Transport?.Send(Encoding.UTF8.GetBytes(asJSON));
        }

        // public override void Start() {
        // }

        /* JSON parsing converter code. Discovered not needed but kept here as an example
        /// <summary>
        /// The BMessage has an IProps field which is a keyed collection of values.
        /// Depending on the encoding of the message, the type of the values can be
        ///     coded differently. Currently, the values are either a string or an array
        ///     of doubles.
        /// This class adds a parser for that case where we check the syntax of the
        ///     value and return the correct type of value.
        /// </summary>
        private class IPropsConverter : JsonConverter {
            public override bool CanConvert(Type objectType) {
                DebugLogger.Debug("BProtocolJSON.IPropsConverter: CanConvert. type={0}", objectType.ToString());
                return objectType == typeof(Dictionary<string,object>);
            }
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
                DebugLogger.Debug("BProtocolJSON.IPropsConverter: ReadJson. type={0}, existing={1}",
                            objectType.ToString(), JsonConvert.SerializeObject(existingValue, Formatting.None));
                Dictionary<string,object> ret = new Dictionary<string, object>();
                JObject packed = JObject.Load(reader);
                if (packed != null) {
                    DebugLogger.Debug("BProtocolJSON.IPropsConverter: packed token type = {0}", packed.GetType());
                    foreach (var prop in packed.Properties()) {
                        DebugLogger.Debug("BProtocolJSON.IPropsConverter: prop name = {0}", prop.Name);
                        JToken val = prop.Value;
                        if (val.Type == JTokenType.String) {
                            DebugLogger.Debug("BProtocolJSON.IPropsConverter: string {0}={1}", prop.Name, (string)val);
                            ret.Add(prop.Name, (string)val);
                        }
                        if (val.Type == JTokenType.Array) {
                            JArray jArray = (JArray)val;
                            double[] arrayValues = jArray.ToObject<List<double>>().ToArray();
                            ret.Add(prop.Name, arrayValues);
                        }
                    }
                }
                DebugLogger.Debug("BProtocolJSON.IPropsConverter: returning ret size {0}", ret.Count);
                return ret;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
                throw new NotImplementedException();
            }
        }
        */

        // static BLogger DebugLogger; // DEBUG DEBUG DEBUG Kludge so IPropsConverter can output debug messages
        private static void ProcessOnMsg(BTransport pTransport, byte[] pData, object pContext) {
            BProtocolJSON caller = pContext as BProtocolJSON;
            // DebugLogger = caller.Log; // DEBUG DEBUG DEBUG

            BMessage bmsg;

            // convert received buffer from JSON into a BMessage
            try {
                string stringData = System.Text.Encoding.UTF8.GetString(pData);
                bmsg = JsonConvert.DeserializeObject<BMessage>(stringData);
                caller.Log.Debug("BProtocolJSON.ProcessOnMsg: received bmsg={0}", bmsg.ToString());
            }
            catch (Exception ee) {
                throw new Exception(String.Format("Failure to parse incoming BMessage: {0}", ee.Message));
            }

            // Give the buffer to our caller
            if (bmsg != null) {
                caller._receptionCallback?.Invoke(bmsg, caller._receptionCallbackContext, caller);
            }
            else {
                caller.Log.Debug("BProtocolJSON.ProcessOnMsg: Received message but not parsed");
            }
        }
    }
}
