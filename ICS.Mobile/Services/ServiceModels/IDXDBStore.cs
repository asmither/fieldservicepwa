namespace ICS.Mobile.Services.ServiceModels
{
    /// <summary>
    /// IDXDBStore defines the name, keypath, and sync priority for each object store.
    /// </summary>
    public class IDXDBStore
    {
        /// <summary>
        /// Constructor for IDXDBStore
        /// </summary>
        /// <param name="name">Specifies the name of the object store.</param>
        /// <param name="keyPath">Specifies the property that uniquely identifies the row</param>
        /// <param name="syncPriority">Specifies the sync priority in ascending order which determines the order for running sync processes. A value of zero indicates the record should not be synced.</param>
        /// <param name="purgeAfter">Specifies the time (in seconds) to wait before purging this record.</param>
        public IDXDBStore(string name, string keyPath, int syncPriority, int refreshAfter, int purgeAfter)
        {
            Name = name;
            KeyPath = keyPath;
            SyncPriority = syncPriority;
            RefreshAfter = refreshAfter;
            PurgeAfter = purgeAfter;
        }
        public string Name { get; }
        public string KeyPath { get; }
        public int SyncPriority { get; }
        public int RefreshAfter { get; }
        public int PurgeAfter { get; }

    }
}
