using ComputerUtils.ADB;
using ComputerUtils.ConsoleUi;
using ComputerUtils.FileManaging;
using ComputerUtils.Logging;
using QuestPatcher.Axml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.Json;

namespace LegacyQuestBSModdingTool
{
    // Some of the code is by Lauries QuestPatcher (https://github.com/Lauriethefish/QuestPatcher)
    public class ModdingManager
    {
        public ADBInteractor interactor = new ADBInteractor();
        public static string apkToolPath = PublicVars.exe + "tools\\apktool.jar";
        public static string apkSignerPath = PublicVars.exe + "tools\\uber-apk-signer.jar";

        public static string libMain32Path = PublicVars.exe + "tools\\libmain32.so";
        public static string libMain64Path = PublicVars.exe + "tools\\libmain64.so";
        public static string libModloader32Path = PublicVars.exe + "tools\\libmodloader32.so";
        public static string libModloader64Path = PublicVars.exe + "tools\\libmodloader64.so";

        public void StartModding()
        {
            Logger.Log("Starting modding of APK");
            Logger.Log("Checking if required Tools Exist");
            //DownloadFileIfMissing(apkToolPath, "https://bitbucket.org/iBotPeaches/apktool/downloads/apktool_2.5.0.jar");
            DownloadFileIfMissing(apkSignerPath, "https://github.com/patrickfav/uber-apk-signer/releases/download/v1.2.1/uber-apk-signer-1.2.1.jar");

            DownloadFileIfMissing(libMain32Path, "https://github.com/sc2ad/QuestLoader/releases/download/v1.1.1/libmain32.so");
            DownloadFileIfMissing(libMain64Path, "https://github.com/sc2ad/QuestLoader/releases/download/v1.1.1/libmain64.so");
            DownloadFileIfMissing(libModloader32Path, "https://github.com/sc2ad/QuestLoader/releases/download/v1.1.1/libmodloader32.so");
            DownloadFileIfMissing(libModloader64Path, "https://github.com/sc2ad/QuestLoader/releases/download/v1.1.1/libmodloader64.so");
            Console.ForegroundColor = ConsoleColor.White;
            UndefinedEndProgressBar undefinedEndProgressBar = new UndefinedEndProgressBar();
            string apkPath = PublicVars.exe + "tmpBSApk.apk";

            apkPath = ConsoleUiController.QuestionString("BS APK Path: ").Replace("\"", "");
            undefinedEndProgressBar.Start();
            undefinedEndProgressBar.SetupSpinningWheel(500);
            FileManager.RecreateDirectoryIfExisting(PublicVars.exe + "tmp");
            undefinedEndProgressBar.UpdateProgress("Pulling APK");
            /*
            interactor.adb("pull " + interactor.adbS("shell pm path com.beatgames.beatsaber").Replace("package:", "") + " \"" + apkPath + "\"");
            if(!File.Exists(apkPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                undefinedEndProgressBar.StopSpinningWheel();
                Logger.Log("APK was unable to get pulled. See above for more info.", LoggingType.Warning);
                Console.WriteLine("APK was unable to get pulled. Aborting.");
                return;
            }
            */


            Logger.Log("Checking if apk is modded");
            undefinedEndProgressBar.UpdateProgress("Checking if APK is modded", true);
            ZipArchive apkArchive = ZipFile.Open(apkPath, ZipArchiveMode.Update);
            if(apkArchive.GetEntry("modded") != null || apkArchive.GetEntry("BMBF.modded") != null)
            {
                Logger.Log("APK is already modded");
                Console.ForegroundColor = ConsoleColor.Green;
                undefinedEndProgressBar.StopSpinningWheel();
                Console.WriteLine("APK is already modded. No need to mod");
                return;
            }
            undefinedEndProgressBar.UpdateProgress("Getting APK Version", true);
            MemoryStream manifestStream = new MemoryStream();
            apkArchive.GetEntry("AndroidManifest.xml").Open().CopyTo(manifestStream);
            manifestStream.Position = 0;
            AxmlElement manifest = AxmlLoader.LoadDocument(manifestStream);
            string versionName = "";
            foreach(AxmlAttribute a in manifest.Attributes)
            {
                if(a.Name == "versionName")
                {
                    Console.WriteLine("\nAPK Version is " + a.Value);
                    Logger.Log("Apk version is " + a.Value);
                    versionName = a.Value.ToString();
                }
            }
            bool isApk64Bit = apkArchive.GetEntry("lib/arm64-v8a/libil2cpp.so") != null;
            undefinedEndProgressBar.UpdateProgress("Adding unstripped libunity to APK if available");
            string libpath = isApk64Bit ? "lib/arm64-v8a/" : "lib/armeabi-v7a/";
            if (AttemptDownloadUnstrippedUnity(versionName))
            {
                Logger.Log("Adding libunity.so to " + (apkArchive.GetEntry("lib/arm64-v8a/libil2cpp.so") != null ? "lib/arm64-v8a/libunity.so" : "lib/armeabi-v7a/libunity.so"));
                
                ZipArchiveEntry unity = apkArchive.GetEntry(libpath + "libunity.so");
                if (unity != null) unity.Delete();
                apkArchive.CreateEntryFromFile(PublicVars.exe + "tmp\\libunity.so", libpath + "libunity.so");
            }

            undefinedEndProgressBar.UpdateProgress("Adding modloader", true);
            apkArchive.CreateEntryFromFile(isApk64Bit ? libModloader64Path : libModloader32Path, libpath + "libmodloader.so");

            undefinedEndProgressBar.UpdateProgress("Adding libmain", true);
            ZipArchiveEntry main = apkArchive.GetEntry(libpath + "libmain.so");
            if (main != null) main.Delete();
            apkArchive.CreateEntryFromFile(isApk64Bit ? libMain64Path : libMain32Path, libpath + "libmain.so");
            apkArchive.CreateEntry("modded");

            undefinedEndProgressBar.UpdateProgress("Saving", true);
            apkArchive.Dispose();

            undefinedEndProgressBar.StopSpinningWheel();

            Console.WriteLine("Modding should be finished now. Please install the apk manually as I want to game now so I didn't add automatic install. It's named tmpBSApk.apk");
        }

