using System;
using System.Text;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Lupus {
    class Program {
        private static Logger _log = LogManager.GetCurrentClassLogger();

        static void Main(string[] args) {
            NLog.Layouts.Layout localLayout = "${longdate}|${uppercase:${level}}|${message}";
            var config = new LoggingConfiguration();


            var brokerIp = Environment.GetEnvironmentVariable("MQTT_BROKER_IP");
            if (string.IsNullOrEmpty(brokerIp)) {
                _log.Warn("Evironment variable \"MQTT_BROKER_IP\" is not provided. Using 127.0.0.1.");
                brokerIp = "127.0.0.1";
            }

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


            var logconsole = new ColoredConsoleTarget("console");
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole, "Lupus.*");

            var logdebug = new DebuggerTarget("debugger");
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logdebug, "Lupus.*");

            var logfile = new FileTarget("file");
            logfile.Layout = localLayout;
            logfile.MaxArchiveFiles = 30;
            logfile.ArchiveDateFormat = "yyyy-MM";
            logfile.ArchiveNumbering = ArchiveNumberingMode.Date;
            logfile.ArchiveEvery = FileArchivePeriod.Month;
            logfile.FileName = "logs/ApplicationLog.txt";
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logfile, "Lupus.*");

            LogManager.Configuration = config;
        }
    }
}
