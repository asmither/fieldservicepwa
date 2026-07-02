using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICS.Portal.Data.Custom;

[Serializable]
public class ICSTextSummarization
{

    public string Id { set; get; } = "";
    public string Status { set; get; } = "";
    public int StatusCode
    {
        get
        {
            if (Status == "notStarted") return 0;
            if (Status == "succeeded") return 1;
            if (Status == "failed") return 2;
            if (Status == "Rejected") return 3;
            if (Status == "running") return 4;
            if (Status == "partiallyCompleted") return 5;
            if (Status == "cancelled") return 7;
            if (Status == "cancelling") return 6;
            return 0;
        }
    }

    public DateTimeOffset CreatedOn { set; get; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresOn { set; get; } = DateTimeOffset.UtcNow;
    public string SourceText { set; get; } = "";

    public List<string> Sentences { set; get; } = new List<string>();
    public string Sentence
    {
        get
        {
            if (Sentences is not null && Sentences.Count > 0)
                return Sentences[0];
            return "";
        }
    }

    public List<string> Sentiments { set; get; } = new List<string>();
    public string Sentiment
    {
        get
        {
            if (Sentiments is not null && Sentiments.Count > 0)
                return Sentiments[0];
            return "";
        }
    }

    public List<string> AbstractSummaries { set; get; } = new List<string>();

    public int AbstractSummaryCount
    {
        get
        {
            if (AbstractSummaries is not null)
                return AbstractSummaries.Count;
            return 0;
        }
    }

    public string AbstractSummary
    {
        get
        {
            if (AbstractSummaries is not null && AbstractSummaries.Count > 0)
                return AbstractSummaries[0];
            return "";
        }
    }

    public string? Error
    {
        get
        {
            if (Errors is not null && Errors.Count > 0)
                return Errors[0];
            return "";
        }
    }

    public List<string> Errors { set; get; } = new List<string>();

    public double CallTime { set; get; } = 0.0;
    
    public List<Entities> EntitiesList { set; get; } = new List<Entities>();
    public bool HasEntities
    {
        get
        {
            if (EntitiesList is not null && EntitiesList.Count > 0)
                return true;
            return false;
        }
    }

    public List<Entities> PIIEntitiesList { set; get; } = new List<Entities>();
    public bool HasPIIEntities
    {
        get
        {
            if (PIIEntitiesList is not null && PIIEntitiesList.Count > 0)
                return true;
            return false;
        }
    }

    public class Entities
    {
        public string Text { set; get; } = "";
        public string Category { set; get; } = "";
        public string SubCategory { set; get; } = "";
        public double ConfidenceScore { set; get; } = 0.0;

    }

}