using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DotnetAPI.Data
{
    public class DataContextDapper(IConfiguration config)
    {
        private readonly IConfiguration _config = config;

        public IDbConnection CreateConnection()
        {
            IDbConnection dbConnection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));

            return dbConnection;
        }

        public T? LoadDataSingle<T>(string sqlCommand)
        {
            IDbConnection dbConnection = CreateConnection();
            return dbConnection.QueryFirstOrDefault<T>(sqlCommand);
        }

        public bool ExecuteWithCustomParams(string sqlCommand, DynamicParameters sqlParameters)
        {
            IDbConnection dbConnection = CreateConnection();
            return dbConnection.Execute(sqlCommand, sqlParameters) > 0;
        }
        
        public IEnumerable<T> LoadDataWithParams<T>(string sqlCommand, DynamicParameters sqlParameters)
        {
            IDbConnection dbConnection = CreateConnection();
            return dbConnection.Query<T>(sqlCommand, sqlParameters);
        }

        public T? LoadDataSingleWithParams<T>(string sqlCommand, DynamicParameters sqlParameters)
        {
            IDbConnection dbConnection = CreateConnection();
            return dbConnection.QueryFirstOrDefault<T>(sqlCommand, sqlParameters);
        }
    }
}