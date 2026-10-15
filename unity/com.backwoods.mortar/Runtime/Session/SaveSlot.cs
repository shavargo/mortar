using System;
using System.IO;
using Mortar.Game.Simulation;
using Mortar.Json.Content;
using Mortar.Json.Saves;
using UnityEngine;

namespace Mortar.Unity
{
    public sealed class SaveSlot
    {
        public SaveSlot(string folder, string name = "slot1")
        {
            Folder = folder;
            FilePath = Path.Combine(folder, name + ".json");
        }

        public string Folder { get; }

        public string FilePath { get; }

        public bool Exists => File.Exists(FilePath);

        public string? Write(World world)
        {
            try
            {
                Directory.CreateDirectory(Folder);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return $"{Folder}: cannot create the save folder: {ex.Message}";
            }
            return SaveFile.Write(FilePath, world);
        }

        public WorldResult Read(GameContent content) => SaveFile.Read(FilePath, content.Content, content.CreateLevelGenerator());

        public static SaveSlot InPersistentData() => new SaveSlot(Path.Combine(Application.persistentDataPath, "saves"));
    }
}