        // Uses https://github.com/Lauriethefish/QuestUnstrippedUnity to download an appropriate unstripped libunity.so for beat saber if there is one
        private bool AttemptDownloadUnstrippedUnity(string version)
        {
            Logger.Log("Checking index for unstrippedUnity");
            WebClient c = new WebClient();
            string libUnityIndexString = c.DownloadString("https://raw.githubusercontent.com/Lauriethefish/QuestUnstrippedUnity/main/index.json");
            Dictionary<string, Dictionary<string, string>> index = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(libUnityIndexString);
            string appId = "com.beatgames.beatsaber";
            if (index.ContainsKey(appId))
            {
                if(index[appId].ContainsKey(version))
                {
                    DownloadProgressUI downloadProgressUI = new DownloadProgressUI();
                    Console.WriteLine();
                    return downloadProgressUI.StartDownload("https://raw.githubusercontent.com/Lauriethefish/QuestUnstrippedUnity/main/versions/" + index[appId][version] + ".so", PublicVars.exe + "tmp\\libunity.so");
                } else
                {
                    Logger.Log("No unstripped libunity found. It does exist for another version of the app");
                }
            } else
            {
                Logger.Log("No unstripped libunity found.", LoggingType.Warning);
            }
            return false;
        }

        public void StartJar(string file, string args)
        {
            Logger.Log("Starting " + file + " with args " + args);
            Process p = Process.Start("java.exe", "-jar " + file + " " + args);
            p.WaitForExit();
            Logger.Log("Process finished");
        }

        public void DownloadFileIfMissing(string filePath, string downloadLink)
        {
            if(!File.Exists(filePath))
            {
                string fileName = Path.GetFileName(filePath);
                Logger.Log(fileName + " doesn't exist. Downloading");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(fileName + " hasn't been downloaded yet. It'll be downloaded");
                DownloadProgressUI downloadProgressUI = new DownloadProgressUI();
                bool success = downloadProgressUI.StartDownload(downloadLink, filePath);
                Console.WriteLine();
            } else
            {
                Logger.Log(Path.GetFileName(filePath) + " exists. Not downloading");
            }
        }
    }
}