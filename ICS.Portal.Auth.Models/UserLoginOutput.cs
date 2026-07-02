namespace ICS.Portal.Auth.Models;

public class UserLoginOutput
{
    public int AuthorizedUserId { set; get; }
    public List<Communication>? Communications { set; get; }
    public class Communication
    {
        public byte CommunicationTypeId { set; get; }
        public string? MaskedData { set; get; }
        public string? Token { set; get; }
    }
}
