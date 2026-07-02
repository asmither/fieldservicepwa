using ICS.Mobile.Services.ServiceModels;

namespace ICS.Portal.Data.Custom
{
    public class PresentationMedia : IDXDBRecordBase
    {
        public PresentationMedia(string generatedFileName, string? title, string? caption, DateTime lastModified, string? base64)
        {
            GeneratedFileName = generatedFileName;
            Title = title;
            Caption = caption;
            LastModified = lastModified;
            Base64 = base64;
        }

        //public override string Key => GeneratedFileName;
        public string GeneratedFileName { get; }

        public string? Title { get; }

        public string? Caption { get; }
        public DateTime LastModified { get; }
        public string? Base64 { set; get; }

        public override bool IsSuccess()
        {
            return true;
        }
    }
}
