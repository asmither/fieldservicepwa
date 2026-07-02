namespace ICS.Portal.Data.Images
{
    public class ImageFileArrayItemDTO
    {
        public ImageFileArrayItemDTO(string? title, string? caption, string contentType, int workflowId, Guid workflowFileId, DateTime? imageLastModifiedUTC)
        {
            Title = title;
            Caption = caption;
            ContentType = contentType;
            WorkflowId = workflowId;
            WorkflowFileId = workflowFileId;
            ImageLastModifiedUTC = imageLastModifiedUTC;

            //TODO: Supports older version
            if(ImageLastModifiedUTC is null)
            {
                ImageLastModifiedUTC = new(2100, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            }
        }

        public string? Title { get; }
        public string? Caption { get; }
        public string ContentType { get; }
        public int WorkflowId { get; }
        public Guid WorkflowFileId { get; }
        public DateTime? ImageLastModifiedUTC { get; }
    }

    public class ImageFileArrayItem : ImageInsertInput
    {
        private string? _title;
        private string? _caption;
        private string? _errorMessage;
        private DateTime _lastModified;

        public Action<string?> TitleChange;
        public Action<string?> CaptionChanged;

        public string? Title
        {
            get { return _title; }
            set { _title = value; TitleChange?.Invoke(_title); }
        }

        public string? Caption
        {
            get { return _caption; }
            set { _caption = value; CaptionChanged?.Invoke(_caption); }
        }

        public string? ErrorMessage
        {
            get { return _errorMessage; }
            set { _errorMessage = value; }
        }

        //TODO: Supports older version
        public DateTime ImageLastModifiedUTC { get; set; } = new DateTime(2100, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
    }
}