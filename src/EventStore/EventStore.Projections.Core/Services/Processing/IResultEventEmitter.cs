namespace EventStore.Projections.Core.Services.Processing
{
    public interface IResultEventEmitter
    {
        EmittedEventEnvelope[] ResultUpdated(string partition, byte[] result, CheckpointTag at);
    }
}
