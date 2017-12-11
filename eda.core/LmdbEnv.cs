using System;
using System.Collections.Generic;
using System.IO;
using eda.Lightning;
using FoundationDB.Client;
using FoundationDB.Layers.Tuples;
using ProtoBuf;

namespace eda {

	public sealed class LmdbEnv : IDisposable {
		readonly LightningDatabase _db;
		readonly LightningEnvironment _env;


		public static LmdbEnv CreateTestDb(string folder) {
			return CreateDb(folder, 1024 * 1024 * 10, EnvironmentOpenFlags.NoSync);
		}

		public static LmdbEnv CreateDb(string folder, long size,
			EnvironmentOpenFlags flags = EnvironmentOpenFlags.None) {

			if (!Directory.Exists(folder)) {
				Directory.CreateDirectory(folder);
			}

			var env = new LightningEnvironment(folder, new EnvironmentConfiguration() {
				MaxDatabases = 1,
				MapSize = size,
				MaxReaders = 1024 * 4
			});

			var readOnly = (flags & EnvironmentOpenFlags.ReadOnly) == EnvironmentOpenFlags.ReadOnly;



			env.Open(flags);

			LightningDatabase db;
			if (readOnly) {
				using (var tx = env.BeginTransaction(TransactionBeginFlags.ReadOnly)) {
					db = tx.OpenDatabase();
					tx.Commit();
				}
			} else {
				var config = new DatabaseConfiguration() {
					Flags = DatabaseOpenFlags.Create
				};
				using (var tx = env.BeginTransaction()) {
					db = tx.OpenDatabase(configuration: config);
					tx.Commit();
				}
			}

			return new LmdbEnv(db, env);

		}

		public void CopyTo(string path, bool compact) {
			_env.CopyTo(path, compact);
		}

		public LmdbEnv(LightningDatabase db, LightningEnvironment env) {
			_db = db;
			_env = env;
		}

		public Tx Write() {
			return Tx.StartWrite(_env, _db);
		}

		public Tx Read() {

			return Tx.StartRead(_env, _db);
		}

		



		public void ResetData() {
			using (var tx = _env.BeginTransaction()) {
				_db.Truncate(tx);
				tx.Commit();
			}
		}


		public void Dispose() {
			_db.Dispose();

			_env.Dispose();
		}
	}


	public sealed class Tx : IDisposable {
		readonly LightningDatabase _db;
		readonly LightningTransaction _tx;





		public void TraceStart(string name, long stamp = 0) {
		}


		public void TraceStop(string name, ushort count = 0) {

		}




		public void Commit() {
			TraceStart("mdb_txn_commit");
			_tx.Commit();
			TraceStop("mdb_txn_commit");

		}
		public void Dispose() {
			_tx.Dispose();
		}


		public void Put(byte[] key, byte[] value) {
			_tx.Put(_db, key, value);
		}

		public void Delete(byte[] key) {
			_tx.Delete(_db, key);
		}

		public byte[] Get(byte[] key) {
			return _tx.Get(_db, key);
		}

		public LightningStats GetUsedSize() {
			return _tx.GetStats(_db);
		}

		public LightningCursor CreateCursor() {
			return _tx.CreateCursor(_db);
		}



		public Tx(LightningDatabase db, LightningTransaction tx) {
			_db = db;
			_tx = tx;
		}



		public static Tx StartRead(LightningEnvironment env, LightningDatabase db) {
			var tx = env.BeginTransaction(TransactionBeginFlags.ReadOnly);
			return new Tx(db, tx);
		
		}

		public static Tx StartWrite(LightningEnvironment env, LightningDatabase db) {
			var tx = env.BeginTransaction(TransactionBeginFlags.None);
			return new Tx(db, tx);
			
		}
	}


	public static class DslLib {

		public static void Delete(Tx tx, byte[] key) {
			tx.TraceStart("Delete");
			tx.Delete(key);
			tx.TraceStop("Delete");
		}

		public static byte[] CreateKey<T1>(byte table, T1 t1) {
			return FdbTuple.Create(table, t1).ToSlice().GetBytes();
		}

		public static byte[] CreateKey<T1, T2>(byte table, T1 t1, T2 t2) {
			return FdbTuple.Create(table, t1, t2).ToSlice().GetBytes();
		}


		public static byte[] CreateKey<T1, T2, T3>(byte table, T1 t1, T2 t2, T3 t3) {
			return FdbTuple.Create(table, t1, t2, t3).ToSlice().GetBytes();
		}

		public static byte[] CreateKey<T1, T2, T3, T4>(byte table, T1 t1, T2 t2, T3 t3, T4 t4) {
			return FdbTuple.Create(table, t1, t2, t3, t4).ToSlice().GetBytes();
		}

		public static byte[] CreateKey<T1, T2, T3, T4, T5>(byte table, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5) {
			return FdbTuple.Create(table, t1, t2, t3, t4, t5).ToSlice().GetBytes();
		}

		public static byte[] CreateKey<T1, T2, T3, T4, T5, T6>(byte table, T1 t1, T2 t2, T3 t3, T4 t4,
			T5 t5, T6 t6) {
			return FdbTuple.Create(table, t1, t2, t3, t4, t5, t6).ToSlice().GetBytes();
		}


