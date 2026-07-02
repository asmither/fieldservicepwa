namespace ICS.Mobile.Services.ServiceModels
{
    /// <summary>
    /// IDXDBDefinition defines the name, version, and stores for creation.
    /// </summary>
    public class IDXDBDefinition
    {
        /// <summary>
        /// Constructor for IDXDBDefinition
        /// </summary>
        /// <param name="version">Specifies the version - incrementing the version forces a DB clear and build.</param>
        /// <param name="name">Specifies the name of the database.</param>
        /// <param name="stores">Specifies the list of object stores to create on the upgraded needed transaction.</param>
        public IDXDBDefinition(int version, string name, List<IDXDBStore> stores)
        {
            Version = version;
            Name = name;
            Stores = stores;
        }

        public int Version { get; }
        public string Name { get; }
        public List<IDXDBStore> Stores { get; }
    }
}
