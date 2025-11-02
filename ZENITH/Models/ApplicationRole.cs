using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ZENITH.Models
{
    public class ApplicationRole : IdentityRole
    {
        [StringLength(255)]
        public string? Description { get; set; }
    }
}
