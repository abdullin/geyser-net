using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using CommandLine;
using eda.Barrel;
using eda.Terminal;
using eda.Util;
using LZ4;
using MessageVault;
using MessageVault.Cloud;
using MessageVault.Files;
using MessageVault.MemoryPool;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace eda.tool.Actions {

	public sealed class DownloadMessageVaultIntoCellar : ConsoleSyntax {
		readonly byte[] _buffer = new byte[Streams.BufferSize];


		readonly RecyclableStreamManagerWrapper _recyclableStreamManagerWrapper =
			new RecyclableStreamManagerWrapper();

		public DownloadMessageVaultIntoCellar() {
			Action = "import-mv";
			Description = "import message vault data into geyser";
		}

		[Option('c', "source-connection", HelpText = "File path or cloud conn")]
		public string SourceFolder { get; set; }

		[Option('s', "source-streams", HelpText = "Source streams")]
		public string SourceStreams { get; set; }

		[Option("output", HelpText = "Output stream")]
		public string OutputStream { get; set; }

		[Option('c', "chunk-size-mb", HelpText = "Chunk size in MBs", Default = 20)]
		public int ChunkSize { get; set; }

		// TODO: use pass phrase
		[Option('k', "key", HelpText = "Encryption Key")]
		public string EncryptionKey { get; set; }

		public override void Run() {
			var streams = SourceStreams.Split(new[] {';', ','}, StringSplitOptions.RemoveEmptyEntries);
			var rawSize = 1024 * 1024 * ChunkSize;


			var key = Convert.FromBase64String(EncryptionKey);
			using (var output = CellarWriter.Create(OutputStream, rawSize, key)) {
				foreach (var stream in streams) {
					var fetcher = GetFetcher(stream);


					var inputPos = output.GetCheckpoint(stream + "-pos");


					using (var cts = new CancellationTokenSource()) {
						// launch reading
						var task = fetcher.ReadAll(cts.Token, inputPos, int.MaxValue);


						while (!cts.IsCancellationRequested) {
							var start = Stopwatch.StartNew();
							var result = task.Result;

							if (result.ReadRecords <= 0) break;
							inputPos = result.CurrentPosition;
							// launch next task in advance
							task = fetcher.ReadAll(cts.Token, inputPos, int.MaxValue);

							foreach (var message in result.Messages) SaveMessage(message, output);

							output.Checkpoint(stream + "-pos", inputPos);

							var stats = output.EstimateSize();

							var totalSize = streams.Sum(s => output.GetCheckpoint(s + "-pos"));

							var compression = totalSize * 1D / stats.DiskSize;

							var streamCompletion = 100F * result.CurrentPosition / result.MaxPosition;
							start.Stop();

							var bytes = result.CurrentPosition - result.StartingPosition;
							var mbpersec = 1D * bytes / 1024 / 1024 / start.Elapsed.TotalSeconds;

							Console.WriteLine(
								"{4}: {0:##0.0}% at {1:###.0}Mb/s. {2:000} chunks with {3:##.0}x compression",
								streamCompletion, mbpersec,
								stats.ChunkCount,
								compression, stream);
						}
					}
					var size = output.EstimateSize();
					Console.WriteLine("Total records: {0} and {1} of data", size.Records,
						Print.Bytes(size.ByteSize));

					output.Checkpoint("stream-pos", inputPos);
					output.Checkpoint("raw-records", size.Records);
					output.Checkpoint("raw-bytes", size.ByteSize);
					output.Checkpoint("timestamp-secs", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
				}

				Console.WriteLine("Done");
			}
		}

		MessageFetcher GetFetcher(string stream) {
			MessageFetcher fetcher;
			CloudStorageAccount account;
			if (CloudStorageAccount.TryParse(SourceFolder, out account)) {
				var containerReference = account.CreateCloudBlobClient().GetContainerReference(stream);
				if (!containerReference.Exists()) {
					Console.WriteLine("Container doesn't exist {0}", stream);
					Environment.Exit(1);
				} else {
					Console.WriteLine(containerReference.Uri);
				}
				var container = containerReference.GetSharedAccessSignature(new SharedAccessBlobPolicy {
					Permissions = SharedAccessBlobPermissions.Read,
					SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddDays(7)
				});

				fetcher = CloudSetup.MessageFetcher(containerReference.Uri + container, stream);
			} else {
				fetcher = FileSetup.MessageFetcher(new DirectoryInfo(SourceFolder), stream,
					_recyclableStreamManagerWrapper);
			}
			return fetcher;
		}

		void SaveMessage(MessageHandlerClosure closure, CellarWriter writer) {
			try {
				var message = closure.Message;
				var keySegment = new ArraySegment<byte>(message.Key);

				using (var mem = new MemoryStream(message.Value))
				using (var zip = new LZ4Stream(mem, CompressionMode.Decompress))
				using (var pooled = _recyclableStreamManagerWrapper.GetStream("mv")) {
					Streams.Copy(zip, pooled, _buffer);
					pooled.Seek(0, SeekOrigin.Begin);

					writer.Append(keySegment, pooled, (int) pooled.Length);
				}
			}
			catch (Exception ex) {
				Console.WriteLine("Failed to read message {0}", ex.Message);
			}
		}
	}


	public sealed class RecyclableStreamManagerWrapper : IMemoryStreamManager {
		readonly RecyclableMemoryStreamManager _manager = new RecyclableMemoryStreamManager();

		public MemoryStream GetStream(string tag) {
			return _manager.GetStream(tag);
		}

		public MemoryStream GetStream(string tag, int length) {
			return _manager.GetStream(tag, length);
		}
	}

}