﻿using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using log4net.Core;

namespace UpdateDB
{
    /// <summary>
    /// Updates the item database. This includes affixes, base items, gems, item images and tree assets.
    /// What is updated and where it is saved can be set through arguments, see the console output below.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For updating the lists that are version controlled (affixes, base items and gems):
    /// <code>UpdateDB /VersionControlledOnly /SourceCodeDir</code>
    /// If you only want to update some of these three lists, you can specify them explicitly
    /// (skip the arguments that you don't want):
    /// <code>UpdateDB /Affixes /Items /Gems /SourceCodeDir</code>
    /// </para>
    /// <para>
    /// The other files (base item images and skill tree assets) are not version controlled. They are
    /// packaged into releases via release.xml.
    /// The skill tree assets can be updated through the main program menu. The base item images
    /// must be manually copied from this project's execution directory (normally UpdateDB/bin/debug/Data/Equipment/Assets)
    /// into the execution directory of the main program (normally WPFSkillTree/bin/debug/[...]). To generate them,
    /// use <code>UpdateDB /Images</code>.
    /// </para>
    /// </remarks>
    public static class Program
    {
        // Main entry point.
        public static int Main(string[] arguments)
        {
            var args = new Arguments
            {
                CreateBackup = true,
                ActivatedLoaders = LoaderCategories.Any,
                LoaderFlags = new List<string>()
            };

            // Get options.
            var unrecognizedSwitches = new List<string>();
            foreach (var arg in arguments)
            {
                if (!arg.StartsWith("/"))
                    continue;

                switch (arg.ToLowerInvariant())
                {
                    case "/?":
                        Console.WriteLine("Updates item database.\r\n");
                        Console.WriteLine("Flags:\r\n");
                        Console.WriteLine("/VersionControlledOnly    Only download version controlled files (gem, affix and base item lists).");
                        Console.WriteLine("/NotVersionControlledOnly Only download not version controlled files (item images and skill tree assets).");
                        Console.WriteLine("/SourceCodeDir            Save into the WPFSKillTree source code directory instead of the AppData directory.");
                        Console.WriteLine("/CurrentDir               Save into the current directory instead of the AppData directory.");
                        Console.WriteLine("/NoBackup                 Do not create backup of files being updated before writing changes.");
                        Console.WriteLine("/Quiet                    Do not display any output.");
                        Console.WriteLine("/Verbose                  Enable verbose output.");
                        Console.WriteLine("/Affixes, /Items, /Images, /TreeAssets, /Gems");
                        Console.WriteLine("If at least one is specified, only the specified files are downloaded.");
                        return 1;

                    case "/versioncontrolledonly":
                        args.ActivatedLoaders = LoaderCategories.VersionControlled;
                        break;
                    case "/notversioncontrolledonly":
                        args.ActivatedLoaders = LoaderCategories.NotVersionControlled;
                        break;

                    case "/sourcecodedir":
                        args.OutputDirectory = OutputDirectory.SourceCode;
                        break;
                    case "/currentdir":
                        args.OutputDirectory = OutputDirectory.Current;
                        break;

                    case "/nobackup":
                        args.CreateBackup = false;
                        break;

                    case "/quiet":
                        var repo = (log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository();
                        repo.Root.Level = Level.Off;
                        repo.RaiseConfigurationChanged(EventArgs.Empty);
                        break;
                    case "/verbose":
                        repo = (log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository();
                        repo.Root.Level = Level.Debug;
                        repo.RaiseConfigurationChanged(EventArgs.Empty);
                        break;

                    default:
                        unrecognizedSwitches.Add(arg.Substring(1));
                        break;
                }
            }

            var nonFlags = unrecognizedSwitches.Where(s => !DataLoaderExecutor.IsLoaderFlagRecognized(s)).ToList();
            if (nonFlags.Any())
            {
                Console.WriteLine("Invalid switches - \"" + string.Join("\", \"", nonFlags) + "\"");
                return 1;
            }
            if (unrecognizedSwitches.Any())
            {
                args.ActivatedLoaders = LoaderCategories.None;
                args.LoaderFlags = unrecognizedSwitches;
            }


            var exec = new DataLoaderExecutor(args);
            exec.LoadAllAsync().Wait();

            return 0;
        }


        private class Arguments : IArguments
        {
            public LoaderCategories ActivatedLoaders { get; set; }
            public OutputDirectory OutputDirectory { get; set; }
            public bool CreateBackup { get; set; }
            public IEnumerable<string> LoaderFlags { get; set; }
        }
    }
}
