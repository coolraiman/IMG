using System;
using System.IO;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using System.Data.SqlClient;

namespace IMG.SQLite
{
    public static class SQLiteConnector
    {
        public static string GetImageDirectory()
        {
            string cur = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string imageDir = Path.Combine(cur, @"C:\Users\craym\Documents\IMG", "Images.db");
            return imageDir;
        }

        private static SQLiteConnection getConnection()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            string cs = localFolder.Path + "\\db.db";

            var con = new SQLiteConnection($@"URI=file:{cs}");
            return con;
        }

        public static string checkVersion()
        {
            string cs = "Data Source=:memory:";
            string stm = "SELECT SQLITE_VERSION()";

            var con = new SQLiteConnection(cs);
            con.Open();

            var cmd = new SQLiteCommand(stm, con);
            string version = cmd.ExecuteScalar().ToString();

            return "SQLite version:" + version;
            //StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            //return localFolder.Path + "\\test.db";
        }

        public static void initDatabase()
        {
            var con = getConnection();
            con.Open();
            var cmd = new SQLiteCommand(con);
            //drop old table
            cmd.CommandText = "DROP TABLE IF EXISTS ImagesTags";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "DROP TABLE IF EXISTS tags";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "DROP TABLE IF EXISTS categories";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "DROP TABLE IF EXISTS images";
            cmd.ExecuteNonQuery();

            //create tables
            cmd.CommandText = "CREATE TABLE images(Hash TEXT PRIMARY KEY, Extension TEXT, Name TEXT)";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "CREATE TABLE categories(Name TEXT PRIMARY KEY, Description TEXT)";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "CREATE TABLE tags(ID INTEGER PRIMARY KEY AUTOINCREMENT," +
                              "NAME TEXT NOT NULL," +
                              "DESCRIPTION TEXT," +
                              "COLLECTIONNAME TEXT)";
                              //"FOREIGN KEY(Tags_FK_categories) REFERENCES categories(name))";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "CREATE TABLE ImagesTags(" +
                              "ImageHash TEXT, TagId Integer," +
                              "Primary KEY (ImageHash, TagId))";
                              //"FOREIGN KEY(hash_FK_images) REFERENCES images(Hash))" +
                              //"FOREIGN KEY(tag_FK_tags) REFERENCES tags(ID))";
            cmd.ExecuteNonQuery();
            con.Close();
        }

        public static async Task<int> addImages(List<Models.ImageData> images)
        {
            SQLiteConnection con = getConnection();
            await con.OpenAsync();
            int rows = 0;
            var cmd = new SQLiteCommand(con);
            
            foreach(Models.ImageData img in images)
            {
                cmd.CommandText = $"INSERT INTO images(Hash, Extension, Name) VALUES('{img.Hash}','{img.Extension}', '{img.Name}')";
                rows += cmd.ExecuteNonQuery();
            }

            con.Close();
            return rows;
        }

        public static bool createTable()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;


            string cs = localFolder.Path + "\\db.db";
            //SQLiteConnection.CreateFile(cs);
            //string cs = "test.db";

            var con = new SQLiteConnection($@"URI=file:{cs}");
            con.Open();

            var cmd = new SQLiteCommand(con);

            //cmd.CommandText = "DROP TABLE IF EXISTS cars";
            //cmd.ExecuteNonQuery();

            //cmd.CommandText = @"CREATE TABLE cars(id INTEGER PRIMARY KEY,
            //name TEXT, price INT)";
            //cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO cars(name, price) VALUES('Audi',52642)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO cars(name, price) VALUES('Mercedes',57127)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO cars(name, price) VALUES('Skoda',9000)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO cars(name, price) VALUES('Volvo',29000)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO cars(name, price) VALUES('Bentley',350000)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO cars(name, price) VALUES('Citroen',21000)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO cars(name, price) VALUES('Hummer',41400)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO cars(name, price) VALUES('Volkswagen',21600)";
            cmd.ExecuteNonQuery();

            Console.WriteLine("Table cars created");

            con.Close();

            return true;
        }

        public static void insertPrepare()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            string cs = localFolder.Path + "\\test.db";

            var con = new SQLiteConnection($@"URI=file:{cs}");
            con.Open();

            var cmd = new SQLiteCommand(con);
            cmd.CommandText = "INSERT INTO cars(name, price) VALUES(@name, @price)";

            cmd.Parameters.AddWithValue("@name", "BMW");
            cmd.Parameters.AddWithValue("@price", 36600);
            cmd.Prepare();

            cmd.ExecuteNonQuery();

            Console.WriteLine("row inserted");

            con.Close();
        }

        public static string readData()
        {

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            string cs = localFolder.Path + "\\test.db";

            var con = new SQLiteConnection($@"URI=file:{cs}");

            con.Open();

            string stm = "SELECT * FROM cars LIMIT 5";

            var cmd = new SQLiteCommand(stm, con);
            SQLiteDataReader rdr = cmd.ExecuteReader();

            string data = "\n";
            while (rdr.Read())
            {
                data += $"{rdr.GetInt32(0)} {rdr.GetString(1)} {rdr.GetInt32(2)}" + "\n";
            }

            con.Close();

            return data;
        }
    }
}
