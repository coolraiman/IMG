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
using IMG.Wrappers;

namespace IMG.SQLite
{
    /// <summary>
    /// This class manage every interaction with the SQL database
    /// </summary>
    public static class SQLiteConnector
    {
        /// <summary>
        /// get the connection *unsafe
        /// </summary>
        /// <returns></returns>
        private static SQLiteConnection getConnection()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            string cs = localFolder.Path + "\\db.db";

            var con = new SQLiteConnection($@"URI=file:{cs}");
            return con;
        }

        /// <summary>
        /// build the connection string
        /// </summary>
        /// <returns></returns>
        private static string ConnectionString()
        {
            string path = ApplicationData.Current.LocalFolder.Path + "\\db.db";
            return $@"URI=file:{path}";
        }

        /// <summary>
        /// check the current version of the SQLite database
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// verify if the database contains all the table (on every launch)
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Create the tables and the basic system reserved tags
        /// </summary>
        public static void initDatabase()
        {
            string[] dropTable = { "DROP TABLE IF EXISTS ImagesTags", "DROP TABLE IF EXISTS tags" , "DROP TABLE IF EXISTS images" };
            string[] createTable =
            {
                "CREATE TABLE images(Hash TEXT PRIMARY KEY, Extension TEXT, Name TEXT, SIZE INT, Rating TINYINT, Favorite BOOLEAN, Views INT, DateAdded DATETIME," +
                " DateTaken DATETIME, Height INT, Width INT, Orientation TINYINT, CameraManufacturer TEXT, CameraModel TEXT, Latitude DOUBLE, Longitude DOUBLE)",

                "CREATE TABLE tags(NAME TEXT PRIMARY KEY NOT NULL,DESCRIPTION TEXT, Protected BOOLEAN DEFAULT 0 NOT NULL)",
                "CREATE TABLE ImagesTags(ID INTEGER PRIMARY KEY, ImageHash TEXT,TagName TEXT,FOREIGN KEY (ImageHash) REFERENCES images(Hash)," +
                //                                              Keep database integrity, very important
                    "FOREIGN KEY (TagName) REFERENCES tags(Name) ON DELETE CASCADE)"

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
                }//re create the tables
                foreach (string c in createTable)
                {
                    using (SQLiteCommand cmd = new SQLiteCommand(c, con))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            List<Tag> tags = new List<Tag>
            {
                new Tag("everything", "system reserved"),//very important hiden tags for the search function optimisation
                new Tag("animated", "system reserved"),
            };
            //create important tags
            CreateProtectedTags(tags);
        }

        /// <summary>
        /// Add an image to the database with most of its data
        /// </summary>
        /// <param name="images"></param>
        /// <returns></returns>
        public static int addImages(List<ImageData> images)
        {
            int rows = 0;
            using (var con = new SQLiteConnection(ConnectionString()))
            {
                con.Open();
                
                using (var tra = con.BeginTransaction())
                {
                    try
                    {
                        foreach (ImageData img in images)
                        {//add the hidden tag to the image
                            img.Tags.Add(new Tag("everything", "system reserved"));
                            //standard insert sql command
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
        /// <summary>
        /// get every tags
        /// </summary>
        /// <returns></returns>
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
                            tags.Add(new Tag(rdr.GetString(0), rdr.GetString(1), rdr.GetBoolean(2)));
                        }
                    }

                }
            }
            return tags;
        }

        /// <summary>
        /// same as getTags but with wrapped tags
        /// </summary>
        /// <returns></returns>
        public static List<TagWrapper> GetWrappedTags()
        {
            List<TagWrapper> tags = new List<TagWrapper>();
            using (SQLiteConnection con = new SQLiteConnection(ConnectionString()))
            {
                con.Open();
                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Tags", con))
                {
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            tags.Add(new TagWrapper(
                                new Tag(rdr.GetString(0), rdr.GetString(1), rdr.GetBoolean(2))
                                ));
                        }
                    }

                }
            }
            return tags;
        }
        /// <summary>
        /// this method is the only way to create protected tags for system reserved tags
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        private static bool CreateProtectedTags(List<Tag> tags)
        {
            int result = 0;
            using (SQLiteConnection con = new SQLiteConnection(ConnectionString()))
            {
                con.Open();
                foreach (Tag tag in tags)
                {
                    using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO tags(Name, Description, Protected) VALUES(@name, @description, 1)", con))
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

        /// <summary>
        /// create standard tags
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public static bool CreateTags(List<Tag> tags)
        {
            int result = 0;
            using (SQLiteConnection con = new SQLiteConnection(ConnectionString()))
            {
                con.Open();
                foreach (Tag tag in tags)
                {
                    using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO tags(Name, Description, Protected) VALUES(@name, @description, 0)", con))
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

        /// <summary>
        /// Create a single tag
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
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

        /// <summary>
        /// delete a tag from the database if it is not protected
        /// also remove every references to that tag
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static bool DeleteTag(Tag tag)
        {
            if (tag.ProtectedTag)
                return false;
            bool deleted = false;
            using (SQLiteConnection con = new SQLiteConnection(ConnectionString()))
            {
                con.Open();
                
                //imageYags cascade delete will remove any reference to deleted tag
                using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Tags WHERE Name = @name AND Protected = 0", con))
                {
                    cmd.Parameters.AddWithValue("@name", tag.Name);
                        
                    deleted = cmd.ExecuteNonQuery() != 0;
                }

            }

            return deleted;
        }

        public static int DeleteTags(List<Tag> tags)
        {
            int deletedRows = 0;
            using (SQLiteConnection con = new SQLiteConnection(ConnectionString()))
            {
                con.Open();

                foreach (Tag tag in tags)
                {

                    if (tag.ProtectedTag)
                        continue;
                    //imageTags cascade delete will remove any reference to deleted tag
                    using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Tags WHERE Name = @name AND Protected = 0", con))
                    {
                        cmd.Parameters.AddWithValue("@name", tag.Name);

                        deletedRows += cmd.ExecuteNonQuery();
                    }
                }

            }

            return deletedRows;
        }

        public static int UpdateTagDescriptions(List<Tag> tags)
        {
            int rows = 0;

            using (SQLiteConnection con = new SQLiteConnection(ConnectionString()))
            {
                con.Open();
                foreach (Tag t in tags)
                {
                    //imageYags cascade delete will remove any reference to deleted tag
                    using (SQLiteCommand cmd = new SQLiteCommand(
                        "UPDATE Tags SET description = @description WHERE Name = @name AND Protected = 0", con))
                    {
                        cmd.Parameters.AddWithValue("@description", t.Description);
                        cmd.Parameters.AddWithValue("@name", t.Name);

                        rows += cmd.ExecuteNonQuery();
                    }
                }

            }

            return rows;
        }

        public static int SetImageFavorite(string hash, bool favorite)
        {
            int rows = 0;
            using(SQLiteConnection con = new SQLiteConnection(ConnectionString()))
            {
                con.Open();
                using(SQLiteCommand cmd = new SQLiteCommand("UPDATE Images SET Favorite = @Favorite WHERE Hash = @Hash", con))
                {
                    cmd.Parameters.AddWithValue("@Favorite", favorite);
                    cmd.Parameters.AddWithValue("@Hash", hash);

                    rows = cmd.ExecuteNonQuery();
                }
            }
            return rows;
        }

        public static int SetImageRating(string hash, int rating)
        {
            if (rating < 0 || rating > 5)
                return -1;

            int rows = 0;
            using (SQLiteConnection con = new SQLiteConnection(ConnectionString()))
            {
                con.Open();
                using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Images SET Rating = @Rating WHERE Hash = @Hash", con))
                {
                    cmd.Parameters.AddWithValue("@Rating", rating);
                    cmd.Parameters.AddWithValue("@Hash", hash);

                    rows = cmd.ExecuteNonQuery();
                }
            }
            return rows;
        }

        /// <summary>
        /// find a tag in the database from its name
        /// </summary>
        /// <param name="name"></param>
        /// <returns>true if tag exist</returns>
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

        /// <summary>
        /// find all duplicates from DB
        /// </summary>
        /// <param name="images"></param>
        /// <returns>return the list of ImageData that are duplicate</returns>
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

        /// <summary>
        /// simplified version of findDuplicateFromList
        /// </summary>
        /// <param name="hash"></param>
        /// <returns>true if duplicate</returns>
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

        /// <summary>
        /// this function find all ImageData who have all the includeArgs and none of the excludeArgs
        /// </summary>
        /// <param name="includeArgs"></param>
        /// <param name="excludeArgs"></param>
        /// <returns>list of found imageData</returns>
        public static async Task<List<ImageData>> searchImages(List<string> includeArgs, List<string> excludeArgs) 
        {
            List<ImageData> images = new List<ImageData>();
            //simplified example of the following query without any args
            //SELECT Images.*, ImagesTags.TagName FROM IMAGES, ImagesTags HERE Images.hash = ImageHash
            using (SQLiteConnection con = new SQLiteConnection(ConnectionString()))
            {
                con.Open();
                var cmd = new SQLiteCommand(con);
                //base command, get everything if no args
                cmd.CommandText = "SELECT Images.hash, Images.extension, Images.name, Images.size, Images.Rating, Images.Favorite, Images.Views, Images.DateAdded, Images.DateTaken, " +
                        "Images.Height, Images.Width, Images.Orientation, Images.CameraManufacturer, Images.CameraModel, Images.Latitude, Images.Longitude, TagName " +
                    "FROM IMAGES, ImagesTags " +
                    "WHERE Images.hash = ImageHash ";

                //add subquery with every includeArgs
                //ex with 2 args, group by allow to only return the hash of image who have every include args
                //SELECT Images.*, ImagesTags.TagName FROM IMAGES, ImagesTags HERE Images.hash = ImageHash AND
                ////Images.hash IN (SELECT DISTINCT ImageHash FROM ImagesTags WHERE (TagName = arg1 OR TagName = arg2) GROUP BY ImageHash HAVING COUNT(*) = 2)
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
                //exclude args is simpler and does not depend on include args, images can be searched only by exclusion
                //ex without include args
                //SELECT Images.*, ImagesTags.TagName FROM IMAGES, ImagesTags HERE Images.hash = ImageHash AND *(optional include args part)
                ////Images.hash NOT IN (SELECT DISTINCT ImageHash FROM ImagesTags WHERE ( TagName = arg1 OR TagName = arg2) GROUP BY ImageHash )
                if (excludeArgs.Count > 0)
                {
                    cmd.CommandText += " AND Images.hash NOT IN (SELECT DISTINCT ImageHash FROM ImagesTags WHERE ( ";
                    for (int i = 0; i < excludeArgs.Count - 1; i++)
                    {
                        cmd.CommandText += " TagName = @exclude" + i.ToString() + " OR ";
                        cmd.Parameters.AddWithValue("@exclude" + i.ToString(), excludeArgs[i]);
                    }
                    cmd.CommandText += " TagName = @include" + (excludeArgs.Count - 1).ToString() + ")";
                    cmd.Parameters.AddWithValue("@include" + (excludeArgs.Count - 1).ToString(), excludeArgs[excludeArgs.Count - 1]);

                    cmd.CommandText += " )"; //big optimisation
                }

                var rdr = await cmd.ExecuteReaderAsync();

                while (rdr.Read())
                {
                    if (images.Count == 0 || images[images.Count - 1].Hash != rdr.GetString(0))
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
                        //hidden tag, do not add
                        if (rdr.GetString(16) != "everything")
                            img.Tags.Add(new Tag(rdr.GetString(16)));

                        images.Add(img);
                    }
                    else
                    {
                        images[images.Count - 1].Tags.Add(new Tag(rdr.GetString(16)));
                    }
                }
                rdr.Close();
            }
            return images;
        }
        /// <summary>
        /// scan database to see if some image are not linked to any image file
        /// </summary>
        public static async Task<int> ScanDatabaseIntegrity()
        {
            StorageFolder imgsFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("imgs");
            List<string> unlinked = new List<string>();
            using (SQLiteConnection con = new SQLiteConnection(ConnectionString()))
            {
                con.Open();
                using (SQLiteCommand cmd = con.CreateCommand()) 
                {
                    cmd.CommandText = "SELECT Hash, extension FROM Images";

                    using (SQLiteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read()) 
                        {
                            string hash = rdr.GetString(0);
                            StorageFolder subFolder = await imgsFolder.GetFolderAsync(hash.Substring(0,2));
                            var file = subFolder.TryGetItemAsync(hash + rdr.GetString(1));
                            if(file == null)
                                unlinked.Add(hash);
                        }
                    }
                }

                foreach(string hash in unlinked) 
                {
                    using (SQLiteCommand cmd = con.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM ImagesTags WHERE Hash = @hash";
                        cmd.Parameters.AddWithValue("@hash", "hash");
                        cmd.ExecuteNonQuery();
                    }
                    using (SQLiteCommand cmd = con.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM Images WHERE Hash = @hash";
                        cmd.Parameters.AddWithValue("@hash", "hash");
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            return unlinked.Count;
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
                //db is locked
            }
            finally
            {
                connection.Close();
            }

            return locked;
        }

    }
}
