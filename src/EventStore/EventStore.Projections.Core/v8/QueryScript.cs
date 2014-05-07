using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using EventStore.Common.Utils;
using EventStore.Projections.Core.Messages;
using EventStore.Projections.Core.Utils;

namespace EventStore.Projections.Core.v8
{
    public class QueryScript : IDisposable
    {
        private readonly PreludeScript _prelude;
        private readonly CompiledScript _script;
        private readonly Dictionary<string, IntPtr> _registeredHandlers = new Dictionary<string, IntPtr>();

        private Func<byte[], byte[], byte[], byte[], string[], string> _getStatePartition;
        private Func<byte[], byte[], byte[], byte[], string[], string> _transformCatalogEvent;
        private Func<byte[], byte[], byte[], byte[], string[], Tuple<byte[], byte[]>> _processEvent;
        private Func<byte[], byte[], byte[], byte[], string[], byte[]> _processDeletedNotification;
        private Func<byte[], byte[], byte[], byte[], string[], byte[]> _processCreatedNotification;
        private Func<byte[]> _transformStateToResult;
        private Action<byte[]> _setState;
        private Action<byte[]> _setSharedState;
        private Action _initialize;
        private Action _initialize_shared;
        private Func<string> _getSources;

        // the following two delegates must be kept alive while used by unmanaged code
        private readonly Js1.CommandHandlerRegisteredDelegate _commandHandlerRegisteredCallback; // do not inline
        private readonly Js1.ReverseCommandHandlerDelegate _reverseCommandHandlerDelegate; // do not inline
        private QuerySourcesDefinition _sources;
        private Exception _reverseCommandHandlerException;

        public event Action<string> Emit;

