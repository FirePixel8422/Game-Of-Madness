using UnityEngine;
using System.IO;
using System.Threading.Tasks;
using System;


[Tooltip("Utility class for saving and loading any type of container or single values or arrays using ValueWrapper<T> and ArrayWrapper<T>")]
public static class FileManager
{
    /// <summary>
    /// Method to get all file names of a specific type in a directory
    /// </summary>
    public static (bool, string[]) GetAllFileNamesFromDirectory(string directoryPath, string fileExtension = ".json")
    {
        directoryPath = EnsurePersistentDataPath(directoryPath);

        if (Directory.Exists(directoryPath))
        {
            // Get all file paths of the specified type in the directory
            string[] filePaths = Directory.GetFiles($"{directoryPath}/", $"*{fileExtension}");

            // Extract file names from the file paths
            string[] fileNames = new string[filePaths.Length];
            for (int i = 0; i < filePaths.Length; i++)
            {
                fileNames[i] = Path.GetFileName(filePaths[i]); // Get the file name from the path
            }

            return (true, fileNames);
        }
        else
        {
            DebugLogger.LogWarning("Directory does not exist: " + directoryPath);
            return (false, new string[0]); // Returns false and an empty array if the directory doesn't exist
        }
    }



    /// <summary>
    /// Method to get all files and deserialize into a NativeArray of structs
    /// </summary>
    public static async Task<(bool, T[])> GetAllFilesFromDirectory<T>(string directoryPath, string fileExtension = ".json")
    {
        // Get all file names with the specified extension
        (bool anyFileInDirectory, string[] fileNames) = GetAllFileNamesFromDirectory(directoryPath, fileExtension);

        //if atleast one file was found in the directory that has the correct fileExtensions
        if (anyFileInDirectory)
        {
            // Create an array for deserialized objects
            T[] fileStructArray = new T[fileNames.Length];

            int successCount = 0;

            for (int i = 0; i < fileNames.Length; i++)
            {
                string fullPath = $"{directoryPath}/{fileNames[i]}";

                (bool success, T loadedStruct) = await LoadInfo<T>(fullPath);

                if (success)
                {
                    fileStructArray[successCount++] = loadedStruct; // Add object to the NativeArray
                }
                else
                {
                    DebugLogger.LogWarning($"Failed to load or deserialize file: {fileNames[i]}");
                }
            }

            // Resize the NativeArray if needed (there is no direct Resize; copy the valid items)
            if (successCount != fileStructArray.Length)
            {
                Array.Resize(ref fileStructArray, successCount);
            }

            return (successCount > 0, fileStructArray);
        }
        //no files found in directory with correct fileExtension
        else
        {
            DebugLogger.LogWarning($"No files with extension '{fileExtension}' found in directory: {directoryPath}");
            return (false, new T[0]);
        }
    }



    /// <summary>
    /// Save data using JSON serialization
    /// </summary>
    public async static Task SaveInfo<T>(T saveData, string pathPlusFileName, bool encryptFile = false)
    {
        try
        {
            pathPlusFileName = EnsurePersistentDataPath(pathPlusFileName);
            pathPlusFileName = EnsureFileExtension(pathPlusFileName);

            // Separate the directory path and the file name from the provided directoryPlusFileName string
            string directoryPath = Path.GetDirectoryName(pathPlusFileName);
            string fileName = Path.GetFileName(pathPlusFileName);

            // if directory path doesnt exist, create it
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Serialize the data to JSON format
            string outputData = JsonUtility.ToJson(saveData);

            if (encryptFile)
            {
                //encrypt if marked for encryption
                outputData = await EncryptionUtility.EncryptAsync(outputData);
            }

            //write the data to the file
            await File.WriteAllTextAsync(pathPlusFileName, outputData);

        }
        catch (Exception ex)
        {
            DebugLogger.LogError("Failed to save game data: " + ex.Message);
        }
    }



    /// <summary>
    /// Load data using JSON deserialization
    /// </summary>
    public async static Task<(bool, T)> LoadInfo<T>(string pathPlusFileName, bool decryptFile = false)
    {
        pathPlusFileName = EnsurePersistentDataPath(pathPlusFileName);
        pathPlusFileName = EnsureFileExtension(pathPlusFileName);

        if (File.Exists(pathPlusFileName))
        {
            try
            {
                // Read the encrypted data from the file
                string outputData = await File.ReadAllTextAsync(pathPlusFileName);


                if (decryptFile)
                {
                    // decrypt the data if marked for decryption
                    outputData = await EncryptionUtility.DecryptAsync(outputData);
                }

                T loadedData = JsonUtility.FromJson<T>(outputData);
                return (true, loadedData);

            }
            catch (Exception ex)
            {
                DebugLogger.LogError("Failed to load game data: " + ex.Message);
                return (false, default);
            }
        }
        else
        {
            DebugLogger.LogWarning("No save file found at: " + pathPlusFileName);
            return (false, default);
        }
    }


    /// <summary>
    /// Delete a File
    /// </summary>
    /// <returns>Whether the deletion was succesfull</returns>
    public static bool TryDeleteFile(string path)
    {
        path = EnsurePersistentDataPath(path);
        path = EnsureFileExtension(path);

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path); // Deletes the file
                return true;
            }
            else
            {
                DebugLogger.LogWarning($"File not found: {path}");
                return false;
            }
        }
        catch (IOException ex)
        {
            DebugLogger.LogError($"Failed to delete file {path}: {ex.Message}");
            return false;
        }
    }


    /// <summary>
    /// Delete a Directory (Folder)
    /// </summary>
    /// /// <returns>Whether the deletion was succesfull</returns>
    public static bool TryDeleteDirectory(string directoryPath)
    {
        directoryPath = EnsurePersistentDataPath(directoryPath);

        try
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath); // Deletes the directory

                DebugLogger.Log($"Directory deleted: {directoryPath}");
                return true;
            }
            else
            {
                DebugLogger.LogWarning($"Directory not found: {directoryPath}");
                return false;
            }
        }
        catch (IOException ex)
        {
            DebugLogger.LogError($"Failed to delete directory {directoryPath}: {ex.Message}");
            return false;
        }
    }



    /// <summary>
    /// Ensure the file path starts with "Application.persistentDataPath".
    /// </summary>
    private static string EnsurePersistentDataPath(string path)
    {
        //if path doesnt start with "Application.persistentDataPath", add it, because all files are preferably located in a fixed path
        if (path.StartsWith(Application.persistentDataPath) == false)
        {
            return $"{Application.persistentDataPath}/{path}";
        }
        else
        {
            return path;
        }
    }


    /// <summary>
    /// Ensure the file path has a valid file extension.
    /// </summary>
    private static string EnsureFileExtension(string path)
    {
        // if the "directoryPlusFileName" string doesnt have an extension (.json, .txt, etc) add .json automatically
        if (string.IsNullOrEmpty(Path.GetExtension(path)))
        {
            return path + ".json";
        }
        else
        {
            return path;
        }
    }
}


public struct ValueWrapper<T>
{
    public T Value;

    public ValueWrapper(T _value)
    {
        Value = _value;
    }
}

public struct ArrayWrapper<T>
{
    public T[] Value;

    public ArrayWrapper(T[] _values)
    {
        Value = _values;
    }
}