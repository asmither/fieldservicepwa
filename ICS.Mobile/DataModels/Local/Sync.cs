namespace ICS.Mobile.DataModels.Local;

public class Sync
{
    public long Id { set; get; }

    public string? ErrorMessage { set; get; } = string.Empty;

    public Sync(long id)
    {
        Id = id; 
    }
}

public class WorkflowDeleteSync : Sync
{
    /// <summary>
    /// Used to retry workflow delete events
    /// </summary>
    /// <param name="id"></param>
    /// <param name="stepPrompt"></param>
    public WorkflowDeleteSync(long id)
        : base(id)
    {
    }
}

public class WorkflowCancelSync : Sync
{
    /// <summary>
    /// Used to retry workflow cancel events
    /// </summary>
    /// <param name="id"></param>
    /// <param name="stepPrompt"></param>
    public WorkflowCancelSync(long id)
        : base(id)
    {
    }
}

public class WorkflowCompleteSync : Sync
{
    /// <summary>
    /// Used to track workflow result step values
    /// </summary>
    /// <param name="id"></param>
    /// <param name="stepPrompt"></param>
    public WorkflowCompleteSync(long id) 
        : base(id)
    {
    }
}

public class ImageUploadSync : Sync
{
    public int WorkflowStepResultId { set; get; }
    public int LoopIndex { set; get; }
    public int ImageIndex { get; }

    public ImageUploadSync(long id, int workflowStepResultId, int loopIndex, int imageIndex) 
        : base(id)
    {
        WorkflowStepResultId = workflowStepResultId;
        LoopIndex = loopIndex;
        ImageIndex = imageIndex;
    }
}

public class WorkflowStepValueSync : Sync
{
    public WorkflowStepValueSync(long id)
        : base(id)
    {}
}

public class AttributeValueSync : Sync
{
    public AttributeValueSync(long id)
        : base(id) { }
}

