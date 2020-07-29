using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

[System.Serializable]
public class GridFile
{
    public int cellCount;
    public int[] coord_x;
    public int[] coord_y;
    public int[] coord_z;
    public int[] typeType;
    public int[] wallDir;
    public static void Save(GridFile f)
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream fs = new FileStream(Application.dataPath + "/FurnitureSystem/gridfile.gdf", FileMode.Create);
        bf.Serialize(fs, f);
        fs.Close();
    }
    public static GridFile Load()
    {
        string path = Application.dataPath + "/FurnitureSystem/gridfile.gdf";
        if (File.Exists(path))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream fs = new FileStream(path, FileMode.Open);
            GridFile data = bf.Deserialize(fs) as GridFile;
            fs.Close();
            return data;
        }
        return null;
    }
}
