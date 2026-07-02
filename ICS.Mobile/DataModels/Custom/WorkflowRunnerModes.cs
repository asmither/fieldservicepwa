namespace ICS.Portal.Data.Custom;
public enum WorkflowRunnerModes
{
    /// <summary>
    /// place holder for no mode, default
    /// </summary>
    None,

    /// <summary>
    /// THe workflow has been completed - items may remain in queue.
    /// </summary>
    CompleteSync,

    /// <summary>
    /// Show the complete or review panel - allows review or complete.
    /// </summary>
    CompleteOrReview,

    /// <summary>
    /// When loading the workflow was found in a completed state - offer to delete local copy.
    /// </summary>
    PreviouslyCompleted,

    /// <summary>
    /// The worklow exists and is available for edit.
    /// </summary>
    InProgress,

    /// <summary>
    /// Not implemented but the intention is to set this when reviewing.
    /// </summary>
    InReview,

    /// <summary>
    /// Utilized when the checklist cannot be loaded.
    /// </summary>
    Error,

    /// <summary>
    /// On the builder side for complete vs complete sync
    /// </summary>
    Complete

}