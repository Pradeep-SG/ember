namespace Kindrith.Analytics
{
    public interface IAnalyticsSink
    {
        void Emit(AnalyticsEvent ev);
    }
}
