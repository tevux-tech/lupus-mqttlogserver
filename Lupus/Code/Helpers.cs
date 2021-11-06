﻿using System;
using System.Threading;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Lupus {
    public class Helpers {
        private static Logger _log = LogManager.GetCurrentClassLogger();
        public static NLog.Layouts.Layout LocalNlogLayout = "${longdate}|${uppercase:${level}}|${logger}|${message} ${onexception}";

        public static string LoadEnvOrDie(string envVariable, string defaultValue = "") {
            if (string.IsNullOrEmpty(envVariable)) { throw new ArgumentException("Requested ENV variable must be an non-empty string.", nameof(envVariable)); }

            var loadedVariable = Environment.GetEnvironmentVariable(envVariable);
            if (string.IsNullOrEmpty(envVariable) && string.IsNullOrEmpty(defaultValue)) {
                _log.Fatal($"Evironment variable {envVariable} is not in the environment. Application will exit after a few seconds.");
                Thread.Sleep(20000); // <-- Preventing restart loops in docker containers, so the user at least could see the error messages.
                Environment.Exit(-1);
            }
            else if (string.IsNullOrEmpty(envVariable)) {
                _log.Warn($"Evironment variable {envVariable} is not provided. Using default value {defaultValue}.");
                loadedVariable = defaultValue;
            }

            return loadedVariable;
        }

        public static void AddFileOutputToLogger(LoggingConfiguration configuration, string filename = "logs/ApplicationLog.txt") {
            var logfile = new FileTarget(filename);
            logfile.Layout = LocalNlogLayout;
            logfile.MaxArchiveFiles = 30;
            logfile.ArchiveDateFormat = "yyyy-MM";
            logfile.ArchiveNumbering = ArchiveNumberingMode.Date;
            logfile.ArchiveEvery = FileArchivePeriod.Month;
            logfile.FileName = filename;
            configuration.AddRule(LogLevel.Info, LogLevel.Fatal, logfile);
        }
    }
}
