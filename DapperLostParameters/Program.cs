using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DapperLostParameters
{
    class Program
    {
        private static bool _run = true;
        private static CancellationToken _cancellationToken;
        private static CancellationTokenSource _cancellationTokenSource;
        private static readonly List<Worker> _tasks = new List<Worker>();

        static void Main(string[] args)
        {
            TaskScheduler.UnobservedTaskException += Log;
            Console.WriteLine($"Starting workers...");

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
            for (int i = 0; i < 200; i++)
            {
                StartNew();
                Console.Write($"\b\b\b\b{i}");
            }
            Console.Write($"\b\b\b\b");
            Console.WriteLine($"Running {_tasks.Count} workers\n");
            Console.WriteLine($"Type number to add workers\nType \"q\" to stop program");
            while (_run)
            {
                var input = Console.ReadLine();
                if (Array.IndexOf(new[] { "stop", "exit", "quit", "q" }, input.ToLowerInvariant()) >= 0) Stop();
                int add = 0;
                if (int.TryParse(input, out add))
                {
                    Console.WriteLine($"Adding... ");
                    for (int i = 0; i < add; i++)
                    {
                        StartNew();
                        Console.Write($"\b\b\b\b{i}");
                    }
                    Console.Write($"\b\b\b\b");
                    Console.WriteLine($"{add} workers added");
                    Console.WriteLine($"Running {_tasks.Count} workers");
                }
            }
        }

        private static void Stop()
        {
            Console.WriteLine($"Waiting {_tasks.Count} workers, stopping...");
            _cancellationTokenSource.Cancel();
            Task.WaitAll(_tasks.Select(x => x.TaskCompletionSource.Task).OfType<Task>().ToArray());
            _run = false;
        }

        private static void StartNew()
        {
            var t = new Worker();
            _tasks.Add(t);
            Task.Factory.StartNew(async () => await t.Run(_cancellationToken), _cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        private static void Log(object sender, UnobservedTaskExceptionEventArgs unobservedTaskExceptionEventArgs)
        {
            foreach (var e in unobservedTaskExceptionEventArgs.Exception.Flatten().InnerExceptions)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(e);
            }
        }
    }
}
