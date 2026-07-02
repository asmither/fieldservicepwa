using ICS.Mobile.DataModels.Custom;
using ICS.Portal.Data.Custom;
using ICS.Portal.Data.Images;

using System.Runtime.CompilerServices;
using System.Text.Json;

using static System.Net.WebRequestMethods;

namespace ICS.Portal.Data.Custom
{
    public class WorkflowReviewItem
    {

        public string BaseImagesURL { set; get; } = "https://interiorcsstorage.blob.core.windows.net/images/";

        public string? Prompt { set; get; }
        public string? SharePrompt { set; get; }

        public string? Value { set; get; }

        public string? DisplayValue { set; get; }
        public int DataType { set; get; }
        public string? NoteLabel { set; get; }
        public string? Note { set; get; }
        public string? ImageLabel { set; get; }
        public string? ImageFileName { set; get; }

        public ImageInsertInput? ImageData { set; get; }

        public string ImageDataSourceContentType
        {
            get
            {
                if (ImageData is not null)
                {
                    return ImageData.ContentType ?? string.Empty;
                }
                return string.Empty;

            }
        }
        public string ImageDataSource
        {
            get
            {
                if (ImageData is not null)
                {

                    string fileExt = Path.GetExtension(ImageData.GeneratedFileName);
                    if (fileExt.Contains(".jpg") || fileExt.Contains(".jpeg") || fileExt.Contains(".png") || fileExt.Contains(".gif") || fileExt.Contains(".bmp") || fileExt.Contains(".heic"))
                        return $"data:{ImageData.ContentType};base64,{Convert.ToBase64String(ImageData.Data!)}";
                    else
                        return $"{BaseImagesURL}{ImageFileName}";
                }

                return $"{BaseImagesURL}{ImageFileName}";
            }
        }
        public List<WorkflowEquipment>? ModifiedEquipmentList { set; get; }
        public WorkflowEquipment? SelectedEquipment { set; get; }
        public string[]? ImageNames { set; get; }
        public string[]? ImageLabels { set; get; }

        public ImageInsertInput[]? ImageDatas { set; get; }

        public string ImageDatasSourceContentType(int index)
        {
            if (ImageDatas is not null && ImageDatas.Length > index && ImageDatas[index] != null)
            {
                return ImageDatas[index].ContentType ?? string.Empty;
            }
            return string.Empty;
        }

        public string ImageDatasSource(int index)
        {

            if (ImageDatas is not null && ImageDatas.Length > index && ImageDatas[index] != null)
            {
                string fileExt = Path.GetExtension(ImageDatas[index].GeneratedFileName);
                if (fileExt.Contains(".jpg") || fileExt.Contains(".jpeg") || fileExt.Contains(".png") || fileExt.Contains(".gif") || fileExt.Contains(".bmp") || fileExt.Contains(".heic"))
                    return $"data:{ImageDatas[index].ContentType};base64,{Convert.ToBase64String(ImageDatas[index].Data!)}";
                else
                    return $"{BaseImagesURL}{ImageNames[index]}";
            }

            return $"{BaseImagesURL}{ImageNames[index]}";
        }

        public Dictionary<string, string> EquipmentImages;

        public int? WorkflowTagId { set; get; }
        public string? WorkflowTag { set; get; }
        public int? NotificationGroupId { set; get; }
        public string? NotificationGroup { set; get; }
        public DateTime? StartedDate { set; get; }
        public DateTime? CompletedDate { set; get; }
        public decimal? Latitude { set; get; }
        public decimal? Longitude { set; get; }

        public int LoopIndex { set; get; }
        public string LoopPath { set; get; } = string.Empty;
        public int WorkflowStepResultValueId { set; get; } = 0;

        public int WorkflowVersion { set; get; } = 0;

        public string ValueOrDisplayValue
        {
            get
            {
                if (DisplayValue is not null)
                {
                    return DisplayValue;
                }
                else if (Value is not null)
                {
                    return Value!;
                }
                return string.Empty;
            }
        }
        public bool HasData()
        {
            if (string.IsNullOrEmpty(Value) &&
                string.IsNullOrEmpty(Note) &&
                string.IsNullOrEmpty(ImageFileName) &&
                ModifiedEquipmentList is null &&
                SelectedEquipment is null)
            {
                return false;
            }
            return true;

        }

        public bool IsConsumable { set; get; }

        public bool IsItem { set; get; }

        public bool IsChecklist { set; get; }

        public List<ConsumableItem> ValueAsConsumableItems()
        {
            if (!string.IsNullOrEmpty(Value))
            {
                return JsonSerializer.Deserialize<List<ConsumableItem>>(Value);
            }
            return new();
        }
        public List<ConsumableDTO> ValueAsConsumables()
        {
            if (!string.IsNullOrEmpty(Value))
            {

                return JsonSerializer.Deserialize<List<ConsumableDTO>>(Value);
            }
            return new();
        }

        public List<TruckStockItem> ValueAsTruckStockItems()
        {
            if (!string.IsNullOrEmpty(Value))
            {
                return JsonSerializer.Deserialize<List<TruckStockItem>>(Value);
            }
            return new();
        }
    }
}
