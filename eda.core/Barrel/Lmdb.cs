// this code is auto-generated, don't modify manually
using FoundationDB.Layers.Tuples;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace eda.Barrel {
	public static partial class Lmdb {
		// User types
		public static ChunkDto NewChunkDto() {
			return new ChunkDto();
		}
		[DataContract]
		public class ChunkDto {
			[DataMember(Order=1)] int _chunkUncompressedByteSize;
			[DataMember(Order=2)] int _chunkCompressedDiskSize;
			[DataMember(Order=3)] long _chunkRecords;
			[DataMember(Order=4)] string _chunkFileName;
			[DataMember(Order=5)] long _chunkStartPos;
			
			public ChunkDto SetUncompressedByteSize(int chunkByteSize) {
				_chunkUncompressedByteSize = chunkByteSize;
				return this;
			}
			public int GetUncompressedByteSize() {
				return _chunkUncompressedByteSize;
			}
			public ChunkDto SetCompressedDiskSize(int chunkDiskSize) {
				_chunkCompressedDiskSize = chunkDiskSize;
				return this;
			}
			public int GetCompressedDiskSize() {
				return _chunkCompressedDiskSize;
			}
			public ChunkDto SetChunkRecords(long chunkRecords) {
				_chunkRecords = chunkRecords;
				return this;
			}
			public long GetChunkRecords() {
				return _chunkRecords;
			}
			public ChunkDto SetChunkFileName(string chunkFileName) {
				_chunkFileName = chunkFileName;
				return this;
			}
			public string GetChunkFileName() {
				return _chunkFileName;
			}
			public ChunkDto SetChunkStartPos(long chunkStartPos) {
				_chunkStartPos = chunkStartPos;
				return this;
			}
			public long GetChunkStartPos() {
				return _chunkStartPos;
			}
		
		}
		public static BufferDto NewBufferDto() {
			return new BufferDto();
		}
		[DataContract]
		public class BufferDto {
			[DataMember(Order=1)] long _bufferStartPos;
			[DataMember(Order=2)] int _bufferMaxBytes;
			[DataMember(Order=3)] int _bufferRecords;
			[DataMember(Order=4)] int _bufferPos;
			[DataMember(Order=5)] string _bufferFileName;
			
			public BufferDto SetBufferStartPos(long bufferStartPos) {
				_bufferStartPos = bufferStartPos;
				return this;
			}
			public long GetBufferStartPos() {
				return _bufferStartPos;
			}
			public BufferDto SetBufferMaxBytes(int bufferMaxBytes) {
				_bufferMaxBytes = bufferMaxBytes;
				return this;
			}
			public int GetBufferMaxBytes() {
				return _bufferMaxBytes;
			}
			public BufferDto SetBufferRecords(int bufferRecords) {
				_bufferRecords = bufferRecords;
				return this;
			}
			public int GetBufferRecords() {
				return _bufferRecords;
			}
			public BufferDto SetBufferPos(int bufferPos) {
				_bufferPos = bufferPos;
				return this;
			}
			public int GetBufferPos() {
				return _bufferPos;
			}
			public BufferDto SetBufferFileName(string bufferFileName) {
				_bufferFileName = bufferFileName;
				return this;
			}
			public string GetBufferFileName() {
				return _bufferFileName;
			}
		
		}
		public static CellarDto NewCellarDto() {
			return new CellarDto();
		}
		[DataContract]
		public class CellarDto {
			[DataMember(Order=1)] int _cellarMaxKeySize;
			[DataMember(Order=2)] int _cellarMaxValSize;
			
			public CellarDto SetCellarMaxKeySize(int cellarMaxKeySize) {
				_cellarMaxKeySize = cellarMaxKeySize;
				return this;
			}
			public int GetCellarMaxKeySize() {
				return _cellarMaxKeySize;
			}
			public CellarDto SetCellarMaxValSize(int cellarMaxValSize) {
				_cellarMaxValSize = cellarMaxValSize;
				return this;
			}
			public int GetCellarMaxValSize() {
				return _cellarMaxValSize;
			}
		
		}
		// Space barrel/Chunk
		public struct ChunkKey {
			readonly IFdbTuple _key;
			public ChunkKey(IFdbTuple key) {
				_key = key;
			}
			public long GetChunkStartPos() {
				return _key.Get<long>(1);
			}
		
		}
		// {:db/op :set, :db/method "AddChunk"}
		public static void AddChunk(Tx tx, long chunkStartPos, ChunkDto dto) {
			tx.TraceStart("AddChunk");
			var key = DslLib.CreateKey((byte)Tables.Chunk, chunkStartPos);
			DslLib.Set(tx, key, dto);
			tx.TraceStop("AddChunk");
		
		}
		// {:db/op :scan, :count 0, :db/method "ListChunks"}
		public static IEnumerable<KeyValuePair<ChunkKey, ChunkDto>> ListChunks(Tx tx, int skip = 0) {
			var key = DslLib.CreateKey((byte)Tables.Chunk);
			return DslLib.Scan<ChunkKey, ChunkDto>(tx, key, t => new ChunkKey(t), "ListChunks", skip);
		}
		// {:db/op :fetch, :db/method "GetChunk"}
		public static ChunkDto GetChunk(Tx tx, long chunkStartPos, ChunkDto dv = default(ChunkDto)) {
			tx.TraceStart("GetChunk");
			var key = DslLib.CreateKey((byte)Tables.Chunk, chunkStartPos);
			var value = DslLib.GetOrDefault<ChunkDto>(tx, key, dv);
			tx.TraceStop("GetChunk");
			return value;
		}
		// {:db/op :delete, :db/method "DropChunk"}
		public static void DropChunk(Tx tx, long chunkStartPos) {
			tx.TraceStart("DropChunk");
			var key = DslLib.CreateKey((byte)Tables.Chunk, chunkStartPos);
			DslLib.Delete(tx, key);
			tx.TraceStop("DropChunk");
		
		}
		// Space barrel/Buffer
		public struct BufferKey {
			readonly IFdbTuple _key;
			public BufferKey(IFdbTuple key) {
				_key = key;
			}
			public byte GetBufferId() {
				return _key.Get<byte>(1);
			}
		
		}
		// {:db/op :fetch, :db/method "GetBuffer"}
		public static BufferDto GetBuffer(Tx tx, byte bufferId, BufferDto dv = default(BufferDto)) {
			tx.TraceStart("GetBuffer");
			var key = DslLib.CreateKey((byte)Tables.Buffer, bufferId);
			var value = DslLib.GetOrDefault<BufferDto>(tx, key, dv);
			tx.TraceStop("GetBuffer");
			return value;
		}
		// {:db/op :set, :db/method "SetBuffer"}
		public static void SetBuffer(Tx tx, byte bufferId, BufferDto dto) {
			tx.TraceStart("SetBuffer");
			var key = DslLib.CreateKey((byte)Tables.Buffer, bufferId);
			DslLib.Set(tx, key, dto);
			tx.TraceStop("SetBuffer");
		
		}
		// Space barrel/Cellar
		public struct CellarKey {
			readonly IFdbTuple _key;
			public CellarKey(IFdbTuple key) {
				_key = key;
			}
			public byte GetCellarId() {
				return _key.Get<byte>(1);
			}
		
		}
		// {:db/op :fetch, :db/method "GetCellarMeta"}
		public static CellarDto GetCellarMeta(Tx tx, byte cellarId, CellarDto dv = default(CellarDto)) {
			tx.TraceStart("GetCellarMeta");
			var key = DslLib.CreateKey((byte)Tables.Cellar, cellarId);
			var value = DslLib.GetOrDefault<CellarDto>(tx, key, dv);
			tx.TraceStop("GetCellarMeta");
			return value;
		}
		// {:db/op :set, :db/method "SetCellarMeta"}
		public static void SetCellarMeta(Tx tx, byte cellarId, CellarDto dto) {
			tx.TraceStart("SetCellarMeta");
			var key = DslLib.CreateKey((byte)Tables.Cellar, cellarId);
			DslLib.Set(tx, key, dto);
			tx.TraceStop("SetCellarMeta");
		
		}
		// Space barrel/Meta
		public struct MetaKey {
			readonly IFdbTuple _key;
			public MetaKey(IFdbTuple key) {
				_key = key;
			}
			public string GetStreamName() {
				return _key.Get<string>(1);
			}
		
		}
		// {:db/op :set, :db/method "SetStreamPosition"}
		public static void SetStreamPosition(Tx tx, string streamName, long dto) {
			tx.TraceStart("SetStreamPosition");
			var key = DslLib.CreateKey((byte)Tables.Meta, streamName);
			DslLib.Set(tx, key, dto);
			tx.TraceStop("SetStreamPosition");
		
		}
		// {:db/op :fetch, :db/method "GetStreamPosition"}
		public static long GetStreamPosition(Tx tx, string streamName, long dv = default(long)) {
			tx.TraceStart("GetStreamPosition");
			var key = DslLib.CreateKey((byte)Tables.Meta, streamName);
			var value = DslLib.GetOrDefault<long>(tx, key, dv);
			tx.TraceStop("GetStreamPosition");
			return value;
		}
	
	}

}
