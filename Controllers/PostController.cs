using DotnetAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DotnetAPI.Models;
using DotnetAPI.Dtos;
using DotnetAPI.Helpers;
using Dapper;
using System.Data;

namespace DotnetAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class PostController(IConfiguration config) : ControllerBase
    {
        private readonly DataContextDapper _dapper = new(config);
        private readonly PostHelpers _postHelpers = new(config);

        [HttpGet("Posts/{userId?}/{postId?}/{searchParameters?}")]
        public IEnumerable<Post> GetPosts(int userId = 0, int postId = 0, string searchParameters = "None")
        {
            string sqlGetPosts = @"EXEC TutorialAppSchema.spPosts_Get";

            DynamicParameters sqlParameters = new();
            string stringParameters = "";
            if (userId != 0)
            {
                stringParameters += ", @UserId = @UserIdParam";
                sqlParameters.Add("@UserIdParam", userId, DbType.Int32);
            }
            if (postId != 0)
            {
                stringParameters += ", @PostId = @PostIdParam";
                sqlParameters.Add("@PostIdParam", postId, DbType.Int32);
            }
            if (searchParameters.ToLower() != "none")
            {
                stringParameters += ", @SearchValue = @SearchValueParam";
                sqlParameters.Add("@SearchValueParam", searchParameters, DbType.String);
            }

            if (stringParameters.Length > 0)
                sqlGetPosts += stringParameters[1..];

            IEnumerable<Post> posts = _dapper.LoadDataWithParams<Post>(sqlGetPosts, sqlParameters);
            return posts;
        }

        [HttpGet("MyPosts")]
        public IEnumerable<Post> GetMyPosts()
        {
            string? userId = User.FindFirst("userId")?.Value;
            string sqlGetMyPosts = "EXEC TutorialAppSchema.spPosts_Get @UserId = @UserIdParam";

            DynamicParameters sqlParameters = new();
            sqlParameters.Add("@UserIdParam",  userId, DbType.String);

            return _dapper.LoadDataWithParams<Post>(sqlGetMyPosts, sqlParameters);
        }

        [HttpPut("UpsertPost")]
        public IActionResult UpsertPost(PostToUpsertDto postToUpsert)
        {
            string? userId = User.FindFirst("userId")?.Value;
            // In case of update, check if user trying to update is the same who created the post
            if(postToUpsert.PostId != null)
            {
                if (!_postHelpers.IsUserPostCreator(postToUpsert.PostId, userId))
                    throw new Exception("Not authorized to update this post (not the author).");
            }
            string sqlUpsertPost = @"TutorialAppSchema.spPosts_Upsert 
                                        @UserId = @UserIdParam,
                                        @PostTitle = @PostTitleParam,
                                        @PostContent = @PostContentParam";

            DynamicParameters sqlParameters = new();
            sqlParameters.Add("@UserIdParam", userId, DbType.Int32);
            sqlParameters.Add("@PostTitleParam", postToUpsert.PostTitle, DbType.String);
            sqlParameters.Add("@PostContentParam", postToUpsert.PostContent, DbType.String);

            if (postToUpsert.PostId != null)
            {
                sqlUpsertPost += ", @PostId = @PostIdParam";
                sqlParameters.Add("@PostIdParam", postToUpsert.PostId, DbType.Int32);
            }

            string action = postToUpsert.PostId != null ? "update" : "create";
            var response = _dapper.ExecuteWithCustomParams(sqlUpsertPost, sqlParameters);
            if (response)
                return Ok($"Post {action}d");
            throw new Exception($"Failed to {action} post");
        }

        [HttpDelete("DeletePost/{postId}")]
        public IActionResult DeletePost(int postId)
        {
            string? userId = User.FindFirst("userId")?.Value;
            //Check if user trying to delete is the same who created the post:
            if (_postHelpers.IsUserPostCreator(postId, userId))
            {
                string sqlDeletePost = @"EXEC TutorialAppSchema.spPosts_Delete @UserId = @UserIdParam, @PostId = @PostIdParam";

                DynamicParameters sqlParameters = new();
                sqlParameters.Add("@PostIdParam", postId, DbType.Int32);
                sqlParameters.Add("@UserIdParam", userId, DbType.Int32);

                var response = _dapper.ExecuteWithCustomParams(sqlDeletePost, sqlParameters);

                if (response)
                    return Ok("Post deleted.");
                throw new Exception("Failed to delete post.");
            }

            throw new Exception("Not authorized to delete this post (not the author).");
        }
    }
}