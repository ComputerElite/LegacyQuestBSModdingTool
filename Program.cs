using ComputerUtils.ConsoleUi;
using ComputerUtils.FileManaging;
using ComputerUtils.Logging;
using ComputerUtils.Updating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegacyQuestBSModdingTool
{
    class Program
    {
        public static Updater updater = new Updater("0.1", "N/A", "Legacy Quest Beat Saber Modding Tool");
        static void Main(string[] args)
        {
            Logger.SetLogFile(AppDomain.CurrentDomain.BaseDirectory + "log.log");
            Logger.LogRaw("\n\n");
            Logger.Log("Starting " + updater.AppName + " version " + updater.version);
            SetupExceptionHandlers();
            if (args.Length == 1 && args[0] == "--update")
            {
                Logger.Log("Starting in update mode");
                updater.Update();
                return;
            }
            Console.WriteLine("Welcome to Legacy Quest Beat Saber Modding Tool. Navigate the menu by typing the corresponding number to the choice you want to do and answering the asked Question. If you get stuck in a loop simply exit the Program and inform ComputerElite.");
            ModdingMenu m = new ModdingMenu();
            m.Start();
        }

        public static void SetupExceptionHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            HandleExtenption((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                HandleExtenption(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
            };
        }

        public static void HandleExtenption(Exception e, string source)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Logger.Log("An unhandled exception has occured:\n" + e.ToString(), LoggingType.Crash);
            Console.WriteLine("\n\nAn unhandled exception has occured. Check the log for more info and send it to ComputerElite for the (probably) bug to get fix. Press any key to close out.");
            Console.ReadKey();
            Logger.Log("Exiting cause of unhandled exception.");
            Environment.Exit(0);
        }
    }

    public class ModdingMenu
    {
        public ModdingManager moddingManager = new ModdingManager();
        public void Start()
        {
            SetupProgram();
            while(true)
            {
                Console.WriteLine();
                Console.WriteLine();
                switch (ConsoleUiController.ShowMenu(new string[] { "Mod APK", "Install Songs", "Install Mods", "Exit" }))
                {
                    case "1":
                        moddingManager.StartModding();
                        break;
                    case "2":
                        Console.WriteLine("Idiot");
                        break;
                    case "3":
                        Console.WriteLine("I hate you");
                        break;
                    case "4":
                        Logger.Log("Exiting");
                        System.Environment.Exit(0);
                        break;
                }
            }
        }

        public void SetupProgram()
        {
            if(Program.updater.CheckUpdate())
            {
                Logger.Log("Update available. Asking user if they want to update");
                string choice = ConsoleUiController.QuestionString("Do you want to update? (Y/n): ");
                if (choice.ToLower() == "y" || choice == "")
                {
                    Program.updater.StartUpdate();
                }
                Logger.Log("Not updating.");
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Creating required directories if not existing");
            FileManager.CreateDirectoryIfNotExisting(PublicVars.exe + "tools");
        }
    }

    public class PublicVars
    {
        public static string exe = AppDomain.CurrentDomain.BaseDirectory;
    }
}
