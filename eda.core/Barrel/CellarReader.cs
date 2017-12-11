using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using eda.Lightning;
using eda.Util;
using LZ4;

namespace eda.Barrel {



	public sealed class CellarReader {
		readonly string _folder;
		readonly byte[] _key;

		CellarReader(string folder, byte[] key) {
			_folder = folder;
			_key = key;
		}

		Lmdb.ChunkDto[] _chunks;
		Lmdb.BufferDto _buffer;
		Lmdb.CellarDto _cellar;

		int _maxKeySize;
		int _maxValueSize;
		public long ByteSize { get; private set; }

		public static CellarReader Open(string folder, byte[] key) {
			var reader = new CellarReader(folder, key);

			reader.LoadData();
			return reader;
		}



		void LoadData() {
			if (!Directory.Exists(_folder)) {
				return;
			}



			using (var env = LmdbEnv.CreateDb(_folder, 1024 * 1024, EnvironmentOpenFlags.ReadOnly)) {
				using (var tx = env.Read()) {
					_chunks = Lmdb.ListChunks(tx).Select(c => c.Value).ToArray();
					_buffer = Lmdb.GetBuffer(tx, 0);
					_cellar = Lmdb.GetCellarMeta(tx, 0, new Lmdb.CellarDto());
					ByteSize = Cellar.EstimateSize(tx).ByteSize;
				}
			}

			_maxKeySize = _cellar.GetCellarMaxKeySize();
			_maxValueSize = _cellar.GetCellarMaxValSize();
		}

		public long GetCheckpoint(string name, long dv = default(long)) {
			using (var env = LmdbEnv.CreateDb(_folder, 1024 * 1024, EnvironmentOpenFlags.ReadOnly)) {
				using (var tx = env.Read()) {
					return Lmdb.GetStreamPosition(tx, name, dv);
				}
			}
		}

		static int Read7BitEncodedInt(Stream s, ref int pos) {
			int num1 = 0;
			int num2 = 0;
			
			while (num2 != 35) {
				byte num3 = (byte)s.ReadByte();
				pos += 1;
				num1 |= ((int)num3 & (int)sbyte.MaxValue) << num2;
				num2 += 7;
				if (((int)num3 & 128) == 0)
					return num1;
			}
			throw new InvalidOperationException("Bad format");
			
		}

		public void ReadAll(Action<ReadPos, ArraySegment<byte>, BoundedStream> handler, long offset = 0, long recordCount = long.MaxValue) {
			if (ByteSize == 0) {
				return;
			}
			
			// interested in all _chunks, where offset comes before the end
			var filteredChunks = _chunks.Where(c => offset < (c.GetChunkStartPos() + c.GetUncompressedByteSize()) ).ToList();

			var keyBuffer = new byte[_maxKeySize];
			
			
			//Console.WriteLine("{0} _chunks to read", filteredChunks.Count);

			if (filteredChunks.Any()) {
				var buffer = new byte[filteredChunks.Max(c => c.GetUncompressedByteSize())];
				foreach (var chunk in filteredChunks) {

					var path = Path.Combine(_folder, chunk.GetChunkFileName());
					var bufferSize = 16 * 1024;
					// allocate all at once
					using (var mem = new MemoryWithDisposeShield(buffer)) {
						
						using (var f = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,
							bufferSize)) {


							using (var aes = new AesCryptoServiceProvider()) {
								var iv = new byte[16];
								f.Read(iv, 0, 16);
								aes.IV = iv;
								aes.Key = _key;
								aes.Mode = CipherMode.CBC;
								aes.Padding = PaddingMode.PKCS7;

								using (var decryptor = aes.CreateDecryptor())
								using (var crypto = new CryptoStream(f, decryptor, CryptoStreamMode.Read))
								using (var lz = new LZ4Stream(crypto, CompressionMode.Decompress)) {
									// uncompress all in one go
									lz.CopyTo(mem);
								}

							}
						}

						




					

						// rewind
						mem.Seek(0, SeekOrigin.Begin);

						var localPos = 0;


						// fast forward seek bytes
						if (offset > chunk.GetChunkStartPos()) {
							var seek = (int)(offset - chunk.GetChunkStartPos());

							mem.Seek(seek, SeekOrigin.Current);
							localPos += seek;
						}

						while (localPos < chunk.GetUncompressedByteSize()) {
							// compute absolute position
							var pos = new ReadPos(localPos + chunk.GetChunkStartPos());
							// read key
							var keySize = ReadKeySize(mem, ref localPos);
							mem.Read(keyBuffer, 0, keySize);
							localPos += keySize;

							var keySegment = new ArraySegment<byte>(keyBuffer, 0, keySize);

							// read value
							var valueSize = ReadValueSize(mem, ref localPos);



							using (var b = new BoundedStream(mem, valueSize)) {
								handler(pos, keySegment, b);
							}
							localPos += valueSize;


							if (--recordCount == 0) return;
						}


					}


					
				}

			}


			if (_buffer == null) {
				return;
			}

			var bufferStartPos = _buffer.GetBufferStartPos();
			

			//Console.WriteLine("Tail starts {0}, {1} offset (or {2})", bufferStartPos, offset, offset - bufferStartPos);

			var bufferPath = Path.Combine(_folder, _buffer.GetBufferFileName());
			using (var f = File.OpenRead(bufferPath)) {


				var localPos = 0;
				if (offset > bufferStartPos) {
					var seek = (int)(offset - bufferStartPos);

					if (seek < 0) {
						var msg = string.Format("Tail starts at {0} trying to read at offset {1}", bufferStartPos, offset);
						throw new IOException(msg);
					}
					f.Seek(seek, SeekOrigin.Begin);
					localPos += seek;
				}
				var bytesInBuffer = _buffer.GetBufferPos();

				while (localPos < bytesInBuffer) {
					// record position
					var rp = new ReadPos(localPos + bufferStartPos);

					// read key
					var keySize = ReadKeySize(f, ref localPos);

					f.Read(keyBuffer, 0, keySize);
					localPos += keySize;
					var keySegment = new ArraySegment<byte>(keyBuffer, 0, keySize);
					
					// read value
					int valueSize = ReadValueSize(f, ref localPos);
					
					using (var b = new BoundedStream(f, valueSize)) {
						handler(rp, keySegment, b);
					}
					localPos += valueSize;
					if (--recordCount == 0) return;
				}
			}
		}

		int ReadValueSize(Stream gz, ref int localPos) {
			int valueSize = Read7BitEncodedInt(gz, ref localPos);
			if (valueSize < 0 || valueSize > _maxValueSize) {
				throw new InvalidOperationException(string.Format("Len {0} should be in [0,{1}]", valueSize,
					_maxValueSize));
			}
			return valueSize;
		}

		int ReadKeySize(Stream gz, ref int localPos) {
			var keySize = Read7BitEncodedInt(gz, ref localPos);

			if (keySize > _maxKeySize) {
				throw new InvalidOperationException(string.Format("{0} > {1}", keySize, _maxKeySize));
			}
			return keySize;
		}
	}

	public sealed class MemoryWithDisposeShield : MemoryStream {

		bool _hasShield = true;


		public MemoryWithDisposeShield(byte[] buffer) : base(buffer) { }

		protected override void Dispose(bool disposing) {
			if (_hasShield) {
				_hasShield = false;

			} else {
				base.Dispose(disposing);
			}

			
		}
	}




}