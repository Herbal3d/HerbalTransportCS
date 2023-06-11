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
using System.Text.Json;
using System.Text.Json.Serialization;

using org.herbal3d.b.protocol;
using org.herbal3d.cs.CommonUtil;

namespace org.herbal3d.transport {
    /**
     * Receive a JSON transport body and pass a BMessage up the stack.
     */

    public class BProtocolJSON : BProtocol {

        public static string ID = "Basil-JSON";

        public BProtocolJSON(ParamBlock pParams,
                            BTransport pTransport,
                            BLogger pLogger) : base(pParams, pTransport, BProtocolJSON.ID, pLogger) {

            // set up to receive messages
            pTransport.SetReceiveCallback(BProtocolJSON.ProcessOnMsg, this);
        }

        public override void Send(BMessage pData) {
            // convert the BMessage to JSON buffer
            var options = new JsonSerializerOptions {
                WriteIndented = true,   // pretty print
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                IncludeFields = true    // normally fields are not serialized
            };
            string asJSON = JsonSerializer.Serialize(pData, options);
            if (logMsgSent) {
                Log.Debug("BProtocolJSON.Send: Sending {0}", asJSON);   // DEBUG DEBUG
            }
            Transport?.Send(Encoding.UTF8.GetBytes(asJSON));
        }

        // public override void Start() {
        // }

        /// <summary>
        /// The BMessage has an IProps field which is a keyed collection of values.
        /// Depending on the encoding of the message, the type of the values can be
        ///     coded differently. Currently, the values are either a string or an array
        ///     of doubles.
        /// This class adds a parser for that case where we check the syntax of the
        ///     value and return the correct type of value.
        /// Values are PropValues and can be string | number | string[] | number[]
        ///     where 'number' is a 'double'.
        /// </summary>
        private class IPropsConverter : JsonConverterFactory {
            public override bool CanConvert(Type typeToConvert) {
                return typeToConvert == typeof(Dictionary<string,object>);
            }

            public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
                return new IPropsConverterReader(typeToConvert, options);
            }
        }
        private class IPropsConverterReader : JsonConverter<Dictionary<string, object>> {
            public IPropsConverterReader(Type typeToConvert, JsonSerializerOptions options) : base() {
            }
            public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
                Dictionary<string, object> ret = new Dictionary<string, object>();
                if (reader.TokenType == JsonTokenType.StartObject) {
                    while (reader.Read()) {
                        if (reader.TokenType == JsonTokenType.EndObject) {
                            break;
                        }
                        if (reader.TokenType == JsonTokenType.PropertyName) {
                            string propName = reader.GetString();
                            reader.Read();
                            switch (reader.TokenType) {
                                case JsonTokenType.String:
                                    ret.Add(propName, reader.GetString());
                                    break;
                                case JsonTokenType.Number:
                                    ret.Add(propName, reader.GetDouble());
                                    break;
                                case JsonTokenType.StartArray:
                                    List<string> strList = new List<string>();
                                    List<double> dblList = new List<double>();
                                    while (reader.Read()) {
                                        if (reader.TokenType == JsonTokenType.EndArray) {
                                            break;
                                        }
                                        switch (reader.TokenType) {
                                            case JsonTokenType.String:
                                                strList.Add(reader.GetString());
                                                break;
                                            case JsonTokenType.Number:
                                                dblList.Add(reader.GetDouble());
                                                break;
                                        }
                                    }
                                    if (strList.Count > 0) {
                                        ret.Add(propName, strList.ToArray());
                                    }
                                    else if (dblList.Count > 0) {
                                        ret.Add(propName, dblList.ToArray());
                                    }
                                    break;
                            }
                        }
                    }

                }

                return ret;
            }

            public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options) {
                throw new NotImplementedException();
            }
        }

        static BLogger DebugLogger; // DEBUG DEBUG DEBUG Kludge so IPropsConverter can output debug messages

        private static void ProcessOnMsg(BTransport pTransport, byte[] pData, object pContext) {
            BProtocolJSON caller = pContext as BProtocolJSON;
            if (caller != null) {

                BMessage bmsg;

                try {
                    JsonConverter[] converters = new JsonConverter[] { new IPropsConverter() };
                    BProtocolJSON.DebugLogger = caller.Log; // Kludge so IPropsConverter can output debug messages
                    string stringData = System.Text.Encoding.UTF8.GetString(pData);
                    var options = new JsonSerializerOptions {
                        IncludeFields = true,   // normally fields are not serialized
                        Converters = { new IPropsConverter() }
                    };
                    bmsg = JsonSerializer.Deserialize<BMessage>(stringData, options);

                    if (caller.logMsgRcvd) {
                        caller.Log.Debug("BProtocolJSON.ProcessOnMsg: {0} received bmsg={1}",
                            caller.Transport.ConnectionName,
                            bmsg.ToString());
                    }
                }
                catch (Exception ee) {
                    caller.Log.Debug("BProtocolJSON.ProcessOnMsg: failure to parse incoming message");
                    throw new Exception(String.Format("Failure to parse incoming BMessage: {0}", ee.Message));
                }

                // Give the buffer to our caller
                if (bmsg != null) {
                    caller._receptionCallback?.Invoke(bmsg, caller._receptionCallbackContext, caller);
                }
                else {
                    caller.Log.Error("BProtocolJSON.ProcessOnMsg: Received message but not parsed");
                }
            }
            else {
                throw new Exception("BProtocolJSON.ProcessOnMsg: called with context not instance of BProtocolJSON");
            }
        }
    }
}
