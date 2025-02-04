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
        void WriteDataToDB();
        void QueryDataToDB();
    }

    public class DataBaseService : IDatabaseService
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

        public void WriteDataToDB()
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                string insertQuery = "INSERT INTO Users (Name, Age, Email) VALUES (@Name, @Age, @Email);";
                using (var insertCmd = new SqliteCommand(insertQuery, conn))
                {
                    insertCmd.Parameters.AddWithValue("@Name", "Bob");
                    insertCmd.Parameters.AddWithValue("@Age", 25);
                    insertCmd.Parameters.AddWithValue("@Email", "bob@example.com");
                    insertCmd.ExecuteNonQuery();
                    Console.WriteLine("✅ 데이터가 삽입되었습니다.");
                }
            }
        }

        public void QueryDataToDB()
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                string selectQuery = "SELECT * FROM Users;";
                using (var selectCmd = new SqliteCommand(selectQuery, conn))
                using (var reader = selectCmd.ExecuteReader())
                {
                    Console.WriteLine("📊 데이터 목록:");
                    while (reader.Read())
                    {
                        Console.WriteLine($"ID: {reader.GetInt32(0)}, Name: {reader.GetString(1)}, Age: {reader.GetInt32(2)}, Email: {reader.GetString(3)}");
                    }
                }
            }
        }
    }
}
