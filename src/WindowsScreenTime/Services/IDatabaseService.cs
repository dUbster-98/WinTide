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
        void WriteDataToDB(string name, string time, string day);
        void UpdateDataToDB(string name, string time, string today);
        void QueryDataToDB(string name, string startDate, string endDate);
    }

    public class DatabaseService : IDatabaseService
    {
        static string CreateTableQuery = "CREATE TABLE IF NOT EXISTS \"AppTimer\" (\r\n\t\"key\"\tINTEGER NOT NULL,\r\n\t\"name\"\tTEXT NOT NULL,\r\n\t\"time\"\tINTEGER NOT NULL,\r\n\t\"day\"\tTEXT NOT NULL,\r\n\tPRIMARY KEY(\"key\" AUTOINCREMENT)\r\n);";
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
            }
        }

        public void WriteDataToDB(string name, string time, string today)
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                string insertQuery = "INSERT INTO AppTimer (name, time, day) VALUES (@name, @time, @day);";
                using (var insertCmd = new SqliteCommand(insertQuery, conn))
                {
                    insertCmd.Parameters.AddWithValue("@name", name);
                    insertCmd.Parameters.AddWithValue("@time", time);
                    insertCmd.Parameters.AddWithValue("@day", today);
                    insertCmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateDataToDB(string name, string time, string today)
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                string insertQuery = "UPDATE AppTimer SET time=@time WHERE name=@name;";
                using (var updateCmd = new SqliteCommand(insertQuery, conn))
                {
                    updateCmd.Parameters.AddWithValue("@name", name);
                    updateCmd.Parameters.AddWithValue("@time", time);
                    updateCmd.Parameters.AddWithValue("@day", today);
                    updateCmd.ExecuteNonQuery();
                }
            }
        }

        public void QueryDataToDB(string name, string startDay, string endDay)
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                string selectQuery = "SELECT * FROM AppTimer WHERE day BETWEEN @startDay AND @endDay;";
                using (var selectCmd = new SqliteCommand(selectQuery, conn))
                {
                    selectCmd.Parameters.AddWithValue("@startDay", startDay);
                    selectCmd.Parameters.AddWithValue("@endDay", endDay);
                    conn.Open();
                    using (var reader = selectCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine($"Name: {reader.GetString(1)}, Time: {reader.GetString(2)}");
                        }
                    }
                }
            }
        }
    }
}
