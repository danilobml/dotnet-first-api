namespace DotnetAPI.Dtos
{
    public partial class PostToUpsertDto
    {
        public int? PostId { get; set; }
        public string PostTitle { get; set; } = "";
        public string PostContent { get; set; } = "";
    }
}