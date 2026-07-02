namespace ICS.Portal.Data.Custom
{
    public interface ITimestampAndPositionResolver
    {
        Task<TimestampAndPosition> GetTimestampAndPositionAsync();
    }
}