using Dapper;
using DotnetAPI.Data;
using DotnetAPI.Models;
using System.Data;

namespace DotnetAPI.Helpers
{
    public class PostHelpers(IConfiguration config)
    {
        private readonly DataContextDapper _dapper = new(config);
                
        public bool IsUserPostCreator(int? postId, string? userId)
        {
            string sqlFindPost = @"EXEC TutorialAppSchema.spPosts_Get @PostId = @PostIdParam";
            DynamicParameters sqlParameters = new();
            sqlParameters.Add("@PostIdParam", postId, DbType.Int32);

            Post? foundPost = _dapper.LoadDataSingleWithParams<Post>(sqlFindPost, sqlParameters);
            if (foundPost == null)
                return false;

            return foundPost.UserId.ToString() == userId;
        }
    }
}