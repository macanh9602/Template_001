namespace CH013.Commons
{
    /// <summary>Implemented by runtime elements that own subscriptions or transient visuals.</summary>
    public interface IPendingCleanup
    {
        void CleanupForLevelUnload();
    }
}
