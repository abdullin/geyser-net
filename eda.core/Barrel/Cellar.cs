using System.IO;

namespace eda.Barrel {

	public static class Cellar {
		

		public static CellarSize EstimateSize(Tx tx) {
			var chunkCount = 0;
			var diskSize = 0L;
			var byteSize = 0L;
			var records = 0L;
			foreach (var pair in Lmdb.ListChunks(tx)) {
				chunkCount += 1;
				var dto = pair.Value;
				diskSize += dto.GetCompressedDiskSize();
				byteSize += dto.GetUncompressedByteSize();
				records += dto.GetChunkRecords();
			}
			var buffer = Lmdb.GetBuffer(tx, 0);
			if (buffer != null) {
				diskSize += buffer.GetBufferMaxBytes();
				byteSize += buffer.GetBufferPos();
				records += buffer.GetBufferRecords();
			}
			return new CellarSize(chunkCount, diskSize, byteSize, records);

		}
	}

}