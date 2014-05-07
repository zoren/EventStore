using System;
using EventStore.Core.Data;
using EventStore.Projections.Core.Messages;
using EventStore.Projections.Core.Services.Processing;
using EventStore.Projections.Core.Utils;
using ResolvedEvent = EventStore.Projections.Core.Services.Processing.ResolvedEvent;

namespace EventStore.Projections.Core.Services
{
    public interface ISourceDefinitionSource
    {
        IQuerySources GetSourceDefinition();
    }


    public interface IProjectionStateHandler : IDisposable, ISourceDefinitionSource
    {
        void Load(byte[] state);
        void LoadShared(byte[] state);
        void Initialize();
        void InitializeShared();

        /// <summary>
        /// Get state partition from the event
        /// </summary>
        /// <returns>partition name</returns>
        string GetStatePartition(CheckpointTag eventPosition, string category, ResolvedEvent data);

        /// <summary>
        /// transforms a catalog event to streamId
        /// </summary>
        /// <param name="eventPosition"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        string TransformCatalogEvent(CheckpointTag eventPosition, ResolvedEvent data);

        /// <summary>
        /// Processes event and updates internal state if necessary.  
        /// </summary>
        /// <returns>true - if event was processed (new state must be returned) </returns>
        bool ProcessEvent(string partition, CheckpointTag eventPosition, string category, ResolvedEvent data, out byte[] newState, out byte[] newSharedState, out EmittedEventEnvelope[] emittedEvents);

        /// <summary>
        /// Processes partition created notificatiion and updates internal state if necessary.  
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="createPosition"></param>
        /// <param name="data"></param>
        /// <param name="emittedEvents"></param>
        /// <returns>true - if notification was processed (new state must be returned)</returns>
        bool ProcessPartitionCreated(
            string partition, CheckpointTag createPosition, ResolvedEvent data, out EmittedEventEnvelope[] emittedEvents);

        /// <summary>
        /// Processes partition deleted notification and updates internal state if necessary.  
        /// </summary>
        /// <returns>true - if event was processed (new state must be returned) </returns>
        bool ProcessPartitionDeleted(string partition, CheckpointTag deletePosition, out byte[] newState);

        /// <summary>
        /// Transforms current state into a projection result.  Should not call any emit/linkTo etc 
        /// </summary>
        /// <returns>result JSON or NULL if current state has been skipped</returns>
        byte[] TransformStateToResult();
    }

    public interface IProjectionCheckpointHandler
    {
        void ProcessNewCheckpoint(CheckpointTag checkpointPosition, out EmittedEventEnvelope[] emittedEvents);
    }

    public static class ProjectionStateHandlerTestExtensions
    {
        public static bool ProcessEvent(
            this IProjectionStateHandler self, string partition, CheckpointTag eventPosition, string streamId,
            string eventType, string category, Guid eventId, int eventSequenceNumber, string metadata, string data,
            out string state, out EmittedEventEnvelope[] emittedEvents, bool isJson = true)
        {
            byte[] ignoredSharedStateBytes;
            byte[] stateBytes;
            var result = self.ProcessEvent(
                partition,
                eventPosition,
                category,
                new ResolvedEvent(
                    streamId,
                    eventSequenceNumber,
                    streamId,
                    eventSequenceNumber,
                    false,
                    new TFPos(0, -1),
                    eventId,
                    eventType,
                    isJson,
                    data,
                    metadata),
                out stateBytes,
                out ignoredSharedStateBytes,
                out emittedEvents);
            state = stateBytes.FromUtf8();
            return result;
        }

        public static bool ProcessEvent(
            this IProjectionStateHandler self, string partition, CheckpointTag eventPosition, string streamId,
            string eventType, string category, Guid eventId, int eventSequenceNumber, string metadata, string data,
            out string state, out string sharedState, out EmittedEventEnvelope[] emittedEvents, bool isJson = true)
        {
            byte[] stateBytes;
            byte[] sharedStateBytes;
            var result = self.ProcessEvent(
                partition,
                eventPosition,
                category,
                new ResolvedEvent(
                    streamId,
                    eventSequenceNumber,
                    streamId,
                    eventSequenceNumber,
                    false,
                    new TFPos(0, -1),
                    eventId,
                    eventType,
                    isJson,
                    data,
                    metadata),
                out stateBytes,
                out sharedStateBytes,
                out emittedEvents);

            state = stateBytes.FromUtf8();
            sharedState = sharedStateBytes.FromUtf8();

            return result;
        }

        public static string GetNativeHandlerName(this Type handlerType)
        {
            return "native:" + handlerType.Namespace + "." + handlerType.Name;
        }

    }

}
