using System.Data;

namespace ICS.Portal.Data.Custom;


public class AddressManager
{
    #region Data Objects for Addresses 

    // optional db conn for cache/db lookup
    public bool dbIsActive { get; private set; }         // see if DB Cache is active
    
    // USPS Homogenize Addresses
    public string AddrIn { set; get; }  // Optional INPUT String can be set for address functions instad of passed in functions
    public AddressParseResult AddrOutput { set; get; }  // Parsed, finished address object!

    private AddressParser AddressParser { set; get; }  // the real usps address clean engine used by manager object

    #endregion

    #region Address Parse Function and Azure Map Parse Function

    // Address Parse Functions
    public AddressParseResult ParseAddressLine(string sAddr = "")
    {
        // Basic clean Address to USPS Standard
        AddrOutput = null;
        if (string.IsNullOrEmpty(sAddr)) sAddr = AddrIn;
        if (!string.IsNullOrEmpty(sAddr)) AddrOutput = AddressParser.ParseAddress(sAddr);
        if (AddrOutput!=null) _ = AddrOutput.GetHashCode(); // compute current hashcode
        return (AddrOutput);

    } // Standardize an address to the USPS format
    
    public InternationalAddress GetInternationalAddress()
    {
        if (AddrOutput != null)
        {
            return new InternationalAddress(AddrOutput);
        }
        return null;
    }

    #endregion
   
    #region Support & junk functions 
    // Support Utilities
    public void DumpAddressDetails()
    {
        if (AddrOutput != null)
        {
            Console.WriteLine($"1: {AddrOutput.StreetLine}");
            Console.WriteLine($"1: {AddrOutput.CityStateZip}");
            Console.WriteLine("AddrOut : {0}", AddrOutput);
            {
                var properties = AddrOutput
                .GetType()
                .GetProperties()
                .OrderBy(x => x.Name);

                foreach (var property in properties)
                {
                    Console.WriteLine(
                       "{0,30} : {1}",
                       property.Name,
                       property.GetValue(AddrOutput, null)
                   );
                }
            }
        }
    }
    
    #endregion

}
