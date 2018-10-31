using System;
using System.Collections;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WebApi.Data;
using WebApi.Entities;
using WebApi.Helpers;
using WebApi.Utils;

namespace WebApi.Services
{
    public interface IUserService
    {
        Task<User> Authenticate(string userName, string password);
        IEnumerable<User> GetAll();
        User GetById(string id);
        Task<User> Create(User user, string password);
        Task<User> Update(User user, string password = null);
        void Delete(string id);
    }

    public class UserService : IUserService
    {
        private ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly AppSettings _appSettings;

        public UserService(ApplicationDbContext context,
            IOptions<AppSettings> appSettings,
            UserManager<User> userManager)
        {
            _appSettings = appSettings.Value;
            _userManager = userManager;
            _context = context;
        }

        public async Task<User> Authenticate(string userName, string password)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
                return null;

            var userVerify = await _userManager.FindByNameAsync(userName);

            // check if UserName exists
            if (userVerify == null)
                return null;


            var user =
                _userManager.PasswordHasher.VerifyHashedPassword(userVerify, userVerify.PasswordHash, password) ==
                PasswordVerificationResult.Success
                    ? userVerify
                    : null;

            // return null if user not found
            if (user == null)
                return null;


            var roles = await _userManager.GetRolesAsync(user);
            var arrayClaims = new ArrayList {new Claim(ClaimTypes.Name, user.Id)};

            foreach (var role in roles)
            {
                arrayClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            // authentication successful so generate jwt token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity((IEnumerable<Claim>) arrayClaims.Cast<Claim>().GetEnumerator()),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            user.Token = tokenHandler.WriteToken(token);

            return user;
        }

        public IEnumerable<User> GetAll()
        {
            return _context.Users;
        }

        public User GetById(string id)
        {
            return _context.Users.Find(id);
        }

        public async Task<User> Create(User user, string password)
        {
            // validation
            if (string.IsNullOrWhiteSpace(password))
                throw new AppException("Password is required");

            if (_context.Users.Any(x => x.UserName == user.UserName))
                throw new AppException("UserName \"" + user.UserName + "\" is already taken");
//
//            var passwordSalt = Salt.Create();
//            var passwordHash = Hash.Create(password, passwordSalt);
//
//            user.PasswordHash = passwordHash;
//            user.PasswordSalt = passwordSalt;
//
//            _context.Users.Add(user);
//            _context.SaveChanges();

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                Console.WriteLine(result.Errors);
                throw new EnumerableErrorsException<IdentityError>(result.Errors);
            }

            // Add a user to the default role, or any role you prefer here
            var identityResult = await _userManager.AddToRoleAsync(user, "Member");
            if (identityResult.Succeeded) return user;

            Console.WriteLine(identityResult.Errors);
            throw new EnumerableErrorsException<IdentityError>(identityResult.Errors);
//                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        }

        public async Task<User> Update(User userParam, string password = null)
        {
            var user = _context.Users.Find(userParam.Id);

            if (user == null)
                throw new AppException("User not found");

            if (userParam.UserName != user.UserName)
            {
                // UserName has changed so check if the new UserName is already taken
                if (_context.Users.Any(x => x.UserName == userParam.UserName))
                    throw new AppException("UserName " + userParam.UserName + " is already taken");
            }

            // update user properties
            user.FirstName = userParam.FirstName;
            user.LastName = userParam.LastName;
            user.UserName = userParam.UserName;


            var identityResult = await _userManager.UpdateAsync(user);
            if (!identityResult.Succeeded)
            {
                Console.WriteLine(identityResult.Errors);
                throw new EnumerableErrorsException<IdentityError>(identityResult.Errors);
            }

            return user;
        }

        public void Delete(string id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return;
            _context.Users.Remove(user);
            _context.SaveChanges();
        }
    }
}