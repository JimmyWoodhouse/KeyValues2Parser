namespace KeyValues2Parser.Models
{
	public static class GameConfigurationValues
    {
        public static string gameFolderPath;
        public static string gameCsgoFolderPath;
        public static string binFolderPath;

        public static string vmapFilepath;
        public static string vmapName;
        public static string vmapFilepathDirectory;

        public static readonly Dictionary<List<string>, int> allArgumentNamesAndNumOfFollowingInputs = new()
        {
            { new() { "-g", "-game" }, 2 },
            { new() { "-vmapFilepath", "-mapFilepath" }, 2 },
        };

        public static readonly Dictionary<int, string> allArgumentGroupDescriptions = new()
        {
            { 1, @"Location of the '...\Counter-Strike Global Offensive\game\csgo\' folder. (optional)" },
            { 2, @"VMap name inside an addon's '...\Counter-Strike Global Offensive\content\csgo_addons\maps\' folder. Including '.vmap' at the end does not matter." },
        };

        private static readonly int maxNumOfDiffArgs = allArgumentNamesAndNumOfFollowingInputs.Count;
        private static readonly int maxNumOfArgInputs = allArgumentNamesAndNumOfFollowingInputs.Values.Sum();


        public static bool SetArgs(string[] args)
        {
            Console.WriteLine("---- Arguments ----");
            Console.WriteLine("Num of arguments provided: " + args.Length);

            for (int i = 0; i < args.Length; i++)
            {
                if (allArgumentNamesAndNumOfFollowingInputs.Keys.Any(x => x.Any(y => y.ToLower() == args[i].ToLower())))
                {
                    var numOfArgInputs = allArgumentNamesAndNumOfFollowingInputs.First(x => x.Key.Any(y => y.ToLower() == args[i].ToLower())).Value;

                    Console.Write(string.Concat("** Argument: ", args[i]));
                    for (int j = i+1; j < i + numOfArgInputs; j++)
                    {
                        Console.Write(string.Concat(" ", args[j]));
                    }
                    Console.WriteLine();

                    i += (numOfArgInputs - 1);
                }
            }

            if (args.Length == 0)
            {
                PrintHelpLinesAndExit();
                return false;
            }

            if (args.Length > maxNumOfArgInputs)
                return false;

            if (args.Any(x => x.ToLower() == "-g") && args.Any(x => x.ToLower() == "-game"))
            {
                Console.WriteLine("Don't provide both \"-g\" and \"-game\", they are the same argument. Remove one of them.");
                return false;
            }

            if (args.Any(x => x.ToLower() == "-vmapFilepath") && args.Any(x => x.ToLower() == "-mapFilepath"))
            {
                Console.WriteLine("Don't provide both \"-vmapFilepath\" and \"-mapFilepath\", they are the same argument. Remove one of them.");
                return false;
            }

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "-h":
                    case "-help":
                        PrintHelpLinesAndExit();
                        return false;
                    case "-g":
                    case "-game":
                        if (i > 0)
                        {
                            Console.WriteLine("-g or -game parameters MUST come first in the list, if they are provided. Aborting.");
                            return false;
                        }
                        if (Path.IsPathRooted(args[i + 1]))
                            gameCsgoFolderPath = args[i + 1]; // overrides the default
                        if (string.IsNullOrWhiteSpace(gameCsgoFolderPath))
                        {
                            Console.WriteLine("gameCsgoFolderPath is null. Check what the -g or -game parameters are set to, or remove them if unnecessary.");
                            return false;
                        }
                        if (gameCsgoFolderPath.ToCharArray().LastOrDefault() != '\\')
                            gameCsgoFolderPath += '\\';
                        i++;
                        break;
                    case "-vmapFilepath":
                    case "-mapFilepath":
                        vmapName = args[i + 1].Replace(".vmap", string.Empty);
                        if (string.IsNullOrWhiteSpace(vmapName))
                        {
                            Console.WriteLine("gameCsgoFolderPath is null. Check what the -vmapName or -mapName parameters are set to, or remove them if unnecessary.");
                            return false;
                        }
                        i++;
                        break;
                    default:
                        Console.WriteLine($"Unexpected parameter given: {args[i]}. Aborting.");
                        return false;
                }
            }

            if (string.IsNullOrWhiteSpace(gameCsgoFolderPath))
            {
                Console.WriteLine("gameCsgoFolderPath has no value. Aborting.");
                return false;
            }

            // sets vmapFilepath
            vmapName = Path.GetFileNameWithoutExtension(vmapFilepath);
            if (string.IsNullOrWhiteSpace(vmapFilepath))
            {
                Console.WriteLine("vmapFilepath is null. Check what the -vmapFilepath parameter is set to.");
                return false;
            }
            if (vmapFilepath.EndsWith(".vmap.txt"))
                vmapFilepath = vmapFilepath.Replace(".vmap.txt", ".vmap");
            else if (!vmapFilepath.EndsWith(".vmap"))
                vmapFilepath += ".vmap";
            vmapFilepathDirectory = Path.GetDirectoryName(vmapFilepath) + @"\";
            //


            if (string.IsNullOrWhiteSpace(vmapFilepathDirectory))
            {
                Console.WriteLine("vmapFilepathDirectory has no value. Aborting.");
                return false;
            }


            // set the rest of the filepaths and folderpaths
            gameFolderPath = Directory.GetParent(gameCsgoFolderPath).Parent.FullName + @"\";

            binFolderPath = Path.Join(gameFolderPath, @"bin\");


            PrintAllValues();

            return true;
        }


        private static void PrintHelpLinesAndExit()
        {
            List<string> lines = new()
            {
                "All arguments:"
            };

            for (int i = 0; i < allArgumentNamesAndNumOfFollowingInputs.Count; i++)
            {
                var argumentGroup = allArgumentNamesAndNumOfFollowingInputs.ElementAt(i).Key;

                foreach (var arg in argumentGroup)
                {
                    lines.Add("\t" + arg);
                }

                lines.Add("\tDesc: " + allArgumentGroupDescriptions[i+1]);
                lines.Add("---------------------------------------------------------------------");
            }

            foreach (var line in lines)
            {
                Console.WriteLine(line);
            }
        }


        public static void PrintAllValues()
        {
            Console.WriteLine();
            Console.WriteLine("---- Game Configuration Values ----");
            Console.WriteLine("game csgo Directory: ");
            Console.WriteLine(gameCsgoFolderPath);
            Console.WriteLine("bin Directory: ");
            Console.WriteLine(binFolderPath);
            Console.WriteLine("vmap Filepath: ");
            Console.WriteLine(vmapFilepath);
            Console.WriteLine("vmap Name: ");
            Console.WriteLine(vmapName);
            Console.WriteLine("vmap Directory: ");
            Console.WriteLine(vmapFilepathDirectory);
        }


        public static bool VerifyAllValuesSet() // ignores isVanillaHammer
        {
            if (string.IsNullOrWhiteSpace(gameFolderPath) ||
                string.IsNullOrWhiteSpace(gameCsgoFolderPath) ||
                string.IsNullOrWhiteSpace(binFolderPath) ||
                string.IsNullOrWhiteSpace(vmapFilepath) ||
                string.IsNullOrWhiteSpace(vmapName) ||
                string.IsNullOrWhiteSpace(vmapFilepathDirectory)
            )
            {
                return false;
            }

            return true;
        }
    }
}
