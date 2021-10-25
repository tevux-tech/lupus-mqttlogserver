using System;
using System.IO;
using System.Text;
using System.Threading;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Lupus {
    class Program {
        private static Logger _log = LogManager.GetCurrentClassLogger();

        static void Main(string[] args) {
            // Configuring NLog.
            NLog.Layouts.Layout localLayout = "${longdate}|${uppercase:${level}}|${message} ${onexception}";
            var config = new LoggingConfiguration();

            var logfile = new FileTarget("file");
            logfile.Layout = localLayout;
            logfile.MaxArchiveFiles = 30;
            logfile.ArchiveDateFormat = "yyyy-MM";
            logfile.ArchiveNumbering = ArchiveNumberingMode.Date;
            logfile.ArchiveEvery = FileArchivePeriod.Month;
            logfile.FileName = "logs/ApplicationLog.txt";
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logfile, "Lupus.*");

            var logconsole = new ColoredConsoleTarget("console");
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole, "Lupus.*");

            var logdebug = new DebuggerTarget("debugger");
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logdebug, "Lupus.*");

            LogManager.Configuration = config;

            if (Directory.Exists("logs") == false) {
                Directory.CreateDirectory("logs");
            }

            _log.Info("Loading ENV variables...");
            var brokerIp = Environment.GetEnvironmentVariable("MQTT_BROKER_IP");
            if (string.IsNullOrEmpty(brokerIp)) {
                _log.Warn("Evironment variable \"MQTT_BROKER_IP\" is not provided. Using 127.0.0.1.");
                brokerIp = "127.0.0.1";
            }

            _log.Info("Setting up MQTT broker connection...");
            var channelOptions = new Tevux.Protocols.Mqtt.ChannelConnectionOptions();
            channelOptions.SetHostname(brokerIp);

            var logConnection = new Tevux.Protocols.Mqtt.MqttClient();
            logConnection.Initialize();
            logConnection.PublishReceived += (sender, e) => {
                var topicParts = e.Topic.Split('/');
                var deviceName = topicParts[2];
                var level = topicParts[3];
                var message = Encoding.UTF8.GetString(e.Message);

                switch (level.ToLower()) {
                    case "trace":
                        _log.Trace(message);
                        break;

                    case "info":
                        _log.Info(message);
                        break;

                    case "warn":
                        _log.Warn(message);
                        break;

                    case "error":
                        _log.Error(message);
                        break;

                    case "fatal":
                        _log.Fatal(message);
                        break;
                }
            };

            logConnection.ConnectAndWait(channelOptions);
            logConnection.Subscribe("tevux/logs/#", Tevux.Protocols.Mqtt.QosLevel.AtLeastOnce);

            _log.Info("Initialization completed.");
            Thread.Sleep(-1);
        }
    }
}
