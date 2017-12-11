using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using eda.Util;
using NUnit.Framework;

namespace eda.Barrel {

	public sealed class CellarTests {
		[Test]
		public void GenerateKeySample() {

			while (true) {
				var base64String = Convert.ToBase64String(GenerateKey());

				if (base64String.IndexOfAny(new char[] {'+', '/'}) >= 0) {
					continue;
				}


				Console.WriteLine(base64String);
				return;
			}
		}

		byte[] GenerateKey() {
			var r = new Random();
			var key = new byte[32];
			r.NextBytes(key);
			return key;

		}

		[Test]
		public void FixedKey() {
			var folder = NewFolder("simple-test");
			var valueWritten = 0;
			var key = new ArraySegment<byte>(new byte[8]);
			var value = new ArraySegment<byte>(new byte[64]);

			// 256 bits = 32 bytes

			var aes = GenerateKey();

			using (var appender = CellarWriter.Create(folder, 1000, aes)) {
				
				for (int i = 0; i < 30; i++) {
					using (var ms = new MemoryStream(value.Array)) {
						appender.Append(key, ms, (int) ms.Length);
					}

					valueWritten += value.Count;
				}

				appender.Checkpoint("s", 0);
			}

			var valuesRead = 0L;

			CellarReader.Open(folder, aes).ReadAll((_, bytes, stream) => valuesRead += stream.Length);

			Assert.AreEqual(valueWritten, valuesRead, "values");
		}



		//[Test]
		//public void VariableOffsetReading() {
		//	var folder = NewFolder("offset-read-test");

		//	var r = new Random();


		//	using (var appender = Cellar.Create(folder, 1000)) {
		//		var list = new List<Record>();
		//		for (int i = 0; i < 1000; i++) {


		//			var key = new byte[r.Next(1, 100)];
		//			r.NextBytes(key);
		//			var val = new byte[r.Next(1, 100)];
		//			r.NextBytes(val);

		//			var record = new Record() {

		//				Key = key,
		//				Value = val
		//			};
		//			var res = appender.Append(new ArraySegment<byte>(record.Key),
		//				new ArraySegment<byte>(record.Value));
		//			record.Position = res;
		//			list.Add(record);


		//		}
		//		appender.CommitAndClose(true);
		//	}

		//	var reader = GeyserReader.Open(folder);


		//	foreach (var record in list) {
		//		reader.ReadAll((rp, bytes, stream) => {

		//			Assert.AreEqual(record.Position.StartPosition, rp.Position, "record " + record.Position);
		//			CollectionAssert.AreEqual(FillBytes(bytes), record.Key, "record " + record.Position);

		//		}, record.Position.StartPosition, 1);
		//	}
		//}

		byte[] FillBytes(ArraySegment<byte> data) {
			var seg = new byte[data.Count];
			Array.Copy(data.Array, seg, seg.Length);
			return seg;
		}


		//[Test]
		//public void FixedOffsetReading() {
		//	var folder = NewFolder("offset-read-test");

		//	var r = new Random();


		//	var appender = GeyserWriter.OpenOrCreate(folder, 1000, 16);
		//	var list = new List<Record>();
		//	for (int i = 0; i < 1000; i++) {

		//		var val = new byte[r.Next(1, 100)];
		//		r.NextBytes(val);

		//		var record = new Record() {

		//			Key = Guid.NewGuid().ToByteArray(),
		//			Value = val
		//		};
		//		var res = appender.Append(new ArraySegment<byte>(record.Key), new ArraySegment<byte>(record.Value));
		//		record.Position = res;
		//		list.Add(record);


		//	}
		//	appender.CommitAndClose(true);

		//	var reader = GeyserReader.Open(folder);


		//	foreach (var record in list) {
		//		reader.ReadAll((rp, bytes, stream) => {
		//			CollectionAssert.AreEqual(bytes.Array, record.Key, "record " + record.Position);
		//			Assert.AreEqual(record.Position.StartPosition, rp.Position);

		//		}, record.Position.StartPosition, 1);
		//	}
		//}

		[Test]
		public void FuzzTest() {

			var fuzz = NewFolder("eda-test-fuzz");



			CellarWriter appender = null;
			var rand = new Random();


			var bytesWritten = 0;
			var maxIterations = 1000;
			var maxValueLength = rand.Next(1, 1024 * 128);
			var valueBuffer = new byte[maxValueLength];
			var tailLength = rand.Next(1, maxValueLength * maxIterations / 2);
			var keyLength = rand.Next(0, 64);

			if (keyLength % 2 == 1) {
				// make key 0 (variable) in half of the cases
				keyLength = -1;
			}

			Console.WriteLine("Keylen {0}", keyLength);


			var keyBuffer = new byte[keyLength == -1 ? ushort.MaxValue : keyLength];
			var aes = GenerateKey();



			for (int i = 0; i <= maxIterations; i++) {

				if (rand.Next(17) == 13 || i == maxIterations) {
					if (appender != null) {
						appender.Checkpoint("",0);
						appender.Dispose();
						appender = null;
						Console.WriteLine("Reopen");
					}


					var bytesRead = 0L;
					
					CellarReader.Open(fuzz, aes).ReadAll((_, arg1, stream) => {
						bytesRead += stream.Length;
					});

					Assert.AreEqual(bytesWritten, bytesRead);
				}
				if (appender == null) {
					appender = CellarWriter.Create(fuzz, tailLength, aes);
				}

				rand.NextBytes(valueBuffer);
				rand.NextBytes(keyBuffer);
				var valueLength = rand.Next(valueBuffer.Length);


				ArraySegment<byte> keySegment;
				if (keyLength == -1) {
					keySegment = new ArraySegment<byte>(keyBuffer, 0, rand.Next(keyBuffer.Length));
				} else {
					keySegment = new ArraySegment<byte>(keyBuffer, 0, keyLength);
				}

				switch (i % 4) {
					case 0:
						// write as byte
						var valueSegment = new ArraySegment<byte>(valueBuffer, 0, valueLength);
						appender.Append(keySegment, valueSegment);
						break;
					case 1:
						// write as bounded stream
						using (var ms = new MemoryStream(valueBuffer))
						using (var bs = new BoundedStream(ms, valueLength)) {
							appender.Append(keySegment, bs, valueLength);
						}
						break;
					default:
						// write as mem stream
						using (var ms = new MemoryStream(valueBuffer))
							appender.Append(keySegment, ms, valueLength);
						break;

				}



				bytesWritten += valueLength;
			}
			using (appender) {
				appender.Checkpoint("",0);
			}
			
		}

		static string NewFolder(string edaTestFuzz) {
			var folder = Path.GetTempPath();
			var fuzz = Path.Combine(folder, edaTestFuzz);
			if (Directory.Exists(fuzz)) {
				Directory.Delete(fuzz, true);
			}

			Directory.CreateDirectory(fuzz);
			return fuzz;
		}
	}

}