using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using eda.tool.Actions;
using eda.Terminal;
using Serilog;

namespace eda.tool {

	class Program {
		static void Main(string[] args) {
			if (args.Length == 0) args = new[] {"engine"};

			Log.Logger = new LoggerConfiguration()
				.WriteTo.ColoredConsole()
				.CreateLogger();

			AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) => {
				Console.WriteLine("Error " + eventArgs.ExceptionObject);
			};

			var verb = args[0];
			args = args.Skip(1).ToArray();

			var exportedTypes = GetExportedTypes();
			var instances = exportedTypes
				.Where(t => typeof(ConsoleSyntax).IsAssignableFrom(t))
				.Where(t => !t.IsAbstract)
				.Select(t => t.GetConstructor(Type.EmptyTypes).Invoke(null))
				.Cast<ConsoleSyntax>()
				.ToList();


			
			var duplicates = instances.GroupBy(c => c.Action).Where(g => g.Count() > 2).ToList();
			if (duplicates.Any()) {
				foreach (var duplicate in duplicates) {
					Log.Fatal("Found duplicate action {action}", duplicate.Key);
				}
				Environment.Exit(1);
			}
			
			var dict = instances
				.ToDictionary(c => c.Action);


			ConsoleSyntax command;
			if (!dict.TryGetValue(verb, out command)) {
				Console.WriteLine("Unknown command '{0}'. Available commands:", verb);

				var len = dict.Keys.Max(k => k.Length);
				foreach (var pair in dict) {
					var prefix = new string(' ', len - pair.Key.Length);
					Console.WriteLine("  {0}: {1}", prefix + pair.Key, pair.Value.Description);
				}
				return;
			}

			var parser = new Parser(settings => settings.HelpWriter = Console.Out);
			var result = parser.ParseArguments<object>(() => command, args);

			result.WithParsed(o => {
				ConsoleUtil.PrintArguments(command);
				command.Run();
			});
		}

		static IEnumerable<Type> GetExportedTypes() {
			var bases = new[] {
				typeof(ConsoleSyntax),
				typeof(DownloadMessageVaultIntoCellar)
			};
			return bases.Select(t => t.Assembly).Distinct().SelectMany(a => a.GetExportedTypes());
		}
	}

}