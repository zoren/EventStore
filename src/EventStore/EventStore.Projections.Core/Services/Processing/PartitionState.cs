using System;
using System.IO;
using EventStore.Common.Utils;
using EventStore.Projections.Core.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EventStore.Projections.Core.Services.Processing
{
    public class PartitionState
    {
        public bool IsChanged(PartitionState newState)
        {
            return !ArrayEqual(_state, newState._state)
                   || !ArrayEqual(_result, newState._result);
        }

        private bool ArrayEqual(byte[] a, byte[] b)
        {

            if (a == b)
                return true;

            if (a == null ^ b == null)
                return false;

            if (a == null)
                return true;

            if (a.Length != b.Length)
                return false;

            for (var i = 0; i < a.Length; i++)
                if (a[i] != b[i])
                    return false;
            return true;
        }

        public static PartitionState Deserialize(byte[] serializedState, CheckpointTag causedBy)
        {
            if (serializedState == null)
                return new PartitionState(causedBy);

            JToken state = null;
            JToken result = null;

            if (serializedState != null)
            {
                var reader = new JsonTextReader(new StreamReader(new MemoryStream(serializedState), Helper.UTF8NoBom));

                if (!reader.Read())
                    Error(reader, "StartArray or StartObject expected");

                if (reader.TokenType == JsonToken.StartObject)
                    // old state format
                    state = JToken.ReadFrom(reader);
                else
                {
                    if (reader.TokenType != JsonToken.StartArray)
                        Error(reader, "StartArray expected");
                    if (!reader.Read() || (reader.TokenType != JsonToken.StartObject && reader.TokenType != JsonToken.EndArray))
                        Error(reader, "StartObject or EndArray expected");
                    if (reader.TokenType == JsonToken.StartObject)
                    {
                        state = JToken.ReadFrom(reader);
                        if (!reader.Read())
                            Error(reader, "StartObject or EndArray expected");
                        if (reader.TokenType == JsonToken.StartObject)
                        {
                            result = JToken.ReadFrom(reader);
                            if (!reader.Read())
                                Error(reader, "EndArray expected");
                        }
                        if (reader.TokenType != JsonToken.EndArray)
                            Error(reader, "EndArray expected");
                    }
                }
            }
            var stateJson = state != null ? state.ToCanonicalJson().ToUtf8() : null;
            var resultJson = result != null ? result.ToCanonicalJson().ToUtf8() : null;

            return new PartitionState(stateJson, resultJson, causedBy);
        }

        private static void Error(JsonTextReader reader, string message)
        {
            throw new Exception(string.Format("{0} (At: {1}, {2})", message, reader.LineNumber, reader.LinePosition));
        }

        private readonly byte[] _state;
        private readonly byte[] _result;
        private readonly CheckpointTag _causedBy;

        public PartitionState(CheckpointTag causedBy)
            : this((byte[])null, null, causedBy)
        {
        }

        public PartitionState(string state, string result, CheckpointTag causedBy)
            : this(state.ToUtf8(), result.ToUtf8(), causedBy)
        {
        }

        public PartitionState(byte[] state, byte[] result, CheckpointTag causedBy)
        {
            if (causedBy == null) throw new ArgumentNullException("causedBy");

            _state = state;
            _result = result;
            _causedBy = causedBy;
        }

        public CheckpointTag CausedBy
        {
            get { return _causedBy; }
        }

        public string GetStateString()
        {
            return _state.FromUtf8();
        }

        public string GetResultString()
        {
            return _result.FromUtf8();
        }

        public byte[] StateBytes
        {
            get { return _state; }
        }

        public byte[] ResultBytes
        {
            get { return _result; }
        }

        public byte[] Serialize()
        {
            var state = _state;
            if (state == null && _result != null)
                throw new Exception("state == \"\" && Result != null");
            return (_result != null
                       ? "[" + state.FromUtf8() + "," + _result.FromUtf8() + "]"
                       : "[" + state.FromUtf8() + "]").ToUtf8();
        }

        public bool SameResult(PartitionState newState)
        {
            return ArrayEqual(_result, newState._result);
        }

        public bool SameState(byte[] newState)
        {
            return ArrayEqual(_state, newState);
        }
    }
}
