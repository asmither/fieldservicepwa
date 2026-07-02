namespace ICS.Mobile.Services.ServiceModels
{
    public class IDXDBSyncRecord
    {
        public IDXDBSyncRecord() { }

        /// <summary>
        /// Constructor for IDXDBStore
        /// </summary>
        /// <param name="id">Auto generated Id from index db.</param>
        /// <param name="recordId">Corresponding record in value table.</param>
        public IDXDBSyncRecord(int id, string recordId, int size)
        {
            Id = id;
            RecordId = recordId;
            Size = size;
        }

        public int Id { get; set; }
        public string RecordId { get; set; }
        public long Size { get; set; }

    }
}
