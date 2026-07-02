namespace ICS.Portal.Auth.Models;
using System;

public class AuthorizedUser 
{
    public AuthorizedUser(int id, int entityTypeId, int entityId, string UserName, long RoleFlag, string Token, DateTime TokenIssuedUtc, int TokenExpireMinutes, int TokenRefreshMinutes, DateTime PasswordExpiresUtc)
    {
        this.Id = id;
        this.EntityId = entityId;
        this.EntityTypeId = entityTypeId;
        this.UserName = UserName;
        this.RoleFlag = RoleFlag;
        this.Token = Token;
        this.TokenIssuedUtc = TokenIssuedUtc;
        this.TokenExpireMinutes = TokenExpireMinutes;
        this.TokenRefreshMinutes = TokenRefreshMinutes;
        this.PasswordExpiresUtc = PasswordExpiresUtc;
    }

    public string Key => "1";

    public int Id { get; private set; }

    public int EntityTypeId { set; get; }
    public int EntityId { set; get; }
    public string UserName { get; private set; }
    public long RoleFlag { get; private set; }
    public string Token { get; private set; }
    public DateTime TokenIssuedUtc { get; private set;}
    public DateTime PasswordExpiresUtc { private set; get; }

    public int TokenExpireMinutes { get; private set; }
    public int TokenRefreshMinutes { get; private set; }
    public bool IsTokenExpired()
    {
        bool result = DateTime.UtcNow > TokenIssuedUtc.AddMinutes(TokenExpireMinutes);
        return result;
    }
    public bool PasswordIsExpired()
    {
        DateTime now = DateTime.UtcNow;
        bool result = PasswordExpiresUtc < now;
        return result;
    }

    public bool IsTokenNearExpiration()
    {
        bool result = DateTime.UtcNow > TokenIssuedUtc.AddMinutes(TokenExpireMinutes).AddDays(-1);
        return result;
    }
}

public class AuthorizedContractorLoginOutput
{
    public AuthorizedUser? User { set; get; }
    public int WorkOrderDispatchTechId { set; get; }
    public int WorkOrderDispatchId { set; get; }
}