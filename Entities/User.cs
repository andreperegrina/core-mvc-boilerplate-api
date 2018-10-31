using Microsoft.AspNetCore.Identity;

namespace WebApi.Entities
{
    public class User : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PasswordSalt { get; set; }
        [ProtectedPersonalData]
        public string Token { get; set; }
    }
}