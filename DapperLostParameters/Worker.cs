using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MiniProfiler.Integrations;

namespace DapperLostParameters
{
    public class Worker
    {
        private CancellationToken _cancellationToken;
        private string _connectionString;
        private Random _rnd;
        private int _randomInit = 12; // from 5 and more
        public TaskCompletionSource<bool> TaskCompletionSource { get; set; }

        public async Task Run(CancellationToken cancellationToken)
        {
            _rnd = new Random();
            _connectionString = ConfigurationManager.ConnectionStrings["Database1"].ConnectionString;
            TaskCompletionSource = new TaskCompletionSource<bool>();
            _cancellationToken = cancellationToken;
            await Task.Delay(1);
            try
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    await CreateRandomQuery();
                }
            }
            finally
            {
                TaskCompletionSource.SetResult(true);
            }
        }

        private async Task CreateRandomQuery()
        {
            StringBuilder sql = new StringBuilder("SELECT inc, UserId, GroupId, Status, Name, Age FROM Table1 WHERE 1=1 ");
            object parameterValues = AddWhere(sql);

            var profiler = new CustomDbProfiler();
            using (var connection = ProfiledDbConnectionFactory.New(new SqlServerDbConnectionFactory(_connectionString), profiler))
            {
                connection.Open();
                try
                {
                    Type[] types = new []{typeof(IncClass), typeof(UserClass), typeof(GroupClass),typeof(StatusClass), typeof(NameClass)};
                    string splitOn = "UserId, GroupId, Status, Name";
                    var result = await connection.QueryAsync(sql.ToString(), map: (objects) =>
                    {
                        var restaurantOrder = (IncClass)objects[0];
                        restaurantOrder.User = (UserClass)objects[1];
                        restaurantOrder.Group = (GroupClass)objects[2];
                        restaurantOrder.StatusObj = (StatusClass)objects[3];
                        restaurantOrder.NameObj = (NameClass)objects[4];
                        return restaurantOrder;
                    }, types: types, splitOn: splitOn, param: parameterValues).ConfigureAwait(false);
                }
                catch (SqlException e)
                {
                    e.Data["profile-query-after-dapper"] = profiler.GetCommands();
                    e.AppendExceptionData(sql.ToString(), parameterValues);
                    throw;
                }
            }
        }

        private object AddWhere(StringBuilder sql)
        {
            List<object> variables = new List<object>();
            ProcessField("Inc", Enumerable.Range(0, 30).ToArray(), sql, variables);
            ProcessField("UserId", GetValues<Guid>("UserId"), sql, variables);
            ProcessField("GroupId", new Guid[] { new Guid("6e93ae8f-7358-4164-8b6f-6fab51b7b1c8"), new Guid("cd3bfcb4-b2e2-41f2-b2a4-57b87b70d54f"), new Guid("40c5e515-9a4d-4938-bd71-1d42b7a692f5") }.ToArray(), sql, variables);
            ProcessField("Status", Enumerable.Range(0, 6).ToArray(), sql, variables);
            ProcessField("Name", GetValues<string>("Name").ToArray(), sql, variables);
            ProcessField("Age", Enumerable.Range(0, 30).ToArray(), sql, variables);
            return variables.Select((s, i) => new { Value = s, Index = i }).ToDictionary(x => "p" + x.Index.ToString(), x => x.Value);
        }

        private T[] GetValues<T>(string field)
        {
            var profiler = new CustomDbProfiler();
            using (var connection = ProfiledDbConnectionFactory.New(new SqlServerDbConnectionFactory(_connectionString), profiler))
            {
                var result = connection.Query<T>($"SELECT DISTINCT {field} FROM Table1");
                return result.ToArray();
            }
        }

        private void ProcessField<T>(string field, T[] values, StringBuilder sql, List<object> variables)
        {
            switch (_rnd.Next(_randomInit))
            {
                case 0:
                    break;
                case 1:
                    sql.Append($" AND {field} = @p" + variables.Count);
                    variables.Add(values[_rnd.Next(values.Length)]);
                    break;
                case 2:
                    sql.Append($" AND {field} IN @p" + variables.Count);
                    variables.Add(values.OrderBy(x => _rnd.Next()).Take(_rnd.Next(values.Length)).ToArray());
                    break;
                case 3:
                    sql.Append($" AND {field} NOT IN @p" + variables.Count);
                    variables.Add(values.OrderBy(x => _rnd.Next()).Take(_rnd.Next(values.Length)).ToArray());
                    break;
                default:
                    if (_rnd.Next(2) == 0)
                    {
                        sql.Append($" AND {field} IN @p" + variables.Count);
                        variables.Add(new[] {values[_rnd.Next(values.Length)]});
                    }
                    else
                    {
                        sql.Append($" AND {field} NOT IN @p" + variables.Count);
                        variables.Add(new[] { values[_rnd.Next(values.Length)] });
                    }
                    break;
            }
        }
    }


}
