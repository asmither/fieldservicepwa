namespace ICS.Portal.Data.Queries.Models;

public partial class DispatchDetailResult
{

    public Equipment? GetEquipmentById(int Id, Guid? uuid = null)
    {
        if (EquipmentResult is not null)
        {
            return EquipmentResult.Find(e =>
                   (e.Id > 0 && Id > 0 && e.Id == Id) ||
                   (e.TemporaryId.HasValue && uuid.HasValue && e.TemporaryId == uuid));
        }
        return null;
    }

}