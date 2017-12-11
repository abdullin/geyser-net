//using System;
//using System.Diagnostics;
//using System.IO;
//using CommandLine;
//using eda.Terminal;
//using HdrHistogram;

//namespace eda.Actions {

//	public sealed class GatherStats : ConsoleSyntax {

//		[Option('s', "source-stream", HelpText = "Source folder")]
//		public string SourceStream { get; set; }
//		[Option('g', "geyser-folder", HelpText = "Target folder")]
//		public string GeyserFolder { get; set; }


//		public GatherStats() {
//			Action = "stats";

//		}

//		public override void Run() {
//			var path = Path.Combine(GeyserFolder, SourceStream);


//			var reader = GeyserReader.Open(path);

//			var meta = reader.Meta;
//			var totalRecords = meta.RecordCount;

//			var keySize = new LongHistogram(meta.MaxKeyLength, 3);
//			var valueSize = new LongHistogram(meta.MaxValueLength, 3);

//			if (meta.RecordCount <= 0) {
//				Console.WriteLine("Empty store.");
//				return;
//			}
//			Console.WriteLine("{0} records in {1} bytes. {2:0.0} bytes per record",
//				meta.RecordCount,
//				meta.RawBytes,
//				meta.RawBytes * 1D/ meta.RecordCount);


//			var stepCounter = Stopwatch.StartNew();
//			var fullCounter = Stopwatch.StartNew();

//			var records = 0;

//			reader.ReadAll((rp, bytes, stream) => {
//					keySize.RecordValue(bytes.Count);
//					valueSize.RecordValue(stream.Length);

//				records += 1;
//				if (stepCounter.Elapsed > TimeSpan.FromSeconds(30)) {
//					stepCounter.Restart();
//					var recsPerSec = records/fullCounter.Elapsed.TotalSeconds;
//					var secRemain = (totalRecords - records)/recsPerSec;


//					Console.WriteLine("{0:##########} records or {1:###.0}%. ETA: {2:0.0}sec", 
//						records, records * 100F / totalRecords, secRemain);
//				}

//			});

//			Printer.Hr('-');

//			keySize.OutputPercentileDistribution(
//				writer: Console.Out,
//				percentileTicksPerHalfDistance: 3,
//				outputValueUnitScalingRatio: OutputScalingFactor.None);
//			Printer.Hr('-');
//			valueSize.OutputPercentileDistribution(
//				writer: Console.Out,
//				percentileTicksPerHalfDistance: 3,
//				outputValueUnitScalingRatio: OutputScalingFactor.None);

//		}
//	}

//}

