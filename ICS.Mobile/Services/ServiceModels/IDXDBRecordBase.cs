using ICS.Mobile.Services.ServiceModels;

using System.Runtime.CompilerServices;

namespace ICS.Mobile.Services.ServiceModels
{
    public abstract class IDXDBRecordBase
    {
        public string Key { get; set; } = "1";

        public DateTime? ExpireDateUTC { set; get; }

        public void SetExpiredDateUTC(int seconds)
        {
            if (seconds != 0)
            {
                ExpireDateUTC = DateTime.UtcNow.AddSeconds(seconds);
            }
            else
            {
                ExpireDateUTC = null;
            }
        }

        public abstract bool IsSuccess();

        public bool IsExpired()
        {
            if (ExpireDateUTC.HasValue)
            {
                return DateTime.UtcNow > ExpireDateUTC;
            }
            return false;
        }
    }
}

namespace ICS.Portal.Data.Commands.Models
{
    /* Note that all records to be synced must provide these properties*/

    public partial class WorkflowStepResultValueSaveInput
    {
        public string Key => $"{WorkflowStepResultId}.{LoopIndex}";
        public long Size => 0;
    }

    public partial class WorkflowResultCompleteInput
    {
        public string Key => $"{WorkflowResultId}";

        public long Size => 0;
    }

    public partial class ServiceErrorInsertInput
    {
        public string Key => $"{UId}";

        public long Size => 0;
    }

    public partial class AuthorizedUserSubscriptionSaveInput
    {
        public string Key => $"{Id}";

        public long Size => 0;
    }

    public partial class LogInsertInput
    {
        public string Key => $"{UId}";
        public Guid UId { set; get; }

        public long Size => 0;
    }

   
}

namespace ICS.Portal.Data.Queries.Models
{
    public partial class WorkflowResultListOutput : IDXDBRecordBase
    {
        //private string _Key = string.Empty;

        //public void SetKey(int dispatchTechId)
        //{
        //    _Key = dispatchTechId.ToString();
        //}
        //public override string Key => _Key;
        public override bool IsSuccess()
        {
            if (ReturnValue == Returns.NotFound || ResultData is null || ResultData.Count == 0)
            {
                return false;
            }
            return true;
        }
    }
    public partial class DispatchDetailOutput : IDXDBRecordBase
    {
        //private string _Key = string.Empty;

        //public void SetKey(int dispatchTechId)
        //{
        //    _Key = dispatchTechId.ToString();
        //}
        //public override string Key => _Key;
        public override bool IsSuccess()
        {
            if(ReturnValue == Returns.NotFound || ResultData is null || ResultData.DispatchTechsResult is null || ResultData.DispatchTechsResult.Count == 0)
            {
                return false;
            }
            return true;
        }
    }

    public partial class WorkflowListAvailableOutput : IDXDBRecordBase
    {
        public override bool IsSuccess()
        {
            return ReturnValue == Returns.Ok;
        }
    }


    public partial class WorkflowResultDetailOutput : IDXDBRecordBase
    {
        public override bool IsSuccess()
        {
            return ReturnValue == Returns.Ok;
        }
    }

    public partial class LookupsOutput : IDXDBRecordBase
    {
        public override bool IsSuccess()
        {
            return ReturnValue == Returns.Ok;
        }
    }

    public partial class CompanyDirectoryOutput : IDXDBRecordBase
    {
        public override bool IsSuccess()
        {
            return ReturnValue == Returns.Ok;
        }
    }

    public partial class TechKPIListOutput : IDXDBRecordBase
    {
        public override bool IsSuccess()
        {
            return ReturnValue == Returns.Ok;
        }
    }

    public partial class TechNewsListOutput : IDXDBRecordBase
    {
        public override bool IsSuccess()
        {
            return ReturnValue == TechNewsListOutput.Returns.Ok;
        }
    }

    public partial class VersionTrackerOutput : IDXDBRecordBase
    {
        public override bool IsSuccess()
        {
            return ReturnValue == Returns.Ok;
        }
    }

    public partial class DispatchListOutput : IDXDBRecordBase
    {
        public override bool IsSuccess()
        {
            return ReturnValue == Returns.Ok;
        }
    }

    public partial class ClientSecretsOutput : IDXDBRecordBase
    {
        public override bool IsSuccess()
        {
            return ReturnValue== Returns.Ok;
        }
    }

    public partial class BluonCacheByIdOutput : IDXDBRecordBase
    {
        public override bool IsSuccess()
        {
            return ReturnValue == Returns.Ok;
        }
    }
}

namespace ICS.Portal.Data.Images
{
    public partial class ImageInsertInput
    {
        public string Key => $"{GeneratedFileName}";

        public long Size => Data?.Length ?? 0;
    }
}