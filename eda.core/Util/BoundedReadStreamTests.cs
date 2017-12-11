using System;
using System.IO;
using System.IO.Compression;
using LZ4;
using NUnit.Framework;

namespace eda.Util {

	public sealed class BoundedReadStreamTests {
		[Test]
		public void LzFuzzTest() {

			var rand = new Random();
			var recordCount = rand.Next(3, 20);

			using (var source = new MemoryStream()) {
				FillStreamWithData(source, recordCount, rand);

				source.Seek(0, SeekOrigin.Begin);


				using (var un = new MemoryStream()) {
					using (var unzip = new LZ4Stream(source, CompressionMode.Decompress, LZ4StreamFlags.IsolateInnerStream)) {
						unzip.CopyTo(un);
						un.Seek(0, SeekOrigin.Begin);
					}

					using (var reader = new BinaryReader(un)) {
						for (int i = 0; i < recordCount; i++) {
							var data = reader.ReadInt32();
							Console.WriteLine("<< {0} bytes", data);

							using (var wrapper = new BoundedStream(un, data)) {

								if (i % 3 == 1) {
									// do nothing, the stream should consume
								} else {

									wrapper.CopyTo(Stream.Null);
								}
							}
						}
					}

				}
			
				
			}
		}

		static void FillStreamWithData(MemoryStream source, int recordCount, Random rand) {
			var flags = LZ4StreamFlags.IsolateInnerStream;
			using (var gzip = new LZ4Stream(source, LZ4StreamMode.Compress, flags)) {
				using (var writer = new BinaryWriter(gzip)) {
					for (int i = 0; i < recordCount; i++) {
						var size = rand.Next(0, 1024);
						var buff = new byte[size];

						Console.WriteLine(">> {0} bytes", size);


						rand.NextBytes(buff);
						writer.Write((int) size);
						writer.Write(buff);
					}
				}
			}
		}
	}

}