using System;

namespace ICS.Mobile.DataModels.Local;

public enum GeoStates
{
    None = 0,
    Initializing = 1,
    Error = 2,
    Running = 3
}

public class PositionResult
{
    public GeoStates GeoState { set; get; }
    public LatLong LatLong { set; get; } = new();
}

public class LatLong
{
    public Decimal Latitude { set; get; } 
    public Decimal Longitude { set; get; }

    public LatLong()
    {
        Latitude = 0;
        Longitude = 0;
    }

    public LatLong(double lat, double lon)
    {
        this.Latitude = (Decimal)lat;
        this.Longitude = (Decimal)lon;
    }
    public LatLong(decimal lat, decimal lon)
    {
        this.Latitude = lat;
        this.Longitude = lon;
    }
    
}
