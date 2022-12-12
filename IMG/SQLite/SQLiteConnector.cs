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
using System.ComponentModel;
using IMG.Models;
using System.Xml.Linq;

namespace IMG.SQLite
{
    public static class SQLiteConnector
    {
        private static SQLiteConnection getConnection()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            string cs = localFolder.Path + "\\db.db";

            var con = new SQLiteConnection($@"URI=file:{cs}");
            return con;
        }

        private static string ConnectionString()
        {
            string path = ApplicationData.Current.LocalFolder.Path + "\\db.db";
            return $@"URI=file:{path}";
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
        }

        public static bool isDatabaseReady()
        {
            bool valid = true;
            using (SQLiteConnection con = new SQLiteConnection(ConnectionString()))
            {
                con.Open();
                string query = "SELECT name FROM sqlite_master WHERE type='table' AND name=@tName";
                string[] tables = { "ImagesTags", "images", "tags" };

                foreach(string name in tables)
                {
                    using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("tName", name);
                        using (var rdr = cmd.ExecuteReader())
                        {
                            if (!rdr.HasRows)
                                valid = false;
                        }
                    }
                }

            }

            return valid;
        }

        public static void initDatabase()
        {
            string[] dropTable = { "DROP TABLE IF EXISTS ImagesTags", "DROP TABLE IF EXISTS tags" , "DROP TABLE IF EXISTS categories", "DROP TABLE IF EXISTS images" };
            string[] createTable =
            {
                "CREATE TABLE images(Hash TEXT PRIMARY KEY, Extension TEXT, Name TEXT, SIZE INT, Rating TINYINT, Favorite BOOLEAN, Views INT, DateAdded DATETIME," +
                " DateTaken DATETIME, Height INT, Width INT, Orientation TINYINT, CameraManufacturer TEXT, CameraModel TEXT, Latitude DOUBLE, Longitude DOUBLE)",

                "CREATE TABLE categories(Name TEXT PRIMARY KEY, Description TEXT)",
                "CREATE TABLE tags(NAME TEXT PRIMARY KEY NOT NULL,DESCRIPTION TEXT,COLLECTIONNAME TEXT)",
                "CREATE TABLE ImagesTags(ID INTEGER PRIMARY KEY, ImageHash TEXT,TagName TEXT,FOREIGN KEY (ImageHash) REFERENCES images(Hash),FOREIGN KEY (TagName) REFERENCES tags(Name))"

            };
            using (SQLiteConnection con = new SQLiteConnection(ConnectionString()))
            {
                con.Open();
                //drop old table
                foreach (string c in dropTable)
                {
                    using (SQLiteCommand cmd = new SQLiteCommand(c, con))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                foreach (string c in createTable)
                {
                    using (SQLiteCommand cmd = new SQLiteCommand(c, con))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                con.Close();
            }

            List<Tag> tags = new List<Tag>
            {
                new Tag("everything", "system reserved"),
                new Tag("chat", "l'animale"),
                new Tag("chien", "l'animale"),
                new Tag("poule", "l'animale")
            };

            CreateTags(tags);
        }

        public static int addImages(List<ImageData> images)
        {
            int rows = 0;
            using (var con = new SQLiteConnection(ConnectionString()))
            {
                con.Open();
                
                using (var tra = con.BeginTransaction())
                {
                    //Hash TEXT PRIMARY KEY, Extension TEXT, Name TEXT, SIZE INT, Rating TINYINT, Favorite BOOLEAN, Views INT, DateAdded DATETIME," +
                    //" DateTaken DATETIME, Height INT, Width INT, Orientation TINYINT, CameraManufacturer TEXT, CameraModel TEXT, Latitude DOUBLE, Longitude DOUBLE)"
                    try
                    {
                        foreach (ImageData img in images)
                        {
                            img.Tags.Add(new Tag("everything", "system reserved"));
                            using (SQLiteCommand cmd = new SQLiteCommand(
                                "INSERT INTO images(Hash, Extension, Name, Size, Rating, Favorite, Views, DateAdded, DateTaken, Height, Width, Orientation, CameraManufacturer, " +
                                    "CameraModel, Latitude, Longitude) " +
                                "VALUES(@IHash, @IExtension, @IName, @ISize, @IRating, @IFavorite, @IViews, @IDateAdded, @IDateTaken, @IHeight, @IWidth, @IOrientation, " +
                                "@ICameraManufacturer, @ICameraModel, @ILatitude, @ILongitude)", tra.Connection))
                            {
                                cmd.Parameters.AddWithValue("@IHash", img.Hash);
                                cmd.Parameters.AddWithValue("@IExtension", img.Extension);
                                cmd.Parameters.AddWithValue("@IName", img.Name);
                                cmd.Parameters.AddWithValue("@ISize", img.Size);
                                cmd.Parameters.AddWithValue("@IRating", img.Rating);
                                cmd.Parameters.AddWithValue("@IFavorite", img.Favorite);
                                cmd.Parameters.AddWithValue("@IViews", img.Views);
                                cmd.Parameters.AddWithValue("@IDateAdded", img.DateAdded);
                                cmd.Parameters.AddWithValue("@IDateTaken", img.DateTaken);
                                cmd.Parameters.AddWithValue("@IHeight", img.Height);
                                cmd.Parameters.AddWithValue("@IWidth", img.Width);
                                cmd.Parameters.AddWithValue("@IOrientation", (ushort)img.Orientation);
                                cmd.Parameters.AddWithValue("@ICameraManufacturer", img.CameraManufacturer);
                                cmd.Parameters.AddWithValue("@ICameraModel", img.CameraModel);
                                cmd.Parameters.AddWithValue("@ILatitude", 5.3d);
                                cmd.Parameters.AddWithValue("@ILongitude", 4.5d);

                                cmd.ExecuteNonQuery();
                            }

                            foreach (Tag tag in img.Tags)
                            {
                                using (SQLiteCommand cmd = new SQLiteCommand($"INSERT INTO ImagesTags(ImageHash, TagName) VALUES(@IHash,@IName)", tra.Connection))
                                {
                                    cmd.Parameters.AddWithValue("@IHash", img.Hash);
                                    cmd.Parameters.AddWithValue("@IName", tag.Name);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                        }
                        tra.Commit();
                    }
                    catch(Exception ex) 
                    {
                        tra.Rollback();
                        throw;
                    }
                }

            }
            return rows;
        }

        public static  List<Tag> GetTags()
        {
            List<Tag> tags = new List<Tag>();
            using (SQLiteConnection con = new SQLiteConnection(ConnectionString()))
            {
                con.Open();
                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Tags", con))
                {
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            tags.Add(new Tag(rdr.GetString(0), rdr.GetString(1)));
                        }
                    }

                }
            }
            return tags;
        }

        public static bool CreateTags(List<Tag> tags)
        {
            int result = 0;
            using (SQLiteConnection con = new SQLiteConnection(ConnectionString()))
            {
                con.Open();
                foreach (Tag tag in tags)
                {
                    using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO tags(Name, Description) VALUES(@name, @description)", con))
                    {
                        cmd.Parameters.AddWithValue("@name", tag.Name);
                        cmd.Parameters.AddWithValue("@description", tag.Description);
                        cmd.Prepare();
                        result = cmd.ExecuteNonQuery();
                    }
                }
            }
            return result != 0;
        }

        public static bool CreateTag(Tag tag)
        {
            bool result = false;
            using (SQLiteConnection con = new SQLiteConnection(ConnectionString()))
            {
                con.Open();
                using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO tags(Name, Description) VALUES(@name, @description)", con))
                {
                    cmd.Parameters.AddWithValue("@name", tag.Name);
                    cmd.Parameters.AddWithValue("@description", tag.Description);
                    cmd.Prepare();
                    result = cmd.ExecuteNonQuery() != 0;
                }
            }
            return result;
        }

        public static bool TagExist(string name) 
        {
            bool hasRows = false;
            using (SQLiteConnection con = new SQLiteConnection(ConnectionString()))
            {
                con.Open();
                using (SQLiteCommand cmd = new SQLiteCommand($"SELECT Name FROM tags WHERE name = @search", con))
                {
                    cmd.Parameters.AddWithValue("@search", name);

                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        hasRows = reader.HasRows;
                    }
                }
                con.Close();
            }
            return hasRows;
        }

        public static List<ImageData> findDuplicateFromList(List<ImageData> images)
        {
            List<ImageData> duplicate = new List<ImageData>();
 
            using (SQLiteConnection con = new SQLiteConnection(ConnectionString()))
            {
                con.Open();

                foreach (ImageData img in images)
                {
                    using (SQLiteCommand cmd = new SQLiteCommand("SELECT Hash FROM Images WHERE hash = @search", con))
                    {
                        cmd.Parameters.AddWithValue("search", img.Hash);

                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                                duplicate.Add(img);
                        }
                    }
                }
                con.Close();
            }
            return duplicate;
        }

        public static bool findDuplicateFromHash(string hash)
        {
            bool duplicate = false;

            using (SQLiteConnection con = new SQLiteConnection(ConnectionString()))
            {
                con.Open();

                    using (SQLiteCommand cmd = new SQLiteCommand("SELECT Hash FROM Images WHERE hash = @search", con))
                    {
                        cmd.Parameters.AddWithValue("search", hash);

                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                        if (reader.HasRows)
                            duplicate = true;
                        }
                    }
            }
            return duplicate;
        }

        //TODO use parameters to make it sql injection safe
        public static async Task<List<ImageData>> searchImages(List<string> includeArgs, List<string> excludeArgs) 
        {
            List<ImageData> images = new List<ImageData>();

            SQLiteConnection con = getConnection();
            con.Open();
            //main command, get everything if no args
            var cmd = new SQLiteCommand(con);

            cmd.CommandText = "SELECT Images.hash, Images.extension, Images.name, Images.size, Images.Rating, Images.Favorite, Images.Views, Images.DateAdded, Images.DateTaken, " +
                    "Images.Height, Images.Width, Images.Orientation, Images.CameraManufacturer, Images.CameraModel, Images.Latitude, Images.Longitude, TagName " +
                "FROM IMAGES, ImagesTags " +
                "WHERE Images.hash = ImageHash ";

            if (includeArgs.Count > 0)
            {
                cmd.CommandText += " AND Images.hash IN (SELECT DISTINCT ImageHash FROM ImagesTags WHERE ( ";
                for (int i = 0; i < includeArgs.Count - 1; i++)
                {
                    cmd.CommandText += " TagName = @include" + i.ToString() + " OR ";
                    cmd.Parameters.AddWithValue("@include" + i.ToString(), includeArgs[i]);
                }
                cmd.CommandText += " TagName = @include" + (includeArgs.Count - 1).ToString() + ") ";
                cmd.Parameters.AddWithValue("@include" + (includeArgs.Count - 1).ToString(), includeArgs[includeArgs.Count - 1]);
                
                cmd.CommandText += " GROUP BY ImageHash HAVING COUNT(*) = " + includeArgs.Count.ToString() + ")";
            }
                
            if (excludeArgs.Count > 0)
            {
                cmd.CommandText += " AND Images.hash NOT IN (SELECT DISTINCT ImageHash FROM ImagesTags WHERE ( ";
                for (int i = 0; i < excludeArgs.Count - 1; i++)
                {
                    cmd.CommandText += " TagName = @exclude" + i.ToString() + " OR ";
                    cmd.Parameters.AddWithValue("@exclude" + i.ToString(), excludeArgs[i]);
                }
                cmd.CommandText += " TagName = @include" + (excludeArgs.Count - 1).ToString() + "))";
                cmd.Parameters.AddWithValue("@include" + (excludeArgs.Count - 1).ToString(), excludeArgs[excludeArgs.Count - 1]);
            }

            var rdr = await cmd.ExecuteReaderAsync();

            while (rdr.Read())
            {
                if(images.Count == 0 || images[images.Count - 1].Hash != rdr.GetString(0))
                {
                    ImageData img = new ImageData()
                    {
                        Hash = rdr.GetString(0),
                        Extension = rdr.GetString(1),
                        Name = rdr.GetString(2),
                        Size = (ulong)rdr.GetInt64(3),
                        Rating = rdr.GetByte(4),
                        Favorite = rdr.GetBoolean(5),
                        Views = rdr.GetInt32(6),
                        DateAdded = rdr.GetDateTime(7),
                        DateTaken = rdr.GetDateTime(8),
                        Height = (uint)rdr.GetInt64(9),
                        Width = (uint)rdr.GetInt32(10),
                        Orientation = (Windows.Storage.FileProperties.PhotoOrientation)rdr.GetInt16(11),
                        CameraManufacturer = rdr.GetString(12),
                        CameraModel = rdr.GetString(13),
                        //Latitude = rdr.GetDouble(14), crash invalid cast, TODO fix
                        //Longitude = rdr.GetDouble(15),
                        Tags = new System.Collections.ObjectModel.ObservableCollection<Tag>()
                    };
                    if (rdr.GetString(16) != "everything")
                        img.Tags.Add(new Tag(rdr.GetString(4)));

                    images.Add(img);
                }
                else
                {
                    images[images.Count - 1].Tags.Add(new Tag(rdr.GetString(4)));
                }
            }

            con.Close();
            return images;
        }

        public static bool IsDatabaseLocked()
        {
            bool locked = true;
            SQLiteConnection connection = new SQLiteConnection(ConnectionString());
            connection.Open();

            try
            {
                SQLiteCommand beginCommand = connection.CreateCommand();
                beginCommand.CommandText = "BEGIN EXCLUSIVE"; // tries to acquire the lock
                                                              // CommandTimeout is set to 0 to get error immediately if DB is locked 
                                                              // otherwise it will wait for 30 sec by default
                beginCommand.CommandTimeout = 0;
                beginCommand.ExecuteNonQuery();

                SQLiteCommand commitCommand = connection.CreateCommand();
                commitCommand.CommandText = "COMMIT"; // releases the lock immediately
                commitCommand.ExecuteNonQuery();
                locked = false;
            }
            catch (SQLiteException)
            {
                // database is locked error
            }
            finally
            {
                connection.Close();
            }

            return locked;
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