		public static byte[] CreateKey(byte table) {
			return FdbTuple.Create(table).ToSlice().GetBytes();
		}

		public static ulong GetValueAsUInt64(Tx tx, byte[] key, ulong defaultValue = 0) {
			var data = tx.Get(key);
			if (data == null) {
				return defaultValue;
			}
			return Slice.Create(data).ToUInt64();
		}

		public static long GetValueAsInt64(Tx tx, byte[] key, long defaultValue = 0) {
			var data = tx.Get(key);
			if (data == null) {
				return defaultValue;
			}
			return Slice.Create(data).ToInt64();
		}


		
		public static void SetValue(Tx tx, byte[] key, ulong value) {
			tx.Put(key, Slice.FromUInt64(value).GetBytes());
		}

		public static void SetValue(Tx tx, byte[] key, long value) {
			tx.Put(key, Slice.FromInt64(value).GetBytes());
		}


		public static T GetOrDefault<T>(Tx tx, byte[] key, T defaultValue = default(T)) {
			var data = tx.Get(key);
			if (data == null) {
				return defaultValue;
			}

			using (var mem = new MemoryStream(data)) {
				return Serializer.Deserialize<T>(mem);
			}
		}


		public static void Set<T>(Tx tx, byte[] key, T dto) {
			using (var mem = new MemoryStream()) {
				Serializer.Serialize(mem, dto);
				tx.Put(key, mem.ToArray());
			}
		}

		static byte[] EmptyBuffer = new byte[0];
		public static void SetEmpty(Tx tx, byte[] key) {
			tx.Put(key, EmptyBuffer);

		}

		public static bool Exists(Tx tx, byte[] key) {
			using (var cursor = tx.CreateCursor()) {
				var exists = cursor.MoveTo(key);
				return exists;
			}
		}

		public static void DeleteRange(Tx tx, byte[] key, string traceName) {
			tx.TraceStart(traceName);
			ushort x = 0;
			var slice = Slice.Create(key);
			var range = FdbKeyRange.StartsWith(slice);

			using (var c = tx.CreateCursor()) {

				if (!c.MoveToFirstAfter(key)) {
					tx.TraceStop(traceName, x);
					return;
				}

				var pair = c.Current;

				for (var i = 0; i < int.MaxValue; i++) {
					var current = Slice.Create(pair.Key);
					if (!range.Contains(current)) {
						tx.TraceStop(traceName, x);
						break;
					}

					x += 1;
					tx.Delete(pair.Key);

					if (!c.MoveNext()) {
						tx.TraceStop(traceName, x);
						break;
					}
					pair = c.Current;
				}
			}


			tx.TraceStop(traceName);
		}

		static IEnumerable<TOut> InternalScan<TOut>(Tx tx, byte[] key,
			Func<Slice, byte[], TOut> convert, string traceName, int skip = 0) {

			tx.TraceStart(traceName);

			ushort x = 0;
			var slice = Slice.Create(key);
			var range = FdbKeyRange.StartsWith(slice);
			using (var c = tx.CreateCursor()) {

				if (!c.MoveToFirstAfter(key)) {
					tx.TraceStop(traceName, x);
					yield break;
				}

				var pair = c.Current;

				for (var i = 0; i < int.MaxValue; i++) {
					var current = Slice.Create(pair.Key);
					if (!range.Contains(current)) {
						tx.TraceStop(traceName, x);
						break;
					}
					if (i >= skip) {
						x += 1;
						yield return convert(current, pair.Value);

					}

					if (!c.MoveNext()) {
						tx.TraceStop(traceName, x);
						break;
					}
					pair = c.Current;
				}
			}

		}


		public static IEnumerable<KeyValuePair<TKey, ulong>> ScanUInt64<TKey>(Tx tx, byte[] key,
			Func<IFdbTuple, TKey> keygen, string traceName, int skip = 0) {

			return InternalScan(tx, key, (slice, bytes) => {
				var value = Slice.Create(bytes).ToUInt64();
				var k = keygen(FdbTuple.Unpack(slice));
				return new KeyValuePair<TKey, ulong>(k, value);
			}, traceName, skip);
		}

		public static IEnumerable<KeyValuePair<TKey, TValue>> Scan<TKey, TValue>(Tx tx, byte[] key,
			Func<IFdbTuple, TKey> keygen, string traceName, int skip = 0) {

			

			return InternalScan(tx, key, (slice, bytes) => {
				using (var mem = new MemoryStream(bytes)) {
					var value = Serializer.Deserialize<TValue>(mem);
					var k = keygen(FdbTuple.Unpack(slice));
					return new KeyValuePair<TKey, TValue>(k, value);
				}
			}, traceName, skip);
		}

		public static IEnumerable<TKey> ScanKeys<TKey>(Tx tx, byte[] key, Func<IFdbTuple, TKey> keygen,
			string traceName, int skip = 0) {
			return InternalScan(tx, key, (slice, bytes) => keygen(FdbTuple.Unpack(slice)), traceName, skip);
		}
	}


}