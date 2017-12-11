using System;
using System.IO;
using eda.Util;

namespace eda.Barrel {


	/// <summary>
	///     Always variable length. Uses LMDB for checkpoints and buffer
	/// </summary>
	public sealed class CellarWriter : IDisposable {
		readonly string _folder;
		readonly int _maxBufferBytes;
		readonly byte[] _key;

		LmdbEnv _env;
		Buffer _buffer;
		int _maxKeySize;
		int _maxValSize;

		CellarWriter(string folder, int maxBufferBytes, byte[] key) {
			_folder = folder;
			_maxBufferBytes = maxBufferBytes;
			_key = key;
		}

		public static CellarWriter Create(string folder, int maxBufferSize, byte[] key) {
			var c = new CellarWriter(folder, maxBufferSize, key);
			c.Open();
			return c;
		}

		

		

		public void Open() {
			if (!Directory.Exists(_folder)) {
				Directory.CreateDirectory(_folder);
			}
			_env = LmdbEnv.CreateDb(_folder, 1*1024*1024);

			using (var tx = _env.Write()) {
				var dto = Lmdb.GetBuffer(tx, 0);
				if (dto == null) {
					SetNewBuffer(tx, 0);
				} else {
					_buffer = new Buffer(dto, _folder);
					_buffer.OpenOrCreate();
				}


				var meta = Lmdb.GetCellarMeta(tx, 0, Lmdb.NewCellarDto());
				_maxKeySize = meta.GetCellarMaxKeySize();
				_maxValSize = meta.GetCellarMaxValSize();

				tx.Commit();
			}
		}

		int PrecheckSize(int keyBytes, int valueBytes) {
			// worst case
			return 6 + keyBytes + 6 + valueBytes;
		}

		// called to flush all in-flight data
		public void Checkpoint(string name, long position) {
			_buffer.Flush();

			using (var tx = _env.Write()) {
				var bufferDto = _buffer.GetState();
				//Console.WriteLine("Checkpoint buffer at {0}+{1}", bufferDto.GetBufferStartPos(), bufferDto.GetBufferPos());
				Lmdb.SetBuffer(tx, 0, bufferDto);
				Lmdb.SetStreamPosition(tx, name, position);

				Lmdb.SetCellarMeta(tx, 0, Lmdb.NewCellarDto()
					.SetCellarMaxKeySize(_maxKeySize)
					.SetCellarMaxValSize(_maxValSize));

				tx.Commit();
			}
		}

		public long GetCheckpoint(string name, long dv = default(long)) {
			using (var tx = _env.Read()) {
				return Lmdb.GetStreamPosition(tx, name, dv);
			}
		}

		public void Append(ArraySegment<byte> key, Stream value, int valueSize) {
			var size = PrecheckSize(key.Count, valueSize);

			if (!_buffer.Fits(size)) {
				SealTheBuffer();
			}

			

			_buffer.Write7BitEncoded(key.Count);
			_buffer.Write(key);
			_buffer.Write7BitEncoded(valueSize);

			var bytesToCopy = valueSize;
			while (true) {
				if (bytesToCopy > _bytes.Length) {
					value.Read(_bytes, 0, _bytes.Length);
					_buffer.Write(new ArraySegment<byte>(_bytes));
					bytesToCopy -= _bytes.Length;
				} else {
					value.Read(_bytes, 0, bytesToCopy);
					_buffer.Write(new ArraySegment<byte>(_bytes, 0, bytesToCopy));
					break;
				}
			}

			_buffer.EndRecord();

			UpdateStatistics(key.Count, valueSize);
		}

		void SealTheBuffer() {
			var oldWriter = _buffer;
			using (var tx = _env.Write()) {
				// flush to disk
				oldWriter.Flush();
				var dto = oldWriter.Compress(_key);
				Lmdb.AddChunk(tx, dto.GetChunkStartPos(), dto);

				_buffer = null;
				var newStartPos = dto.GetChunkStartPos() + dto.GetUncompressedByteSize();
				SetNewBuffer(tx, newStartPos);

				tx.Commit();
			}

			// cleanup old writer, if possible
			oldWriter.Dispose();
			File.Delete(oldWriter.FullPath);
		}

		public void Append(ArraySegment<byte> key, ArraySegment<byte> value) {
			var size = PrecheckSize(key.Count, value.Count);
			if (!_buffer.Fits(size)) {
				SealTheBuffer();
			}

			_buffer.Write7BitEncoded(key.Count);
			_buffer.Write(key);
			_buffer.Write7BitEncoded(value.Count);
			_buffer.Write(value);
			_buffer.EndRecord();


			UpdateStatistics(key.Count, value.Count);
		}

		void UpdateStatistics(int keyCount, int valueSize) {
			if (valueSize > _maxValSize) {
				_maxValSize = valueSize;
			}
			if (keyCount > _maxKeySize) {
				_maxKeySize = keyCount;
			}
		}

		readonly byte[] _bytes = new byte[Streams.BufferSize];

		void SetNewBuffer(Tx tx, long newStartPos) {
			if (_buffer != null) {
				throw new InvalidOperationException("Previous buffer wasn't cleaned up");
			}

			var name = string.Format("{0:0000000000}.blob", newStartPos);

			var newBuffer = Lmdb
				.NewBufferDto()
				.SetBufferPos(0)
				.SetBufferStartPos(newStartPos)
				.SetBufferMaxBytes(_maxBufferBytes)
				.SetBufferRecords(0)
				.SetBufferFileName(name);


			Lmdb.SetBuffer(tx, 0, newBuffer);

			_buffer = new Buffer(newBuffer, _folder);
			_buffer.OpenOrCreate();
		}

		bool _disposed;

		public void Dispose() {
			if (_disposed) {
				throw new InvalidOperationException("Can't dispose twice");
			}
			using (_env) {
				using (_buffer) {
					_disposed = true;
				}
			}
		}


		public CellarSize EstimateSize() {
			using (var tx = _env.Read()) {
				return Cellar.EstimateSize(tx);
			}
		}
	}


	public struct CellarSize {

		public readonly int ChunkCount;
		public readonly long DiskSize;
		public readonly long ByteSize;
		public readonly long Records;
		

		public CellarSize(int chunkCount, long diskSize, long byteSize, long records) {
			ChunkCount = chunkCount;
			DiskSize = diskSize;
			ByteSize = byteSize;
			Records = records;
		}
	}

}