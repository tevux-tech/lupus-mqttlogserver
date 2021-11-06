using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Lupus {
    class Program {
        private static Logger _lupusLog = LogManager.GetCurrentClassLogger();
        private static Dictionary<string, Logger> _allTheLoggers = new Dictionary<string, Logger>();

        static void Main(string[] args) {
            // Configuring NLog.
            var config = new LoggingConfiguration();

            Helpers.AddFileOutputToLogger(config);

            var logconsole = new ColoredConsoleTarget("console");
            logconsole.Layout = Helpers.LocalNlogLayout;
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);

            var logdebug = new DebuggerTarget("debugger");
            logdebug.Layout = Helpers.LocalNlogLayout;
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logdebug, "Lupus.*");

            LogManager.Configuration = config;

            if (Directory.Exists("logs") == false) {
                Directory.CreateDirectory("logs");
            }

            _lupusLog.Info("Loading ENV variables...");
            var brokerIp = Environment.GetEnvironmentVariable("MQTT_BROKER_IP");
            if (string.IsNullOrEmpty(brokerIp)) {
                _lupusLog.Warn("Evironment variable \"MQTT_BROKER_IP\" is not provided. Using 127.0.0.1.");
                brokerIp = "127.0.0.1";
            }

            _lupusLog.Info($"Setting up MQTT broker connection to {brokerIp}...");
            var channelOptions = new Tevux.Protocols.Mqtt.ChannelConnectionOptions();
            channelOptions.SetHostname(brokerIp);

            var logConnection = new Tevux.Protocols.Mqtt.MqttClient();
            logConnection.Initialize();
            logConnection.PublishReceived += (sender, e) => {
                var topicParts = e.Topic.Split('/');
                var logger = topicParts[2];
                var level = topicParts[3];
                var contentToLog = Encoding.UTF8.GetString(e.Message);

                if (_allTheLoggers.ContainsKey(logger) == false) {
                    _allTheLoggers.Add(logger, LogManager.GetLogger(logger));
                }
                var pickedLogger = _allTheLoggers[logger];

                switch (level.ToLower()) {
                    case "trace":
                        pickedLogger.Trace(contentToLog);
                        break;

                    case "info":
                        pickedLogger.Info(contentToLog);
                        break;

                    case "warn":
                        pickedLogger.Warn(contentToLog);
                        break;

                    case "error":
                        pickedLogger.Error(contentToLog);
                        break;

                    case "fatal":
                        pickedLogger.Fatal(contentToLog);
                        break;
                }
            };

            logConnection.ConnectAndWait(channelOptions);
            logConnection.Subscribe("tevux/logs/#", Tevux.Protocols.Mqtt.QosLevel.AtLeastOnce);

            _lupusLog.Info("Initialization completed.");
            Thread.Sleep(-1);
        }
    }
}
