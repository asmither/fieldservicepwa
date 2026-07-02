using ICS.Mobile.Helpers;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;


namespace ICS.Portal.Data.Custom;



/// <summary>
/// Contains the fields that were extracted by the <see cref="AddressParser"/> object.
/// </summary>

public class AddressParseResult : IEquatable<AddressParseResult>
{

    /// <summary>
    /// JSON Constructor to handle deserialization
    /// </summary>
    AddressParseResult(string city, string number, string predirectional, string postdirectional, string state, string street, string streetLine, string suffix, string secondaryUnit, string secondaryNumber, string zip, int hash)
    {

        City = city;
        Number = number;
        Predirectional = predirectional;
        Postdirectional = postdirectional;
        State = state;
        Street = street;
        StreetLine = streetLine;
        Suffix = suffix;
        SecondaryUnit = secondaryUnit;
        SecondaryNumber = secondaryNumber;
        Zip = zip;
        if (hash == 0) hash = GetHashCode();
        AddressHash = hash;
        

    }
    [JsonConstructor]
    AddressParseResult()
    {
        AddressHash = 0;
    }

    /// <summary>
    /// Dapper junk used to read/write cache table
    /// </summary>
    [JsonIgnore]
    private int hash;
    /// <summary>
    /// The fill street line.
    /// </summary>
    [JsonIgnore]
    private string streetLine;
    /// <summary>
    /// The First street line.
    /// </summary>
    [JsonIgnore]
    private string streetLine1;
    /// <summary>
    /// The Second street line.
    /// </summary>
    [JsonIgnore]
    private string streetLine2;

    /// <summary>
    /// The c/s/z line.
    /// </summary>
    [JsonIgnore]
    private string cityStZip;
    /// <summary>
    /// Initializes a new instance of the <see cref="AddressParseResult"/> class.
    /// </summary>
    /// <param name="fields">The fields that were parsed.</param>
    internal AddressParseResult(Dictionary<string, string> fields)
    {
        if (fields == null)
        {
            throw new ArgumentNullException("fields");
        }

        var type = this.GetType();
        foreach (var pair in fields)
        {
            var bindingFlags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.IgnoreCase;
            var propertyInfo = type.GetProperty(pair.Key, bindingFlags);
            if (propertyInfo != null)
            {
                var methodInfo = propertyInfo.GetSetMethod(true);
                if (methodInfo != null)
                {
                    methodInfo.Invoke(this, new[] { pair.Value });
                }
            }
        }
    }

    /// <summary>
    /// Gets the city name.
    /// </summary>
    public string City
    {
        get;
        private set;
    }

    /// <summary>
    /// Gets the house number.
    /// </summary>
    public string Number
    {
        get;
        private set;
    }

    /// <summary>
    /// Gets the predirectional, such as "N" in "500 N Main St".
    /// </summary>
    public string Predirectional
    {
        get;
        private set;
    }

    /// <summary>
    /// Gets the postdirectional, such as "NW" in "500 Main St NW".
    /// </summary>
    public string Postdirectional
    {
        get;
        private set;
    }

    /// <summary>
    /// Gets the state or territory.
    /// </summary>
    public string State
    {
        get;
        private set;
    }

    /// <summary>
    /// Gets the name of the street, such as "Main" in "500 N Main St".
    /// </summary>
    public string Street
    {
        get;
        private set;
    }

    /// <summary>
    /// Gets the full street line, such as "500 N Main St" in "500 N Main St".
    /// This is typically constructed by combining other elements in the parsed result.
    /// However, in some special circumstances, most notably APO/FPO/DPO addresses, the
    /// street line is set directly and the other elements will be null.
    /// </summary>
    public string StreetLine
    {
        get
        {
            if (this.streetLine == null)
            {
                var streetLine = string.Join(
                    " ",
                    new[] {
                        this.Number,
                        this.Predirectional,
                        this.Street,
                        this.Suffix,
                        this.Postdirectional,
                        this.SecondaryUnit,
                        this.SecondaryNumber
                });
                streetLine = Regex
                    .Replace(streetLine, @"\ +", " ")
                    .Trim();
                return streetLine;
            }

            return this.streetLine;
        }

        private set
        {
            this.streetLine = value;
        }
    }

    /// <summary>
    /// Returns the typical "Address1" street line, without SECONDARY parts
    /// </summary>
    public string StreetLine1
    {
        get
        {
            if (this.streetLine1 == null)
            {
                var streetLine1 = string.Join(
                    " ",
                    new[] {
                        this.Number,
                        this.Predirectional,
                        this.Street,
                        this.Suffix,
                        this.Postdirectional
                });
                streetLine1 = Regex
                    .Replace(streetLine1, @"\ +", " ")
                    .Trim();
                return streetLine1;
            }

            return this.streetLine1;
        }

        private set
        {
            this.streetLine1 = value;
        }
    }

