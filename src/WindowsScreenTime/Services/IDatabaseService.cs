using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace WindowsScreenTime.Services
{
    public interface IDatabaseService
    {
        void InitializeDataBase();
        void WriteDataToDB(string name, string day);
        void UpdateDataToDB(string name, int time, string today);
        int QueryDataToDB(string name, string startDate, string endDate);
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
                    string insertQuery = "INSERT INTO AppTimer (name, day) VALUES (@name, @day);";
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

        public int QueryDataToDB(string name, string startDay, string endDay)
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
    }
}
