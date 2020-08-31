using MySql.Data.MySqlClient;
using CommandLine;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Resolver
{
    class ResolverData
    {
        [JsonPropertyName("problem_count")]
        public int ProblemCount { get; set; }

        [JsonPropertyName("solutions")]
        public Dictionary<string, Solution> Solutions { get; set; }

        [JsonPropertyName("users")]
        public Dictionary<string, User> Users { get; set; }
    }

    class Contest
    {
        public int Id { get; set; }

        public long StartTime { get; set; }

        public string Title { get; set; }
    }

    class Solution
    {
        [JsonPropertyName("user_id")]
        public string UserId { get; set; }

        [JsonPropertyName("problem_index")]
        public string ProblemIndex { get; set; }

        [JsonPropertyName("verdict")]
        public string Verdict { get; set; }

        [JsonPropertyName("submitted_seconds")]
        public long SubmittedSeconds { get; set; }
    }

    class User
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("college")]
        public string College { get; set; }

        [JsonPropertyName("is_exclude")]
        public bool IsExclude { get; set; }
    }

    class DBHandler
    {
        private MySqlConnection conn;

        private Dictionary<string, User> getUsers(int contestId)
        {
            var query = $"select t_user.id, t_user.nickname, t_user.school, t_contest_user.is_star " +
                $"from t_contest_user join t_user on t_contest_user.user_id = t_user.id " +
                $"where t_contest_user.contest_id = {contestId}";
            var cmd = new MySqlCommand(query, conn);
            var dataReader = cmd.ExecuteReader();

            var users = new Dictionary<string, User>();
            while (dataReader.Read())
            {
                var user = new User();
                user.Name = dataReader["nickname"].ToString();
                user.College = (string.IsNullOrEmpty(dataReader["school"].ToString()) ? "知名大学" : dataReader["school"].ToString()).ToUpper();
                user.IsExclude = (bool)dataReader["is_star"];

                string id = dataReader["id"].ToString();
                users[id] = user;
            }
            dataReader.Close();
            return users;
        }

        private Dictionary<int, int> getProblemsMap(int contestId)
        {
            var query = $"select problem_id, lable from t_contest_problem where contest_id = {contestId}";
            var cmd = new MySqlCommand(query, conn);
            var dataReader = cmd.ExecuteReader();

            var problems = new Dictionary<int, int>();
            while (dataReader.Read())
            {
                int problemId = (int)dataReader["problem_id"];
                int contestProblemId = dataReader["lable"].ToString()[0] - 'A' + 1;
                problems[problemId] = contestProblemId;
            }

            dataReader.Close();
            return problems;
        }

        private Contest GetContest(int contestId)
        {
            var query = $"select id, title, start_time from t_contest where id = {contestId}";
            var cmd = new MySqlCommand(query, conn);
            var dataReader = cmd.ExecuteReader();

            if (!dataReader.Read())
            {
                throw new Exception($"No such contest: {contestId}");
            }

            var contest = new Contest();
            contest.Id = contestId;
            contest.Title = dataReader["title"].ToString();
            contest.StartTime = ((DateTimeOffset)((DateTime)dataReader["start_time"])).ToUnixTimeSeconds();

            dataReader.Close();
            return contest;
        }

        private (int, Dictionary<string, Solution>) getSolutions(int contestId)
        {
            HashSet<string> failedResults = new HashSet<string> {
                "Wrong Answer",
                "Presentation Error",
                "Time Limit Exceeded",
                "Memory Limit Exceeded",
                "Output Limit Exceeded",
                "Runtime Error",
                "Restricted Function"
            };
            var problemsMap = getProblemsMap(contestId);
            var contest = GetContest(contestId);

            var query = $"select id, problem_id, result, user_id, submit_time from t_status where contest_id = {contestId}";
            var cmd = new MySqlCommand(query, conn);
            var dataReader = cmd.ExecuteReader();

            var solutions = new Dictionary<string, Solution>();
            while (dataReader.Read())
            {
                var solution = new Solution();
                var result = dataReader["result"].ToString();
                if (result == "Accepted") solution.Verdict = "AC";
                else if (failedResults.Contains(result)) solution.Verdict = "WA";
                else continue;
                solution.ProblemIndex = problemsMap[(int)dataReader["problem_id"]].ToString();
                solution.UserId = ((int)dataReader["user_id"]).ToString();
                solution.SubmittedSeconds = ((DateTimeOffset)((DateTime)dataReader["submit_time"])).ToUnixTimeSeconds() - contest.StartTime;
                solutions[dataReader["id"].ToString()] = solution;
            }

            dataReader.Close();
            return (problemsMap.Count, solutions);
        }

        public ResolverData GetResolverData(string host, int port, string user, string passwd, int contestId)
        {
            conn = new MySqlConnection();
            conn.ConnectionString = $"server={host};port={port};uid={user};pwd={passwd};database=db_nenu_oj";
            conn.Open();

            var users = getUsers(contestId);
            var (problemCount, solutions) = getSolutions(contestId);
            conn.Close();

            var contest = new ResolverData();
            contest.ProblemCount = problemCount;
            contest.Users = users;
            contest.Solutions = solutions;
            return contest;
        }
    }

    public class Options
    {
        [Option('h', "host", Required = true, HelpText = "Set database host to connect.")]
        public string Host { get; set; }

        [Option('P', "port", Required = true, HelpText = "Set database port to connect.")]
        public int Port { get; set; }

        [Option('u', "user", Required = true, HelpText = "Set database user to connect.")]
        public string User { get; set; }

        [Option('p', "passwd", Required = true, HelpText = "Set database password to connect.")]
        public string Passwd { get; set; }

        [Option('c', "contest", Required = true, HelpText = "Set contest ID.")]
        public int ContestID { get; set; }
    }

    class Program
    {
        static void InitLog()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();
        }

        static void Main(string[] args)
        {
            Options options = null;
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o => options = o);
            if (options == null)
                Environment.Exit(1);

            InitLog();

            var dbHandler = new DBHandler();

            var contestData = dbHandler.GetResolverData(options.Host, options.Port, options.User, "db_nenu_oj", options.ContestID);

            var json = JsonSerializer.Serialize(contestData);
            System.IO.File.WriteAllText(@"contest.json", json);
        }
    }
}