    /// <summary>
    /// Returns only the typical "Address2" start parts - SECONDARY 
    /// </summary>
    public string StreetLine2
    {
        get
        {
            if (this.streetLine2 == null)
            {
                var streetLine2 = string.Join(
                    " ",
                    new[] {
                        this.SecondaryUnit,
                        this.SecondaryNumber
                });
                streetLine2 = Regex
                    .Replace(streetLine2, @"\ +", " ")
                    .Trim();
                return streetLine2;
            }

            return this.streetLine2;
        }

        private set
        {
            this.streetLine2 = value;
        }
    }

    /// <summary>
    /// Gets the street suffix, such as "ST" in "500 N MAIN ST".
    /// </summary>
    public string Suffix
    {
        get;
        private set;
    }

    /// <summary>
    /// Gets the secondary unit, such as "APT" in "500 N MAIN ST APT 3".
    /// </summary>
    public string SecondaryUnit
    {
        get;
        private set;
    }

    /// <summary>
    /// Gets the secondary unit, such as "3" in "500 N MAIN ST APT 3".
    /// </summary>
    public string SecondaryNumber
    {
        get;
        private set;
    }

    
    /// <summary>
    /// Gets the ZIP code.
    /// </summary>
    public string Zip
    {
        get;
        private set;
    }

    /// <summary>
    /// Pure 5 digit zip code
    /// </summary>
    public string PostalCodeFive
    {
        get
        {
            if (string.IsNullOrEmpty(this.Zip)) return string.Empty;
            return this.Zip.Left(5);
        }
        set { }

    }

    /// <summary>
    /// Pure 4 digit portion of the zip code
    /// </summary>
    public string PostalCodeFour
    {
        get
        {
            if (string.IsNullOrEmpty(this.Zip)) return string.Empty;
            if (this.Zip.Length > 5) return this.Zip.Right(4); else return String.Empty;
        }
        set
        {
            //
        }
    }

    /// <summary>
    /// Pure 4 digit portion of the zip code
    /// </summary>
    public string PostalCode
    {
        get
        {

            if (string.IsNullOrEmpty(this.Zip)) return string.Empty;

            if (this.Zip.Length > 5)
            {
                return ($"{this.Zip.Left(5)}-{this.Zip.Right(4)}");
            }
            else return (this.Zip);
        }
        set
        {
            //
        }

    }

    /// <summary>
    /// Gets the City, State, Zip code on one line
    /// </summary>
    public string CityStateZip
    {
        get
        {
            if (this.cityStZip == null)
            {
                var cityStZip = string.Join(
                    " ",
                    new[] {
                                this.City,
                                this.State,
                                this.Zip,
                });
                cityStZip = Regex
                    .Replace(cityStZip, @"\ +", " ")
                    .Trim();
                return cityStZip;
            }

            return this.cityStZip;
        }

        private set
        {
            this.cityStZip = value;
        }
    }


