using ICS.Portal.Data.Enumerations;

namespace ICS.Portal.Data.Custom;
public class JumpConditions
{
    public static bool IsMatch(int workflowDataTypeId, int jumpConditionTypeId, string jumpConditionValue, string testValue)
    {
        WorkflowDataTypes dataType = (WorkflowDataTypes)workflowDataTypeId;

        //if (string.IsNullOrEmpty(testValue))
        //{
        //    return false;
        //}

        switch (dataType)
        {
            case WorkflowDataTypes.Boolean:
            case WorkflowDataTypes.Choice:
            case WorkflowDataTypes.DataQuery:
            case WorkflowDataTypes.SmallText:
                return StringIsMatch(jumpConditionTypeId, jumpConditionValue, testValue);
            case WorkflowDataTypes.Date:
                return DateIsMatch(jumpConditionTypeId, jumpConditionValue, testValue);
            case WorkflowDataTypes.Time:
                return TimeIsMatch(jumpConditionTypeId, jumpConditionValue, testValue);
            case WorkflowDataTypes.Numeric:
                return NumberIsMatch(jumpConditionTypeId, jumpConditionValue, testValue);
            case WorkflowDataTypes.Information:
                return true;
            default:
                throw new IndexOutOfRangeException($"Data Type: {dataType} does not support jump to actions.");
        }
    }

    private static bool StringIsMatch(int jumpConditionTypeId, string jumpConditionValue, string testValue)
    {
        switch ((WorkflowJumpTypes)jumpConditionTypeId)
        {
            case WorkflowJumpTypes.Contains:
                return testValue.Contains(jumpConditionValue, StringComparison.OrdinalIgnoreCase);
            case WorkflowJumpTypes.EndsWith:
                return testValue.EndsWith(jumpConditionValue, StringComparison.OrdinalIgnoreCase);
            case WorkflowJumpTypes.Equals:
                return testValue.Equals(jumpConditionValue, StringComparison.OrdinalIgnoreCase);
            case WorkflowJumpTypes.GreaterThan:
                return testValue.CompareTo(testValue) > 0;
            case WorkflowJumpTypes.LessThan:
                return testValue.CompareTo(jumpConditionValue) < 0;
            case WorkflowJumpTypes.None:
                break;
            case WorkflowJumpTypes.NotEqual:
                return testValue != jumpConditionValue;
            case WorkflowJumpTypes.StartsWith:
                return testValue.StartsWith(jumpConditionValue);

        }
        return false;
    }
    private static bool NumberIsMatch(int jumpConditionTypeId, string jumpConditionValue, string testValue)
    {
        if (double.TryParse(jumpConditionValue, out double condition) && double.TryParse(testValue, out double test))
        {
            switch ((WorkflowJumpTypes)jumpConditionTypeId)
            {
                case WorkflowJumpTypes.Contains:
                case WorkflowJumpTypes.EndsWith:
                case WorkflowJumpTypes.StartsWith:
                    return StringIsMatch(jumpConditionTypeId, jumpConditionValue, testValue);
                case WorkflowJumpTypes.Equals:
                    return test == condition;
                case WorkflowJumpTypes.GreaterThan:
                    return test > condition;
                case WorkflowJumpTypes.LessThan:
                    return test < condition;
                case WorkflowJumpTypes.NotEqual:
                    return test != condition;
            }
            return false;
        }

        return false;
    }
    private static bool DateIsMatch(int jumpConditionTypeId, string jumpConditionValue, string testValue)
    {
        if (DateTime.TryParse(jumpConditionValue, out DateTime condition) && DateTime.TryParse(testValue, out DateTime test))
        {
            switch ((WorkflowJumpTypes)jumpConditionTypeId)
            {
                case WorkflowJumpTypes.Contains:
                case WorkflowJumpTypes.EndsWith:
                case WorkflowJumpTypes.StartsWith:
                    return StringIsMatch(jumpConditionTypeId, jumpConditionValue, testValue);
                case WorkflowJumpTypes.Equals:
                    return test.Date == condition.Date;
                case WorkflowJumpTypes.GreaterThan:
                    return test.Date > condition.Date;
                case WorkflowJumpTypes.LessThan:
                    return test.Date < condition.Date;
                case WorkflowJumpTypes.NotEqual:
                    return test.Date != condition.Date;
            }
            return false;
        }

        return false;
    }
    private static bool TimeIsMatch(int jumpConditionTypeId, string jumpConditionValue, string testValue)
    {
        if (TimeOnly.TryParse(jumpConditionValue, out TimeOnly condition) && TimeOnly.TryParse(testValue, out TimeOnly test))
        {
            switch ((WorkflowJumpTypes)jumpConditionTypeId)
            {
                case WorkflowJumpTypes.Contains:
                case WorkflowJumpTypes.EndsWith:
                case WorkflowJumpTypes.StartsWith:
                    return StringIsMatch(jumpConditionTypeId, jumpConditionValue, testValue);
                case WorkflowJumpTypes.Equals:
                    return test == condition;
                case WorkflowJumpTypes.GreaterThan:
                    return test > condition;
                case WorkflowJumpTypes.LessThan:
                    return test < condition;
                case WorkflowJumpTypes.NotEqual:
                    return test != condition;
            }
            return false;
        }

        return false;
    }
}
