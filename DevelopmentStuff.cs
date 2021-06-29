using ComputerUtils.ConsoleUi;
using ComputerUtils.FileManaging;
using ComputerUtils.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;

namespace LegacyQuestBSModdingTool
{
    public class DevelopingStuff
    {
        public void ShowMenu()
        {
            switch (ConsoleUiController.ShowMenu(new string[] { "Get Versions of all APKs in folder", "Get Versions of all APKs in folder and add to index.json of UnstippedUnity" }))
            {
                case "1":
                    GetAppUnityVersions();
                    break;
                case "2":
                    GetAppUnityVersions(true);
                    break;
            }
        }

        public void GetAppUnityVersions(bool addToIndexJson = false)
        {
            Console.WriteLine();
            string folder = ConsoleUiController.QuestionString("APK Folder: ");
            Logger.Log("Getting all unity versions from " + folder);
            UndefinedEndProgressBar undefinedEndProgressBar = new UndefinedEndProgressBar();
            undefinedEndProgressBar.Start();
            undefinedEndProgressBar.SetupSpinningWheel(500);
            FileManager.RecreateDirectoryIfExisting(PublicVars.exe + "tmp");
            string end = "\n";
            string appid = "";
            Dictionary<string, SortedDictionary<string, string>> index = new Dictionary<string, SortedDictionary<string, string>>();
            if (addToIndexJson)
            {
                WebClient c = new WebClient();
                string libUnityIndexString = c.DownloadString("https://raw.githubusercontent.com/Lauriethefish/QuestUnstrippedUnity/main/index.json");
                index = JsonSerializer.Deserialize<Dictionary<string, SortedDictionary<string, string>>>(libUnityIndexString);
                appid = ConsoleUiController.QuestionString("AppId (com.beatgames.beatsaber)");
                if (appid == "") appid = "com.beatgames.beatsaber";
            }
            foreach(string f in Directory.GetFiles(folder))
            {
                if (f.EndsWith(".apk"))
                {
                    Logger.Log("Processing " + f);
                    undefinedEndProgressBar.UpdateProgress("Processing " + f);
                    ZipArchive apk = ZipFile.OpenRead(f);
                    MemoryStream ms = new MemoryStream();
                    apk.GetEntry("assets/bin/Data/0000000000000000f000000000000000").Open().CopyTo(ms);
                    bool is64Bit = apk.GetEntry("lib/arm64-v8a/libil2cpp.so") != null;
                    ms.Position = 20;
                    byte[] bytes = new byte[20];
                    ms.Read(bytes, 0, bytes.Length);
                    string version = Encoding.UTF8.GetString(bytes);
                    version = version.Substring(0, version.IndexOf("f") + 2);
                    Logger.Log("Unity version is " + version);
                    string AppVersion = ModdingManager.GetAPKVersionString(apk);
                    if (addToIndexJson && !index[appid].ContainsKey(AppVersion)) index[appid].Add(AppVersion, version + "_" + (is64Bit ? "64" : "32"));
                    end += (AppVersion + ": ").PadRight(18) + version.PadRight(15) + "Is 64 Bit? " + is64Bit + "\n";
                }
            }
            undefinedEndProgressBar.StopSpinningWheel();
            Console.WriteLine(end);
            if (addToIndexJson)
            {
                Console.WriteLine(JsonSerializer.Serialize(index, new JsonSerializerOptions { WriteIndented = true }));
            }
        }
    }
}