    /// <summary>
    /// Finished print-ready formatted address
    /// </summary>
    public string FormattedAddress
    {
        get
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}, {1}, {2} {3}",
                this.StreetLine.TitleCase(),
                this.City,
                this.State,
                this.Zip);
        }

    }
    
    public string FormattedAddressWithSemiDelim
    {
        get
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}; {1}, {2} {3}",
                this.StreetLine.TitleCase(),
                this.City,
                this.State,
                this.Zip);
        }

    }

    /// <summary>
    /// Returns a <see cref="System.String"/> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="System.String"/> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0}; {1}, {2} {3}",
            this.StreetLine,
            this.City,
            this.State,
            this.Zip);
    }


    //    End of Basic Parser Results, added data items below
    // --------------------------------------------------------

    /// <summary>
    /// Funtion to send in a ZIP CODE override value to this object and set it properly
    /// </summary>
    public string UpdateZipCode(string szip = "")
    {
        if (!string.IsNullOrEmpty(szip))
        {
            this.Zip = szip;
        } 
        return this.Zip;
    }

    /// <summary>
    /// Funtion to send in a CITY override value to this object and set it properly
    /// </summary>
    public string UpdateCity(string scity = "")
    {
        if (!string.IsNullOrEmpty(scity))
        {
            this.City = scity;
        }
        return this.City;
    }

    
    private int Id { get; set; }

    /// <summary>
    /// Hash Code of address
    /// </summary>
    public int AddressHash
    {
        get
        {
            if (hash == 0) hash = GetHashCode();
            return hash;
        }
        set
        {
            hash = value;
        }
    }


    // Following EXTENDED ZIP data REQUIRES EXTERNAL DB CONNECTION and function to populate them OUTSIDE of basic AddressParser.ParseAddress() function

    /// <summary>
    /// Alternative Alias for this city name.  Likely the larger metro city name
    /// </summary>
    public string CityAlias { set; get; }
    /// <summary>
    /// STATE(2)+COUNTY(3) FIPS code of the county for this address
    /// </summary>
    public string CountyFIPS { set; get; }
    /// <summary>
    /// The local municipality governing this address
    /// </summary>
    public string Municipality { set; get; }
    /// <summary>
    /// County Name of this zip code
    /// </summary>
    public string County { set; get; }
    /// <summary>
    /// Country Code - US
    /// </summary>
    public string Country { set; get; }
    /// <summary>
    /// Longitude of this address
    /// </summary>
    public double Longitude { set; get; }
    /// <summary>
    /// Latitude of this address
    /// </summary>
    public double Latitude { set; get; }
    /// <summary>
    /// Hours offset from GMT (you need to *-1)
    /// </summary>
    public string TimeZoneOffset { set; get; }
    /// <summary>
    /// Does this zip/area/addrress recognize DST
    /// </summary>
    public string DayLightSaving { set; get; }

    // optional EXTERNAL json payloads for saving

    /// <summary>
    /// This object, as JSON
    /// </summary>
    [JsonIgnore]
    public string AddressResultsJSON { set; get; }

    /// <summary>
    /// The ZIP(5) area information in JSON
    /// </summary>
    [JsonIgnore]
    public string AddressAreaDataJSON { set; get; }
    /// <summary>
    /// Azure Maps JSON for all info, cross-streets, geocode, and potential search matches
    /// </summary>
    [JsonIgnore]
    public string AzureMapsJSON { set; get; }
    /// <summary>
    /// BING/ALAN address format
    /// </summary>
    [JsonIgnore]
    public string InternationalAddressJSON { set; get; }


    // Class/Object Functions for ICOMPARE
    public override bool Equals(Object other)
    {
        if (other == null) return false;
        AddressParseResult objDisp = other as AddressParseResult;
        if (objDisp == null) return false;
        else return Equals(objDisp);
    } 
    public override int GetHashCode()
    {
        // HASH CODE THIS IS THE HASHCODE FOR THE ADDRESS DIRTY - SQL COMPATIBLE
        return SQLHashFunctions.GetDeterministicHashCodeSQL(string.Format(
            CultureInfo.InvariantCulture,
            "{0}; {1}, {2} {3}",
            this.StreetLine.TitleCase(),
            this.City,
            this.State,
            this.Zip)
        );
    }
    public bool Equals(AddressParseResult other)
    {
        if (other == null) return false;
        if (this.GetHashCode().Equals(other.GetHashCode())) { return true; }
        if (this.FormattedAddress.Equals(other.FormattedAddress)) { return true; }
        return false;
    }

}

/// <summary>
/// International Object for our ICS db  
/// Create after cleaning address as: InternationalAddress internationalAddress = new InternationalAddress(AddrResultsOutput);
/// </summary>
public class InternationalAddress
{
    [MaxLength(128)]
    public string AddressLine { get; set; } = (string)"";
    [MaxLength(64)]
    public string AdminDistrict { get; set; } = (string)"";
    [MaxLength(64)]
    public string AdminDistrict2 { get; set; } = (string)"";
    [MaxLength(64)]
    public string CountryRegion { get; set; } = (string)"US";
    [MaxLength(5)]
    public string PostalCode { get; set; } = (string)"";
    [MaxLength(4)]
    public string PostalCodeFour { get; set; } = null;
    [MaxLength(255)]
    public string FormattedAddress { get; set; } = (string)"";
    [MaxLength(255)]
    public string Landmark { get; set; } = null;
    [MaxLength(64)]
    public string Locality { get; set; } = null;
    [MaxLength(128)]
    public string Neighborhood { get; set; } = null;

    public Double Latitude { get; set; } = 0;
    public Double Longitude { get; set; } = 0;
    public int AddressHash { get; set; } = (int)0;
    public string GeocodeJSON { get; set; } = null;


    ~InternationalAddress()
    {
        // done
    }

    /// <summary>
    /// Default Constructor
    /// </summary>
    // public InternationalAddress() { }
    /// <summary>
    /// Create and seed Object with AddressParseResult 
    /// </summary>
    public InternationalAddress(AddressParseResult addr)
    {
        SetFromAddressParseResult(addr);
    }
    /// <summary>
    /// Pass in AddressParseResult to seed InternationalAddress
    /// </summary>
    public void SetFromAddressParseResult(AddressParseResult addr)
    {

        if (addr != null)
        {
            AddressLine = addr.StreetLine.TitleCase();
            AdminDistrict = addr.State;
            AdminDistrict2 = addr.County;
            CountryRegion = addr.Country;

            // zips
            PostalCode = addr.PostalCodeFive;
            PostalCodeFour = addr.PostalCodeFour;


            // full address
            FormattedAddress = addr.FormattedAddress;

            Locality = addr.CityAlias;
            Neighborhood = addr.Municipality;
            Latitude = addr.Latitude;
            Longitude = addr.Longitude;
            AddressHash = addr.GetHashCode();
        }

    }

}


