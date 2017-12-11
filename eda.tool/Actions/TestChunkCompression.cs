using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using CommandLine;
using eda.Terminal;
using LZ4;

namespace eda.tool.Actions {

	public sealed class TestChunkCompression : ConsoleSyntax {
		public TestChunkCompression() {
			Action = "test-compression";
			Description = "Figure out the best compression given an existing folder with gzip files";
		}

		[Option('f', "folder", HelpText = "Target folder", Required = true)]
		public string FolderName { get; set; }

		public override void Run() {
			var files = Directory.GetFiles(FolderName, "*.gzip").OrderBy(f => f);

			

			foreach (var file in files) {
				Printer.Hr('=');
				Printer.WriteLine("Testing on file ", "/yellow", file);

				var rawFile = Path.Combine(FolderName, "compression.temp");
				if (File.Exists(rawFile)) File.Delete(rawFile);

				Printer.WriteLine("Using temp file " + rawFile);
				using (var f = File.OpenRead(file))
				using (var o = File.Create(rawFile))
				using (var gzip = new GZipStream(f, CompressionMode.Decompress)) {
					gzip.CopyTo(o);
				}


				RunCompression(rawFile, "lz4hc",
					s => new LZ4Stream(s, LZ4StreamMode.Compress, LZ4StreamFlags.HighCompression),
					s => new LZ4Stream(s, LZ4StreamMode.Decompress));
				RunCompression(rawFile, "lz4",
					s => new LZ4Stream(s, LZ4StreamMode.Compress),
					s => new LZ4Stream(s, LZ4StreamMode.Decompress));
				RunCompression(rawFile, "gzip",
					s => new GZipStream(s, CompressionLevel.Optimal),
					s => new GZipStream(s, CompressionMode.Decompress));
				RunCompression(rawFile, "gzipfast",
					s => new GZipStream(s, CompressionLevel.Fastest),
					s => new GZipStream(s, CompressionMode.Decompress));
			}
		}

		static void RunCompression(string rawData,
			string name,
			Func<Stream, Stream> compressor,
			Func<Stream, Stream> decompressor) {
			var tempCompressedFile = rawData + "." + name;
			var info = new FileInfo(rawData);

			var rawSize = info.Length;
			var compressedSize = 0L;
			try {
				var compression = Stopwatch.StartNew();


				// test

				using (var input = File.OpenRead(rawData)) {
					using (var fz = File.Create(tempCompressedFile)) {
						using (var zip = compressor(fz)) {
							input.CopyTo(zip);
						}
					}
				}

				compressedSize = new FileInfo(tempCompressedFile).Length;
				compression.Stop();
				var decompression = Stopwatch.StartNew();
				using (var input = File.OpenRead(tempCompressedFile)) {
					using (var zip = decompressor(input)) {
						zip.CopyTo(Stream.Null);
					}
				}
				decompression.Stop();

				var ratio = compressedSize * 100F / rawSize;

				Console.WriteLine("{0}: {1:0.00}%: {2:0.0}ms compress and {3:0.0}ms decompress",
					name,
					ratio,
					compression.Elapsed.TotalMilliseconds,
					decompression.Elapsed.TotalMilliseconds
				);
			}
			finally {
				if (File.Exists(tempCompressedFile)) File.Delete(tempCompressedFile);
			}
		}
	}

}