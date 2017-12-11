using System.IO;

namespace eda.Util {

	public static class Streams {
		//We pick a value that is the largest multiple of 4096 that is still smaller than the large object heap threshold (85K).
		// The CopyTo/CopyToAsync buffer is short-lived and is likely to be collected at Gen0, and it offers a significant
		// improvement in Copy performance.
		public const int BufferSize = 81920;
		public static void Copy(Stream source, Stream target, int count, byte[] buffer = null) {
			if (buffer == null) {
				buffer = new byte[BufferSize];
			}
			var bytesToCopy = count;
			while (true) {
				if (bytesToCopy > BufferSize) {
					source.Read(buffer, 0, buffer.Length);
					target.Write(buffer, 0, buffer.Length);
					bytesToCopy -= BufferSize;
				} else {
					source.Read(buffer, 0, bytesToCopy);
					target.Write(buffer, 0, bytesToCopy);
					break;
				}
			}
		}



		public static void Copy(Stream source, Stream destination, byte[] buffer = null) {
			if (buffer == null) {
				buffer = new byte[BufferSize];
			}
			
			int read;
			while ((read = source.Read(buffer, 0, buffer.Length)) != 0)
				destination.Write(buffer, 0, read);
		}
	}

}