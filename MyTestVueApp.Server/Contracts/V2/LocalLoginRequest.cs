using System.ComponentModel.DataAnnotations;

namespace MyTestVueApp.Server.Contracts.V2
{
    /// <summary>
    /// Development-only credentials used to create or reuse a local test account.
    /// </summary>
    public class LocalLoginRequest
    {
        /// <summary>
        /// Email address that uniquely identifies the local test account.
        /// </summary>
        [Required]
        [EmailAddress]
        [MaxLength(40)]
        public string Email { get; set; } = "swagger@example.com";
    }
}
