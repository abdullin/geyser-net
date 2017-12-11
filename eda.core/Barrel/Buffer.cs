using System;
using System.IO;
using System.Security.Cryptography;
using eda.Util;
using LZ4;

namespace eda.Barrel {

	/// <summary>
	/// Append-only chunk of data that could be compressed.
	/// It preallocates bytes on the disk (to reduce fragmentation) and hence 
	/// needs to make sure that new appends fit into the storage
	/// </summary>
	public sealed class Buffer : IDisposable {
		FileStream _stream;
		BinaryWriter _writer;

		readonly string _fileName;
		readonly int _maxBytes;
		readonly long _startPos;

		public readonly string FullPath;

		int _records;
		int _pos;


		public bool Fits(int count) {
			return (_pos + count) <= _maxBytes;
		}


		public Buffer(Lmdb.BufferDto dto, string folder) {
			
			_fileName = dto.GetBufferFileName();

			FullPath = Path.Combine(folder, _fileName);

			_startPos = dto.GetBufferStartPos();

			_maxBytes = dto.GetBufferMaxBytes();
			_pos = dto.GetBufferPos();
			_records = dto.GetBufferRecords();
		}

		public Lmdb.BufferDto GetState() {
			return Lmdb.NewBufferDto()
				.SetBufferFileName(_fileName)
				.SetBufferMaxBytes(_maxBytes)
				.SetBufferRecords(_records)
				.SetBufferStartPos(_startPos)
				.SetBufferPos(_pos);
		}

		public void OpenOrCreate() {
			var data = new FileInfo(FullPath);
			var stream = data.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
			if (stream.Length < _maxBytes) {
				stream.SetLength(_maxBytes);
			}
			
			stream.Seek(_pos, SeekOrigin.Begin);

			_stream = stream;
			_writer = new BinaryWriter(_stream);
		}

		public void Write7BitEncoded(int value) {
			// Write out an int 7 bits at a time.  The high bit of the byte,
			// when on, tells reader to continue reading more bytes.
			uint v = (uint)value;   // support negative numbers
			while (v >= 0x80) {
				_stream.WriteByte((byte)(v | 0x80));
				_pos += 1;
				v >>= 7;
			}
			_stream.WriteByte((byte)v);
			_pos += 1;
		}


		public void Write(ArraySegment<byte> key) {
			_stream.Write(key.Array, key.Offset, key.Count);
			_pos += key.Count;
		}

		public void EndRecord() {
			_records += 1;
		}
		
		public Lmdb.ChunkDto Compress(byte[] symmetricKey) {

			var fileName = _stream.Name + ".lz4";
			_stream.Seek(0, SeekOrigin.Begin);

			
			using (var chunkFile = File.Create(fileName)) {
				var flags = LZ4StreamFlags.HighCompression | LZ4StreamFlags.IsolateInnerStream;



				using (var aes = new AesCryptoServiceProvider()) {
					aes.Key = symmetricKey;
					aes.Mode = CipherMode.CBC;
					aes.Padding = PaddingMode.PKCS7;
					aes.GenerateIV();
					var iv = aes.IV;
					chunkFile.Write(iv, 0, iv.Length);

					using (var enc = aes.CreateEncryptor())
					using (var crypto = new CryptoStream(chunkFile, enc, CryptoStreamMode.Write))
					using (var lz4 = new LZ4Stream(crypto, LZ4StreamMode.Compress, flags, 1048576 * 4)) {
						Streams.Copy(_stream, lz4, (int) _pos);
					}
				}
			}

			var size = new FileInfo(fileName).Length;

			return Lmdb.NewChunkDto()
					.SetChunkFileName(fileName)
					.SetUncompressedByteSize(_pos)
					.SetChunkRecords(_records)
					.SetCompressedDiskSize((int) size)
					.SetChunkStartPos(_startPos);			
		}


		public void Flush() {
			_stream.Flush();
		}

		bool _disposed;


		public void Dispose() {
			if (_disposed) {
				return;

			}
			using (_stream)
			using (_writer) {
				_disposed = true;
			}
		}
	}

}