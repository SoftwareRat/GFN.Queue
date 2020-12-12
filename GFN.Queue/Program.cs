using Microsoft.Extensions.Configuration;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace GFN.Queue
{
    class Program
    {
        private const string TEXT_BEFORE_QUEUE_NUMBER = "queue: ";
        private static IConfigurationRoot config;

        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true);

            config = builder.Build();

            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromSeconds(7);

            var timer = new Timer((e) =>
            {
                Refresh();
            }, null, startTimeSpan, periodTimeSpan);
            Console.Read();
        }

        private static void Refresh()
        {
            int currentQueueNumber = -1;
            var startDate = DateTime.Now;

            var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\NVIDIA Corporation\\GeForceNOW\\debug.log";
;

            List<string> debugFileLines = new List<string>();

            var queueDatas = new List<QueueData>();

            using (var fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fileStream))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    debugFileLines.Add(line);
                }
            }

            foreach (var line in debugFileLines)
            {
                var queueData = ParseLine(line);
                if(queueData is null)
                {
                    continue;
                }
                queueDatas.Add(queueData);
               currentQueueNumber = queueData.QueueNumber;
            }
            var queueHandler = new QueueHandler(queueDatas);
            AnsiConsole.Console.Clear(true);
            var table = new Table();
            // Add some columns
            table.AddColumn("Position in queue");
            table.AddColumn("StartTime");
            table.AddColumn("Time Awaited");
            table.AddColumn("Time by queue increment");
            table.AddColumn("Estimation");
            table.AddRow(
                queueHandler.CurrentQueuePosition.ToString(), 
                queueHandler.StartTime.ToString(),
                queueHandler.TimeAwaited.ToString(),
                queueHandler.TimeByQueueIncrement.ToString(),
                queueHandler.Estimation.ToString());
            AnsiConsole.Render(table);
        }

        private static QueueData ParseLine(string line)
        {
            if (!line.Contains(TEXT_BEFORE_QUEUE_NUMBER))
            {
                return null;
            }

            int f = line.IndexOf("INFO:");
            var dateString = line.Substring(f - 13, 12);
            var date = DateTime.Parse(dateString);

            int pFrom = line.IndexOf(TEXT_BEFORE_QUEUE_NUMBER) + TEXT_BEFORE_QUEUE_NUMBER.Length;

            int pTo = line.LastIndexOf(",");

            var result = line.Substring(pFrom, pTo - pFrom);

            var queueNumber = int.Parse(result);

            return new QueueData(queueNumber, date);
        }
    }

    public class QueueHandler
    {
        public List<QueueData> QueueDatas { get; set; }

        public int StartQueuePosition => QueueDatas.First().QueueNumber;
        public int CurrentQueuePosition => QueueDatas.Last().QueueNumber;
        public DateTime StartTime => QueueDatas.First().Date;
        public TimeSpan TimeAwaited => DateTime.Now.Subtract(StartTime);
        public TimeSpan TimeByQueueIncrement => TimeAwaited / (StartQueuePosition - CurrentQueuePosition);
        public TimeSpan Estimation => TimeByQueueIncrement * CurrentQueuePosition;
        public QueueHandler(List<QueueData> queueDatas)
        {
            QueueDatas = queueDatas;
        }
    }
    public class QueueData
    {
        public int QueueNumber { get; set; }
        public DateTime Date { get; set; }

        public QueueData(int queueNumber, DateTime date)
        {
            QueueNumber = queueNumber;
            Date = date;
        }
    }
}
