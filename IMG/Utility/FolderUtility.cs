using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace IMG.Utility
{
    public static class FolderUtility
    {
        public static async         Task
deleteAllImages()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFolder dbFolder = await localFolder.CreateFolderAsync("imgs", CreationCollisionOption.OpenIfExists);

            
            dbFolder = await localFolder.GetFolderAsync("imgs");
            await dbFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);//delete all
            dbFolder = await localFolder.CreateFolderAsync("imgs");// re create folder
            

            for (int i = 0; i < 256; i++)
            {
                await dbFolder.CreateFolderAsync(i.ToString("X2"), CreationCollisionOption.OpenIfExists);
            }
        }


    }
}
