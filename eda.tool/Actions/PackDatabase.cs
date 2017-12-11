using System;
using System.IO;
using System.IO.Compression;
using CommandLine;
using eda.Lightning;
using eda.Terminal;

namespace eda.tool.Actions {

	public sealed class PackDatabase : ConsoleSyntax {
		public PackDatabase() {
			Action = "pack";
			Description = "Packs the database";
		}

		//[Option("db-path", HelpText = "Database folder", Required = true)]
		[Value(0, HelpText = "Database path", Required = true)]
		public string DatabasePath { get; set; }

		public override void Run() {
			if (!Directory.Exists(DatabasePath)) {
				Console.WriteLine("Path not found {0}", DatabasePath);
				Environment.Exit(1);
			}
			var outPath = DatabasePath.TrimEnd('/') + ".pack";
			if (Directory.Exists(outPath)) Directory.Delete(outPath, true);
			Directory.CreateDirectory(outPath);


			using (var db = LmdbEnv.CreateDb(DatabasePath, 0, EnvironmentOpenFlags.ReadOnly)) {
				using (var tx = db.Read()) {
					var stats = tx.GetUsedSize();

					Console.WriteLine("Entry count: {0}. Size: {1}", stats.EntryCount,
						Print.Bytes(stats.UsedBytes));
				}

				db.CopyTo(outPath, true);
			}

			var inFile = Path.Combine(outPath, "data.mdb");
			Console.WriteLine("Packed into {0}", Print.Bytes(new FileInfo(inFile).Length));

			var outFile = Path.Combine(outPath, "data.mdb.gzip");
			using (var input = File.OpenRead(inFile))
			using (var output = File.Create(outFile))
			using (var gz = new GZipStream(output, CompressionLevel.Optimal)) {
				input.CopyTo(gz);
			}


			Console.WriteLine("Gzipped into {0}", Print.Bytes(new FileInfo(outFile).Length));
		}
	}

}