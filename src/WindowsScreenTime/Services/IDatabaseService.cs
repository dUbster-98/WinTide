using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Data.SqlClient;
using Microsoft.Data.Sqlite;
using YamlDotNet.Core.Tokens;
using SmartDateControl.UI.Units;
using System.Windows.Media.Animation;
using System.Xml.Linq;
using WindowsScreenTime.Models;

namespace WindowsScreenTime.Services
{
    public interface IDatabaseService
    {
        void InitializeDataBase();
        void WriteDataToDB(string name, string day);
        void UpdateDataToDB(string name, int time, string today);
        int QueryPastUsageTime(string name, string startDate, string endDate);
        int QueryTodayUsageTime(string name, string today);
        List<(int, string)>QueryDayTimeData(string name, string startDate, string endDate);
        List<ProcessUsage> QueryEntireProcessData();
    }

    public class DatabaseService : IDatabaseService
    {
        static string CreateTableQuery = "CREATE TABLE IF NOT EXISTS AppTimer (key INTEGER NOT NULL, name TEXT NOT NULL, time INTEGER, day TEXT, PRIMARY KEY(key AUTOINCREMENT))";
        private static string currentDirectory = Directory.GetCurrentDirectory();
        private static string pathDir = Path.Combine(currentDirectory, "data");
        public static string _filePath = pathDir + "/wst.db";
        string ConnectionString = $"Data Source={_filePath}";

        public void InitializeDataBase()         // DB 연결만 확인하는 부분
        {
            string directoryPath = Path.GetDirectoryName(_filePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }


            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();    // db파일 생성

                using (var cmd = new SqliteCommand(CreateTableQuery, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                conn.Close();
            }
        }

        public void WriteDataToDB(string name, string today)
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();

                bool isExist = false;
                string searchQuery = "SELECT * FROM AppTimer WHERE name=@name AND day=@day;";
                using (var selectCmd = new SqliteCommand(searchQuery, conn))
                {
                    selectCmd.Parameters.AddWithValue("@name", name);
                    selectCmd.Parameters.AddWithValue("@day", today);
                    using (var reader = selectCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            isExist = true;
                        }
                    }
                }

                if (!isExist)
                {
                    string insertQuery = "INSERT INTO AppTimer (name, time, day) VALUES (@name, 0, @day);";
                    using (var insertCmd = new SqliteCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@name", name);
                        insertCmd.Parameters.AddWithValue("@day", today);
                        insertCmd.ExecuteNonQuery();
                    }
                }
                conn.Close();
            }
        }

        public void UpdateDataToDB(string name, int time, string today)
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                string insertQuery = "UPDATE AppTimer SET time=@time WHERE name=@name AND day=@day;";
                using (var updateCmd = new SqliteCommand(insertQuery, conn))
                {
                    updateCmd.Parameters.AddWithValue("@name", name);
                    updateCmd.Parameters.AddWithValue("@time", time);
                    updateCmd.Parameters.AddWithValue("@day", today);
                    updateCmd.ExecuteNonQuery();
                }
                conn.Close();
            }
        }

        public int QueryPastUsageTime(string name, string startDay, string endDay)
        {
            List<int> timeList = new List<int>();

            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                string selectQuery = "SELECT * FROM AppTimer WHERE name=@name AND day BETWEEN @startDay AND @endDay;";
                using (var selectCmd = new SqliteCommand(selectQuery, conn))
                {
                    selectCmd.Parameters.AddWithValue("@name", name);
                    selectCmd.Parameters.AddWithValue("@startDay", startDay);
                    selectCmd.Parameters.AddWithValue("@endDay", endDay);

                    using (var reader = selectCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(2))
                            {
                                timeList.Add(reader.GetInt32(2));
                            }
                        }
                    }
                }
                conn.Close();
            }

            return timeList.Sum();
        }

        public int QueryTodayUsageTime(string name, string today)
        {
            List<int> timeList = new List<int>();

            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                string selectQuery = "SELECT * FROM AppTimer WHERE name=@name AND day=@today;";
                using (var selectCmd = new SqliteCommand(selectQuery, conn))
                {
                    selectCmd.Parameters.AddWithValue("@name", name);
                    selectCmd.Parameters.AddWithValue("@today", today);

                    using (var reader = selectCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(2))
                            {
                                timeList.Add(reader.GetInt32(2));
                            }
                        }
                    }
                }
                conn.Close();
            }

            return timeList.Sum();
        }

        public List<(int, string)> QueryDayTimeData(string name, string startDate, string endDate)
        {
            List<(int, string)> timeList = new();
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                string selectQuery = "SELECT * FROM AppTimer WHERE name=@name AND day BETWEEN @startDate AND @endDate;";
                using (var selectCmd = new SqliteCommand(selectQuery, conn))
                {
                    selectCmd.Parameters.AddWithValue("@name", name);
                    selectCmd.Parameters.AddWithValue("@startDate", startDate);
                    selectCmd.Parameters.AddWithValue("@endDate", endDate);
                    using (var reader = selectCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(2))
                            {
                                if (int.TryParse(reader.GetString(2), out int time))
                                {
                                    timeList.Add((time, reader.GetString(3)));
                                }
                            }
                            else
                            {
                                timeList.Add((0, reader.GetString(3)));
                            }
                        }
                    }
                }
                conn.Close();
            }
            return timeList;
        }

        public List<ProcessUsage> QueryEntireProcessData()
        {
            //HashSet<string> processSet = new HashSet<string>();
            List<string> processList = new();
            List<ProcessUsage> processes = new();
            DateTime today = DateTime.Today;
            DateTime pastDate = today.AddMonths(-1);
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                string selectQuery = "SELECT * FROM AppTimer WHERE day BETWEEN @startDate AND @endDate;";
                using (var selectCmd = new SqliteCommand(selectQuery, conn))
                {
                    selectCmd.Parameters.AddWithValue("@startDate", pastDate);
                    selectCmd.Parameters.AddWithValue("@endDate", today);
                    using (var reader = selectCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string processName = reader.GetString(1);
                            if (!processList.Contains(processName))
                            {
                                processList.Add(processName);
                            }
                 
                        }
                    }
                }
                conn.Close();
            }

            foreach (var processName in processList)
            {
                ProcessUsage process = new();
                process.ProcessName = processName;
                processes.Add(process);
            }
            
            return processes;
        }
   }
}
