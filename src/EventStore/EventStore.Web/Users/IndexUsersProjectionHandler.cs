using System;
using EventStore.Common.Utils;
using EventStore.Core.Data;
using EventStore.Projections.Core.Messages;
using EventStore.Projections.Core.Services;
using EventStore.Projections.Core.Services.Processing;
using EventStore.Projections.Core.Utils;
using ResolvedEvent = EventStore.Projections.Core.Services.Processing.ResolvedEvent;

namespace EventStore.Web.Users
{
    public sealed class IndexUsersProjectionHandler : IProjectionStateHandler
    {
        private const string UserStreamPrefix = "$user-";
        private const string UsersStream = "$users";
        private const string UserEventType = "$User";

        public IndexUsersProjectionHandler(string query, Action<string> logger)
        {
        }

        public void Dispose()
        {
        }

        public void ConfigureSourceProcessingStrategy(SourceDefinitionBuilder builder)
        {
            builder.FromAll();
            builder.IncludeEvent("$UserCreated");
        }

        public void Load(byte[] state)
        {
        }

        public void LoadShared(byte[] state)
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
        }

        public void InitializeShared()
        {
        }

        public string GetStatePartition(CheckpointTag eventPosition, string category, ResolvedEvent data)
        {
            throw new NotImplementedException();
        }

        public string TransformCatalogEvent(CheckpointTag eventPosition, ResolvedEvent data)
        {
            throw new NotImplementedException();
        }

        public bool ProcessEvent(string partition, CheckpointTag eventPosition, string category, ResolvedEvent data, out byte[] newState, out byte[] newSharedState, out EmittedEventEnvelope[] emittedEvents)
        {
            newSharedState = null;
            if (!data.EventStreamId.StartsWith(UserStreamPrefix))
                throw new InvalidOperationException(
                    string.Format(
                        "Invalid stream name: '{0}' The IndexUsersProjectionHandler cannot handle events from other streams than named after the '$user-' pattern",
                        data.EventStreamId));

            var loginName = data.EventStreamId.Substring(UserStreamPrefix.Length);

            var userData = data.Data.ParseJson<UserData>();
            if (userData.LoginName != loginName)
                throw new InvalidOperationException(
                    string.Format(
                        "Invalid $UserCreated event found.  '{0}' login name expected, but '{1}' found", loginName,
                        userData.LoginName));

            emittedEvents = new[]
            {
                new EmittedEventEnvelope(
                    new EmittedDataEvent(
                        UsersStream,
                        Guid.NewGuid(),
                        UserEventType,
                        false,
                        loginName.ToUtf8(),
                        null,
                        eventPosition,
                        null))
            };

            newState = null;
            return true;
        }

        public bool ProcessPartitionCreated(string partition, CheckpointTag createPosition, ResolvedEvent data, out EmittedEventEnvelope[] emittedEvents)
        {
            emittedEvents = null;
            return false;
        }

        public bool ProcessPartitionDeleted(string partition, CheckpointTag deletePosition, out byte[] newState)
        {
            newState = null;
            return false;
        }

        public byte[] TransformStateToResult()
        {
            throw new NotImplementedException();
        }

        public IQuerySources GetSourceDefinition()
        {
            return SourceDefinitionBuilder.From(ConfigureSourceProcessingStrategy);
        }

    }
}
