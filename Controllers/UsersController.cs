using System;
using System.Collections;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WebApi.Dtos;
using WebApi.Entities;
using WebApi.Helpers;
using WebApi.Services;
using WebApi.Utils;

namespace WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class UsersController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        private IUserService _userService;
        private IMapper _mapper;
        private readonly AppSettings _appSettings;

        public UsersController(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<User> signInManager,
            IUserService userService,
            IMapper mapper,
            IOptions<AppSettings> appSettings)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userService = userService;
            _mapper = mapper;
            _appSettings = appSettings.Value;
        }


        [AllowAnonymous]
        [HttpPost("authenticate")]
        public IActionResult Authenticate([FromBody] UserDto userDto)
        {
            var user = _userService.Authenticate(userDto.Username, userDto.Password).Result;

            if (user == null)
                return BadRequest(new {message = "Username or password is incorrect"});

            var response = new Response(Status.Success, new
            {
                user.Id,
                Username = user.UserName,
                user.FirstName,
                user.LastName,
                tokenType = "Bearer",
                user.Token
            });
            // return basic user info (without password) and token to store client side
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public IActionResult Register([FromBody] UserDto userDto)
        {
            // map dto to entity
            var user = _mapper.Map<User>(userDto);

            try
            {
                try
                {
                    var userSave = _userService.Create(user, userDto.Password).Result;

                    var response = new Response(Status.Success, new
                    {
                        Username = userSave.UserName,
                        userSave.FirstName,
                        userSave.LastName
                    });
                    return Ok(response);
                }
                catch (AggregateException e)
                {
                    var arrayErrors = new ArrayList();
                    e.Handle(x =>
                    {
                        if (!(x is EnumerableErrorsException<IdentityError>)) return false;
                        var errorsException = (EnumerableErrorsException<IdentityError>) x;
                        foreach (var error in errorsException.Errors)
                        {
                            arrayErrors.Add(new {error.Code, error.Description});
                        }

                        return true;
                    });
                    return BadRequest(new Response(Status.Fail, arrayErrors));
                }
            }
            catch (AppException ex)
            {
                // return error message if there was an exception
                return BadRequest(new {message = ex.Message});
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult GetAll()
        {
            var users = _userService.GetAll();
            var userDtos = _mapper.Map<IList<UserDto>>(users);
            return Ok(new Response(Status.Success, userDtos));
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Member")]
        public IActionResult GetById(string id)
        {
            var user = _userService.GetById(id);
            object userResponse = null;
            if (user != null)
                userResponse = new
                {
                    user.Id,
                    Username = user.UserName,
                    user.FirstName,
                    user.LastName
                };
            return Ok(new Response(Status.Success, userResponse));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult Update(string id, [FromBody] UserDto userDto)
        {
            // map dto to entity and set id
            var user = _mapper.Map<User>(userDto);
            user.Id = id;

            try
            {
                // save 
                var userUpdated =_userService.Update(user, userDto.Password).Result;
                return Ok(new
                {
                    userUpdated.UserName,
                    userUpdated.FirstName,
                    userUpdated.LastName
                });
            }
            catch (AggregateException e)
            {
                var arrayErrors = new ArrayList();
                if (e.InnerException.GetType() == typeof(AppException))
                {
                    return BadRequest(new {message = e.InnerException.Message});
                }

                e.Handle(x =>
                {
                    if (!(x is EnumerableErrorsException<IdentityError>)) return false;
                    var errorsException = (EnumerableErrorsException<IdentityError>) x;
                    foreach (var error in errorsException.Errors)
                    {
                        arrayErrors.Add(new {error.Code, error.Description});
                    }

                    return true;
                });
                return BadRequest(new Response(Status.Fail, arrayErrors));
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(string id)
        {
            _userService.Delete(id);
            return Ok();
        }
    }
}