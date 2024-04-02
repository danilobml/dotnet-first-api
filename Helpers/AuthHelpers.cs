using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using DotnetAPI.Data;
using DotnetAPI.Dtos;
using DotnetAPI.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.IdentityModel.Tokens;

namespace DotnetAPI.Helpers
{
    public class AuthHelpers(IConfiguration config)
    {
        private readonly IConfiguration _config = config;
        private readonly DataContextDapper _dapper = new(config);
        public byte[] GetPasswordHash(byte[] passwordSalt, string password)
        {
            string? passwordSaltPlusKey = _config.GetSection("AppSettings:PasswordKey").Value + Convert.ToBase64String(passwordSalt);

            byte[] passwordHash = KeyDerivation.Pbkdf2(
                password: password,
                salt: Encoding.ASCII.GetBytes(passwordSaltPlusKey),
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000, numBytesRequested: 256 / 8
                );

            return passwordHash;
        }

        public byte[] GeneratePasswordSalt()
        {
            byte[] passwordSalt = new byte[128 / 8];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetNonZeroBytes(passwordSalt);
            }

            return passwordSalt;
        }

        public bool ByteArrayCompare(byte[] array1, byte[] array2)
        {
            if (array1.Length != array2.Length)
                return false;

            for (int i = 0; i < array1.Length; i++)
                if (array1[i] != array2[i])
                    return false;

            return true;
        }

        public string CreateToken(int userId)
        {
            Claim[] claims = [
                new Claim("userId", userId.ToString())
            ];

            string? tokenKeyString = _config.GetSection("AppSettings:TokenKey").Value;

            SymmetricSecurityKey tokenKey = new(
                    Encoding.UTF8.GetBytes(
                        tokenKeyString != null ? tokenKeyString : ""
                    )
                );

            SigningCredentials credentials = new(tokenKey, SecurityAlgorithms.HmacSha512Signature);

            SecurityTokenDescriptor descriptor = new()
            {
                Subject = new ClaimsIdentity(claims),
                SigningCredentials = credentials,
                Expires = DateTime.Now.AddDays(1)
            };

            JwtSecurityTokenHandler tokenHandler = new();

            SecurityToken token = tokenHandler.CreateToken(descriptor);

            return tokenHandler.WriteToken(token);
        }

        public bool SetPassword(UserForLoginDto userForSetPassword)
        {
            byte[] passwordSalt = GeneratePasswordSalt();

            byte[] passwordHash = GetPasswordHash(passwordSalt, userForSetPassword.Password);

            string sqlAddAuth = @"TutorialAppSchema.spRegistration_Upsert
                                            @Email = @EmailParam, 
                                            @PasswordHash = @PasswordHashParam,
                                            @PasswordSalt = @PasswordSaltParam";

            DynamicParameters sqlParameters = new();
            sqlParameters.Add("@EmailParam", userForSetPassword.Email, DbType.String);
            sqlParameters.Add("@PasswordHashParam", passwordHash, DbType.Binary);
            sqlParameters.Add("@PasswordSaltParam", passwordSalt, DbType.Binary);

            var responseAddAuth = _dapper.ExecuteWithCustomParams(sqlAddAuth, sqlParameters);

            return responseAddAuth;
        }

        public bool IsUser(string email, string userId)
        {
            string sqlFindUser = @"EXEC TutorialAppSchema.spUsers_Get @UserId = @UserIdParam";
            DynamicParameters sqlParameters = new();
            sqlParameters.Add("@UserIdParam", userId, DbType.Int32);

            UserComplete? foundUser = _dapper.LoadDataSingleWithParams<UserComplete>(sqlFindUser, sqlParameters) ?? throw new Exception("User doesn't exist");
            return email == foundUser.Email; 
        }
    }
}