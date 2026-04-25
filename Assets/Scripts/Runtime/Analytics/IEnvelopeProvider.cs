namespace Kindrith.Analytics
{
    public interface IEnvelopeProvider
    {
        string PlayerId { get; }
        string SessionId { get; }
        string AppVersion { get; }
        string Platform { get; }
        string BuildType { get; }
        string Locale { get; }
    }
}
