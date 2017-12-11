(ns working
  (:refer-clojure :exclude [type alias])
  (:require
   [lang :refer :all]))

(group "base" "Native bindings"
       (native long "Int64" "long")
       (native ulong "ulong" "ulong")
       (native bool "Boolean" "bool")
       (native string "Unicode string" "string")
       (native int "Int32" "int")
       (native byte "256 bits" "byte")
       (native guid ".NET Guid" "Guid")
       (native decimal "Decimal" "decimal")
       (native timestamp ".NET DateTime" "DateTime")
       (native date-time "DateTime" "DateTime")
       (native uint "uint" "uint"))

(group "ts" "Time-series"
       (alias day "Days since epoch" uint)
       (alias counter "a counter" ulong))

(group "evt" "Message stuff"
       (alias type "Event type" byte))

(group "tenant" "tenant-related schemas"
       (alias id "Numeric tenant id" long (> 0)))

(group "chunk" "Barrel storage"
       (alias start-pos "Raw chunk pos" long)
       (alias file-name "File name" string)
       (alias byte-size "Raw size" int)
       (alias disk-size "Storage size" int)
       (alias records "Record count" long))

(group "buffer" "Tail buffer"
       (alias id "Buffer id = 0" byte)
       (alias file-name "File name" string)
       (alias start-pos "Raw position where the buffer starts" long)

       (alias max-bytes "Max allowed size for the buffer" int)
       (alias records "Record count" int)
       (alias pos "Position within the buffer" int))

(group "stream" "Stream references"
       (alias name "Name of the stream" string)
       (alias byte-pos "Raw position" long))
(group "cellar" "Meta cellar info"
       (alias id "Cellar id = 0" byte)
       (alias max-key-size "Max key size" int)
       (alias max-val-size "Max Val size" int))
	   ;; comment

(lmdb "barrel"
  {:ns "eda.Barrel"
   :path "eda.mv/Barrel/Lmdb.cs"
   :class "Lmdb"
   :using ["FoundationDB.Layers.Tuples","System.Collections.Generic"]}

   ;; storage chunks
  (space "Chunk"
    [chunk/start-pos]
    [chunk/byte-size chunk/disk-size chunk/records chunk/file-name chunk/start-pos]

    (set "AddChunk")
    (scan "ListChunks" 0)
    (fetch "GetChunk")
    (del "DropChunk"))

   ;; meta info
  (space "Buffer"
    [buffer/id]
    [buffer/start-pos buffer/max-bytes buffer/records buffer/pos buffer/file-name]

    (fetch "GetBuffer")

    (set "SetBuffer"))

  (space "Cellar"
    [cellar/id]
    [cellar/max-key-size cellar/max-val-size]

    (fetch "GetCellarMeta")
    (set "SetCellarMeta"))
   ;;
  (space "Meta" [stream/name]
    [stream/byte-pos]
    (set "SetStreamPosition")
    (fetch "GetStreamPosition")))
