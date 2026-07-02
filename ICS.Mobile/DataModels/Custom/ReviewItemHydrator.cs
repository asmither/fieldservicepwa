using ICS.Mobile.Services;
using ICS.Portal.Data.Images;

namespace ICS.Portal.Data.Custom
{
    public class ReviewItemHydrator
    {
        private readonly DataService dataManager;

        public ReviewItemHydrator(DataService dataManager)
        {
            this.dataManager = dataManager;
        }

        public async Task<bool> HydrateEquipmentAsync(WorkflowDataTypeDetail workflowEquipment)
        {
            throw new NotImplementedException();
        }

        public bool HydrateImage(ImageInsertInput imageInsertInput)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> HydrateImageAsync(ImageInsertInput imageInsertInput)
        {
            try
            {
                await dataManager.HydrateImageAsync(imageInsertInput);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }
}
