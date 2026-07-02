using System.ComponentModel.DataAnnotations;
namespace ICS.Portal.Auth.Models;
public class ValidInput
{
    /// <summary>
    /// List of ValidationResults populated during IsValid() call.
    /// </summary>
    public List<ValidationResult> ValidationResults { private set; get; } = new List<ValidationResult>();

    public bool IsValid()
    {
        Validator.TryValidateObject(this, new ValidationContext(this), ValidationResults, true);
        return ValidationResults.Count == 0;
    }

    public void ClearErrors()
    {
        ValidationResults.Clear();
    }
}
