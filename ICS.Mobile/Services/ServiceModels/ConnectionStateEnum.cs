namespace ICS.Mobile.Services.ServiceModels
{
    public enum ConnectionStates
    {
        /// <summary>
        /// Connection status cannot be detected. This does not indicate a connection is not present.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Browser Value: slow-2g
        /// Suitable for sending text only
        /// </summary>
        VerySlow = 1,

        /// <summary>
        /// Browser Value: 2g
        /// Suitable for sending text only
        /// </summary>
        Slow = 2,

        /// <summary>
        /// Browser Value: 3g
        /// Suitable for sending text and small images
        /// </summary>
        Good = 3,

        /// <summary>
        /// Browser Value: 4g
        /// Suitable for sending text only small images
        /// </summary>
        VeryGood = 4,

    }
}
