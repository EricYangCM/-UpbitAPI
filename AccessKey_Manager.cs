
 class AccessKey_Manager
 {

     public AccessKey_Manager() { }


     [Serializable]
     public class SerializedKey
     {
         public string Access_Key = "";
         public string Secret_Key = "";
     }


     public bool Create_Key(string FileName, string Access, string Secret)
     {
        
         try
         {
             SerializedKey _tempKey = new SerializedKey();

             _tempKey.Access_Key = Access;
             _tempKey.Secret_Key = Secret;

             // Class Serialization
             var formatter = new BinaryFormatter();

             // Save as file
             Stream streamFileWrite = new FileStream(FileName, FileMode.Create, FileAccess.Write);
             formatter.Serialize(streamFileWrite, _tempKey);
             streamFileWrite.Close();

             return true;
         }
         catch
         {
             return false;
         }
     }


     List<string> Get_Keys(string FileName)
     {
         try
         {
             SerializedKey _tempKey = new SerializedKey();

             // Class Serialization
             var formatter = new BinaryFormatter();

             // Load from File
             Stream streamFileRead = new FileStream(FileName, FileMode.Open, FileAccess.Read);
             _tempKey = (SerializedKey)formatter.Deserialize(streamFileRead);
             streamFileRead.Close();

             List<string> _ListKey = new List<string>();

             _ListKey.Add(_tempKey.Access_Key);
             _ListKey.Add(_tempKey.Secret_Key);

             return _ListKey;
         }
         catch
         {
             return null;
         }
     }

 }
