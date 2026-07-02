using ICS.Portal.Auth.Models;

namespace ICS.Mobile.Services.ServiceModels
{
    public class AppState : IDXDBRecordBase
    {
        public int? EmployeeId
        {
            get
            {
                return AuthorizedUser?.EntityId ?? null;
            }
        }
        public int? DispatchId { set; get; }
        public int? DispatchTechId { set; get; }
        public DateTime? LastCachePurge { set; get; }
        public int WorkflowCount { set; get; } 
        public int DispatchCount { set; get; }
        
        public string? UserName
        {
            get
            {
                return AuthorizedUser?.UserName ?? string.Empty;
            }
        }

        public DateTime LastPing { set; get; } = DateTime.UtcNow.AddMinutes(-60);

        public string? BluonProjects { set; get; } 
        public string? BluonCurrentProjectId { set; get; }
        public string? BluonCurrentView { set; get;}
        public string? BluonView { set; get; }
        public string? BluonRecentModels { set; get; }

        public DateTime? LastAppVersionWarning { set; get; }

        public AuthorizedUser? AuthorizedUser { set; get; }
        
        public string LoginRedirect { set; get; }

        public int RowUserId
        {
            get
            {
                return AuthorizedUser?.Id ?? 0;
            }
        }

        public bool IsContractor
        {
            get
            {
                return ((AuthorizedUser?.RoleFlag ?? 0) & 2048) == 2048;
            }
        }

        public int? LockedDispatchId { get; set; }

        public override bool IsSuccess()
        {
            return true;
        }
        //TODO: Add addition fields to persist as state;
    }
}
