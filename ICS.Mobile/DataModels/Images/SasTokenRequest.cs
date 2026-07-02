using System.ComponentModel.DataAnnotations;

namespace ICS.Portal.Data.Images
{
    public class SasTokenRequest
    {
        public SasTokenRequest(string fileName, string contentType)
        {
            FileName = fileName;
            ContentType = contentType;
        }

        [Required]
        public string FileName { get; }

        [Required]
        public string ContentType { get; }

        /// <summary>
        /// List of ValidationResults populated during IsValid() call.
        /// </summary>
        public List<ValidationResult> ValidationResults { private set; get; } = new List<ValidationResult>();

        /// <summary>
        /// Validates the object according to the annotations assigned by the SQL Plus tags.
        /// When the method returns false the ValidationErrors will have a count > 1.
        /// </summary>
        /// <returns>True|False based on the valid state of the object.</returns>
        public virtual bool IsValid()
        {
            ClearErrors();
            Validator.TryValidateObject(this, new ValidationContext(this), ValidationResults, true);
            return ValidationResults.Count == 0;
        }

        /// <summary>
        /// Clears ValidationResults of any previous errors.
        /// </summary>
        public virtual void ClearErrors()
        {
            ValidationResults.Clear();
        }
    }
}
