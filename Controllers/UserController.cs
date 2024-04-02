using Microsoft.AspNetCore.Mvc;
using DotnetAPI.Models;
using DotnetAPI.Data;
using Dapper;
using System.Data;
using DotnetAPI.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace DotnetAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class UserController(IConfiguration config) : ControllerBase()
    {
        private readonly DataContextDapper _dapper = new(config);
        private readonly ReusableSql _resusableSql = new(config);

        //User
        [HttpGet("GetUsers/{userId?}/{isActive?}")]
        public IEnumerable<UserComplete> GetUsers(int userId = 0, bool isActive = false)
        {
            string sqlGetUsers = "EXEC TutorialAppSchema.spUsers_Get";
            DynamicParameters sqlParameters = new();
            string stringParameters = "";
            if (userId != 0)
            {   
                stringParameters += ", @UserId = @UserIdParam";
                sqlParameters.Add("@UserIdParam", userId, DbType.Int32);
            }
            if (isActive)
            {
                stringParameters += ", @IsActive = @IsActiveParam";
                sqlParameters.Add("@IsActiveParam", isActive, DbType.Boolean);
            }
            
            if (stringParameters.Length > 0)
                sqlGetUsers += stringParameters[1..];

            IEnumerable<UserComplete> users = _dapper.LoadDataWithParams<UserComplete>(sqlGetUsers, sqlParameters);

            return users;
        }

        [HttpPut("UpsertUser")]
        public IActionResult UpsertUser(UserComplete user)
        {
            var response = _resusableSql.UpsertUser(user);
            if (response)
                return Ok();

            string action = user.UserId != null ? "edit" : "create";
            throw new Exception($"Failed to {action} user");
        }
        
        [HttpDelete("DeleteUser/{userId}")]
        public IActionResult DeleteUser(int userId)
        {
            string sqlDeleteUser = @"EXEC TutorialAppSchema.spUser_Delete @UserId = @UserIdParam";
            
            DynamicParameters sqlParameters = new();
            sqlParameters.Add("@UserIdParam", userId, DbType.Int32);

            if (_dapper.ExecuteWithCustomParams(sqlDeleteUser, sqlParameters))
                return Ok();
            throw new Exception("Failed to delete user");
        }
    }
}