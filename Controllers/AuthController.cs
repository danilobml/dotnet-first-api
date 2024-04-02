using System.Data;
using DotnetAPI.Data;
using DotnetAPI.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DotnetAPI.Helpers;
using Dapper;
using DotnetAPI.Models;
using AutoMapper;

namespace DotnetAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class AuthController(IConfiguration config) : ControllerBase
    {
        private readonly DataContextDapper _dapper = new(config);
        private readonly AuthHelpers _authHelpers = new(config);
        private readonly ReusableSql _reusableSql = new(config);
        private readonly IMapper _mapper = new Mapper(new MapperConfiguration(cfg => {
            cfg.CreateMap<UserForRegistrationDto, UserComplete>();
        }));

        [AllowAnonymous]
        [HttpPost("Register")]
        public IActionResult Register(UserForRegistrationDto userForRegistration)
        {
            if (userForRegistration.Password == userForRegistration.PasswordConfirm)
            {
                string sqlCheckUserExists = @"SELECT Email 
                                FROM TutorialAppSchema.Auth
                                WHERE Email = @EmailParam";
                DynamicParameters sqlParametersCheckUser = new();
                sqlParametersCheckUser.Add("@EmailParam", userForRegistration.Email, DbType.String);

                IEnumerable<string>? existingUsers = _dapper.LoadDataWithParams<string>(sqlCheckUserExists, sqlParametersCheckUser);

                if (!existingUsers.Any())
                {
                    UserForLoginDto userForSetPassword = new()
                    {
                        Email = userForRegistration.Email,
                        Password = userForRegistration.Password
                    };

                    bool responseAddAuth = _authHelpers.SetPassword(userForSetPassword);
                    if (responseAddAuth)
                    {
                        UserComplete userCompleteForRegistration = _mapper.Map<UserComplete>(userForRegistration);
                        userCompleteForRegistration.Active = true;

                        bool responseAddUser = _reusableSql.UpsertUser(userCompleteForRegistration);
                        if (responseAddUser)
                            return Ok();
                            
                        throw new Exception("Failed user registration - user couldn't be added to Users table.");
                    }
                    throw new Exception("Failed user registration");
                }
                else
                    throw new Exception("User with this Email already exists");
            }
            else
                throw new Exception("Password and confirmation password don't Match");
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public IActionResult Login(UserForLoginDto userForlogin)
        {
            string sqlForHashAndSalt = @"TutorialAppSchema.spRegistration_Get @Email = @EmailParam";

            DynamicParameters sqlParameters = new();
            sqlParameters.Add("@EmailParam", userForlogin.Email, DbType.String);

            UserForLoginConfirmationDto? userForLoginConfirmation = _dapper
                .LoadDataSingleWithParams<UserForLoginConfirmationDto>(sqlForHashAndSalt, sqlParameters);

            if (userForLoginConfirmation == null)
                return StatusCode(401, "Incorrect login data.");

            byte[] passwordHash = _authHelpers.GetPasswordHash(userForLoginConfirmation.PasswordSalt, userForlogin.Password);

            if (_authHelpers.ByteArrayCompare(passwordHash, userForLoginConfirmation.PasswordHash))
            {
                string sqlFindUserId = @"SELECT UserId 
                                            FROM TutorialAppSchema.Users
                                            WHERE Email = '" + userForlogin.Email + "'";
                int userId = _dapper.LoadDataSingle<int>(sqlFindUserId);

                string jwt = _authHelpers.CreateToken(userId);

                return Ok(new Dictionary<string, string> {
                            {"token", jwt}
                        });
            }
            else
                return StatusCode(401, "Incorrect login data.");
        }

        [HttpGet("RefreshToken")]
        public string RefreshToken()
        {
            string sqlFindUserId = @"SELECT UserId 
                                            FROM TutorialAppSchema.Users
                                            WHERE UserId = '" + User.FindFirst("userId")?.Value + "'";
            int userId = _dapper.LoadDataSingle<int>(sqlFindUserId);

            return _authHelpers.CreateToken(userId);
        }

        [HttpPut("ResetPassword")]
        public IActionResult ResetPassword(UserForLoginDto userForSetPassword)
        {
            if (_authHelpers.IsUser(userForSetPassword.Email, User.FindFirst("userId")!.Value))
            {
                bool responseSetPassword = _authHelpers.SetPassword(userForSetPassword);
                if (responseSetPassword)
                    return Ok();
                throw new Exception("Failed to reset password");
            }
            throw new Exception("Not authorized to reset password");
        }
    }
}