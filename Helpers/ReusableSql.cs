using Dapper;
using DotnetAPI.Data;
using DotnetAPI.Models;
using System.Data;

namespace DotnetAPI.Helpers
{
    public class ReusableSql(IConfiguration config)
    {
        private readonly DataContextDapper _dapper = new(config);
                
        public bool UpsertUser(UserComplete user)
        {
            string sqlUpsertUser = @"EXEC TutorialAppSchema.spUser_Upsert 
                        @FirstName = @FirstNameParam,
                        @LastName  = @LastNameParam, 
                        @Gender = @GenderParam,
                        @Email = @EmailParam,
                        @Active = @ActiveParam,
                        @Department = @DepartmentParam,
                        @JobTitle = @JobTitleParam,
                        @Salary = @SalaryParam,
                        @UserId = @UserIdParam";

            DynamicParameters sqlParameters = new();
            sqlParameters.Add("@FirstNameParam", user.FirstName, DbType.String);
            sqlParameters.Add("@LastNameParam", user.LastName, DbType.String);
            sqlParameters.Add("@GenderParam", user.Gender, DbType.String);
            sqlParameters.Add("@EmailParam", user.Email, DbType.String);
            sqlParameters.Add("@ActiveParam", user.Active, DbType.Boolean);
            sqlParameters.Add("@DepartmentParam", user.Department, DbType.String);
            sqlParameters.Add("@JobTitleParam", user.JobTitle, DbType.String);
            sqlParameters.Add("@SalaryParam", user.Salary, DbType.Decimal);
            sqlParameters.Add("@UserIdParam", user.UserId, DbType.Int32);

            var response = _dapper.ExecuteWithCustomParams(sqlUpsertUser, sqlParameters);

            return response;
        }
    }
}