        public QueryScript(PreludeScript prelude, string script, string fileName)
        {
            _prelude = prelude;
            _commandHandlerRegisteredCallback = CommandHandlerRegisteredCallback;
            _reverseCommandHandlerDelegate = ReverseCommandHandler;

            _script = CompileScript(prelude, script, fileName);

            try
            {
                GetSources();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private CompiledScript CompileScript(PreludeScript prelude, string script, string fileName)
        {
            prelude.ScheduleTerminateExecution();
            IntPtr query = Js1.CompileQuery(
                prelude.GetHandle(),
                script,
                fileName,
                _commandHandlerRegisteredCallback,
                _reverseCommandHandlerDelegate);
            var terminated = prelude.CancelTerminateExecution();
            CompiledScript.CheckResult(query, terminated, disposeScriptOnException: true);
            return new CompiledScript(query);
        }

        private void ReverseCommandHandler(string commandName, string commandBody)
        {
            try
            {
                switch (commandName)
                {
                    case "emit":
                        //TODO: byte[]
                        DoEmit(commandBody);
                        break;
                    default:
                        Console.WriteLine("Ignoring unknown reverse command: '{0}'", commandName);
                        break;
                }
            }
            catch (Exception ex)
            {
                // report only the first exception occured in reverse command handler
                if (_reverseCommandHandlerException == null)
                    _reverseCommandHandlerException = ex;
            }
        }

        private void CommandHandlerRegisteredCallback(string commandName, IntPtr handlerHandle)
        {
            _registeredHandlers.Add(commandName, handlerHandle);
            //TODO: change to dictionary
            switch (commandName)
            {
                case "initialize":
                    _initialize = () => ExecuteHandler(handlerHandle);
                    break;
                case "initialize_shared":
                    _initialize_shared = () => ExecuteHandler(handlerHandle);
                    break;
                case "get_state_partition":
                    _getStatePartition =
                        (data, metadata, positionMetadata, streamMetadata, other) =>
                            ExecuteHandler(handlerHandle, data, metadata, positionMetadata, streamMetadata, other)
                                .FromUtf8();
                    break;
                case "process_event":
                    byte[] newSharedState;
                    _processEvent =
                        (data, metadata, positionMetadata, streamMetadata, other) =>
                            Tuple.Create(
                                ExecuteHandler(
                                    handlerHandle,
                                    data,
                                    metadata,
                                    positionMetadata,
                                    streamMetadata,
                                    other,
                                    out newSharedState),
                                newSharedState);
                    break;
                case "process_deleted_notification":
                    _processDeletedNotification =
                        (data, metadata, positionMetadata, streamMetadata, other) =>
                            ExecuteHandler(handlerHandle, data, metadata, positionMetadata, streamMetadata, other);
                    break;
                case "process_created_notification":
                    _processCreatedNotification =
                        (data, metadata, positionMetadata, streamMetadata, other) =>
                            ExecuteHandler(handlerHandle, data, metadata, positionMetadata, streamMetadata, other);
                    break;
                case "transform_catalog_event":
                    _transformCatalogEvent =
                        (data, metadata, positionMetadata, streamMetadata, other) =>
                            ExecuteHandler(handlerHandle, data, metadata, positionMetadata, streamMetadata, other)
                                .FromUtf8();
                    break;
                case "transform_state_to_result":
                    _transformStateToResult = () => ExecuteHandler(handlerHandle);
                    break;
                case "test_array":
                    break;
                case "set_state":
                    _setState = data => ExecuteHandler(handlerHandle, data, null, null, null);
                    break;
                case "set_shared_state":
                    _setSharedState = data => ExecuteHandler(handlerHandle, data, null, null, null);
                    break;
                case "get_sources":
                    _getSources = () => ExecuteHandler(handlerHandle).FromUtf8();
                    break;
                case "set_debugging":
                case "debugging_get_state":
                    // ignore - browser based debugging only
                    break;
                default:
                    Console.WriteLine(
                        string.Format("Unknown command handler registered. Command name: {0}", commandName));
                    break;
            }
        }

        private void DoEmit(string commandBody)
        {
            OnEmit(commandBody);
        }

        private void GetSources()
        {
            if (_getSources == null)
                throw new InvalidOperationException("'get_sources' command handler has not been registered");
            var sourcesJson = _getSources();


            _sources = sourcesJson.ParseJson<QuerySourcesDefinition>();
        }

        private byte[] ExecuteHandler(
            IntPtr commandHandlerHandle,
            byte[] data,
            byte[] metadata,
            byte[] positionMetadata,
            byte[] streamMetadata,
            string[] other = null)
        {
            byte[] newSharedState;
            return ExecuteHandler(
                commandHandlerHandle,
                data,
                metadata,
                positionMetadata,
                streamMetadata,
                other,
                out newSharedState);
        }

        private byte[] ExecuteHandler(IntPtr commandHandlerHandle)
        {
            byte[] newSharedState;
            return ExecuteHandler(commandHandlerHandle, null, null, null, null, null, out newSharedState);
        }

        private unsafe byte[] ExecuteHandler(
            IntPtr commandHandlerHandle,
            byte[] data,
            byte[] metadata,
            byte[] positionMetadata,
            byte[] streamMetadata,
            string[] other,
            out byte[] newSharedState)
        {
            _reverseCommandHandlerException = null;

            _prelude.ScheduleTerminateExecution();

            IntPtr resultJsonPtr;
            IntPtr result2JsonPtr;
            IntPtr memoryHandle;
            bool success;

            var dataLength = data != null ? data.Length : 0;
            var metadataLength = metadata != null ? metadata.Length : 0;
            var positionMetadataLength = positionMetadata != null ? positionMetadata.Length : 0;
            var streamMetadataLength = streamMetadata != null ? streamMetadata.Length : 0;
            int resultJsonLength;
            int result2JsonLength;

            fixed (byte* data_ptr = data)
            fixed (byte* metadata_ptr = metadata)
            fixed (byte* position_metadata_ptr = positionMetadata)
            fixed (byte* stream_mertadata_ptr = streamMetadata)
            {
                success = Js1.ExecuteCommandHandler(
                    _script.GetHandle(),
                    commandHandlerHandle,
                    data_ptr,
                    dataLength,
                    metadata_ptr,
                    metadataLength,
                    metadata_ptr != position_metadata_ptr,
                    position_metadata_ptr,
                    positionMetadataLength,
                    stream_mertadata_ptr,
                    streamMetadataLength,
                    other,
                    other != null ? other.Length : 0,
                    out resultJsonPtr,
                    out resultJsonLength,
                    out result2JsonPtr,
                    out result2JsonLength,
                    out memoryHandle);

            }

            var resultJson = resultJsonLength > 0 ? new byte[resultJsonLength] : null;
            var result2Json = result2JsonLength > 0 ? new byte[result2JsonLength] : null;

            if (resultJsonLength > 0)
                Marshal.Copy(resultJsonPtr, resultJson, 0, resultJsonLength);

            if (result2JsonLength > 0)
                Marshal.Copy(result2JsonPtr, result2Json, 0, result2JsonLength);

            var terminated = _prelude.CancelTerminateExecution();
            if (!success)
                CompiledScript.CheckResult(_script.GetHandle(), terminated, disposeScriptOnException: false);
            Js1.FreeResult(memoryHandle);
            if (_reverseCommandHandlerException != null)
            {
                throw new ApplicationException(
                    "An exception occurred while executing a reverse command handler. "
                    + _reverseCommandHandlerException.Message,
                    _reverseCommandHandlerException);
            }
            newSharedState = result2Json;
            return resultJson;
        }

        private void OnEmit(string obj)
        {
            Action<string> handler = Emit;
            if (handler != null) handler(obj);
        }

        public void Dispose()
        {
            _script.Dispose();
        }

        public void Initialize()
        {
            InitializeScript();
        }

        public void InitializeShared()
        {
            InitializeScriptShared();
        }

        private void InitializeScript()
        {
            if (_initialize != null)
                _initialize();
        }

        private void InitializeScriptShared()
        {
            if (_initialize_shared != null)
                _initialize_shared();
        }

        public string GetPartition(
            byte[] data,
            byte[] metadata,
            byte[] positionMetadata,
            byte[] streamMetadata,
            string[] other)
        {
            if (_getStatePartition == null)
                throw new InvalidOperationException("'get_state_partition' command handler has not been registered");

            return _getStatePartition(data, metadata, positionMetadata, streamMetadata, other);
        }

        public string TransformCatalogEvent(
            byte[] data,
            byte[] metadata,
            byte[] positionMetadata,
            byte[] streamMetadata,
            string[] other)
        {
            if (_transformCatalogEvent == null)
                throw new InvalidOperationException("'transform_catalog_event' command handler has not been registered");

            return _transformCatalogEvent(data, metadata, positionMetadata, streamMetadata, other);
        }

        public Tuple<byte[], byte[]> Push(
            byte[] data,
            byte[] metadata,
            byte[] positionMetadata,
            byte[] streamMetadata,
            string[] other)
        {
            if (_processEvent == null)
                throw new InvalidOperationException("'process_event' command handler has not been registered");

            return _processEvent(data, metadata, positionMetadata, streamMetadata, other);
        }

        public byte[] NotifyDeleted(
            byte[] data,
            byte[] metadata,
            byte[] positionMetadata,
            byte[] streamMetadata,
            string[] other)
        {
            if (_processDeletedNotification == null)
                throw new InvalidOperationException(
                    "'process_deleted_notification' command handler has not been registered");

            return _processDeletedNotification(data, metadata, positionMetadata, streamMetadata, other);
        }

        public byte[] NotifyCreated(
            byte[] data,
            byte[] metadata,
            byte[] positionMetadata,
            byte[] streamMetadata,
            string[] other)
        {
            if (_processCreatedNotification == null)
                throw new InvalidOperationException(
                    "'process_created_notification' command handler has not been registered");

            return _processCreatedNotification(data, metadata, positionMetadata, streamMetadata, other);
        }

        public byte[] TransformStateToResult()
        {
            if (_transformStateToResult == null)
                throw new InvalidOperationException(
                    "'transform_state_to_result' command handler has not been registered");

            return _transformStateToResult();
        }

        public void SetState(byte[] state)
        {
            if (_setState == null)
                throw new InvalidOperationException("'set_state' command handler has not been registered");
            _setState(state);
        }

        public void SetSharedState(byte[] state)
        {
            if (_setSharedState == null)
                throw new InvalidOperationException("'set_shared_state' command handler has not been registered");
            _setSharedState(state);
        }

        public QuerySourcesDefinition GetSourcesDefintion()
        {
            return _sources;
        }

    }
}
