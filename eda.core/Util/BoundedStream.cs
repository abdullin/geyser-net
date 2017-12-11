using System;
using System.IO;

namespace eda.Util {

	/// <summary>
	/// A bounded stream forwards all read and write calls to the underlying stream, but is prevented from
	/// reading or writing past the set bound.
	/// </summary>
	public class BoundedStream : Stream {
		readonly long _length;
		readonly Stream _source;
		long _position;

		public BoundedStream(Stream source, long length) {
			_source = source;
			_length = length;
			_position = 0;

			if (!_source.CanRead) throw new NotSupportedException();
			if (!_source.CanSeek) throw new NotSupportedException("Underlying stream must support seek");
		}

		public override bool CanRead => _source.CanRead;
		public override bool CanSeek => false;
		public override bool CanWrite => false;
		public override long Length => _length;
		public override long Position {
			get { return _position; }
			set { throw new NotSupportedException(); }
		}

		public override void Flush() {
			_source.Flush();
		}

		public override long Seek(long offset, SeekOrigin origin) {
			throw new NotSupportedException();
		}

		public override void SetLength(long value) {
			throw new NotSupportedException();
		}

		public override int Read(byte[] buffer, int offset, int count) {
		
			if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
			if (count == 0) return 0;

			var bytesAvailable = (int)(_length - _position);

			if (bytesAvailable <= 0) {
				return 0;
			}

			if (count > bytesAvailable) {
				count = bytesAvailable;
			}


			var read = _source.Read(buffer, offset, count);
			_position += count;

			return read;
		}

		public override void Write(byte[] buffer, int offset, int count) {
			throw new NotSupportedException();
		}


		protected override void Dispose(bool disposing) {
			if (_position == _length) {
				return;
			}

			_source.Seek((int) (_length - _position), SeekOrigin.Current);

			

			// don't dispose base, but make sure we are moving forward

		}
	}

}