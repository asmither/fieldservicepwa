using System.ComponentModel.DataAnnotations;

namespace ICS.Portal.Auth.Models
{
    public class OnboardUser
    {
        [Required]
        [MaxLength(16)]
        public string? UserName {set; get;}

        [Required]
        [MaxLength(32)]
        public string? FirstName {set; get;}

        [Required]
        [MaxLength(16)]
        public string? Phone {set; get;}

        [Required]
        [MaxLength(128)]
        [EmailAddress]
        public string? Email {set; get;}

        [Required]
        public List<int>? Services {set; get;}

        /// <summary>
        /// List of ValidationResults populated during IsValid() call.
        /// </summary>
        public List<ValidationResult> ValidationResults { private set; get; } = new List<ValidationResult>();

        public bool IsValid()
        {
            Validator.TryValidateObject(this, new ValidationContext(this), ValidationResults, true);
            if(Services == null || Services.Count == 0)
            {
                ValidationResults.Add(new ValidationResult("Services are required, choose at least 1", new string[] { nameof(Services) }));
            }
            return ValidationResults.Count == 0;
        }
    }
}
