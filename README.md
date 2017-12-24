# Geyser-net

This is an implementation of an event storage in .NET for running analytical 
workloads efficiently on a single machine. Core features:

- events are automatically split into the chunks;
- chunks are encrypted and compressed;
- designed for batching operations (high throughput);
- supports single writer and multiple concurrent readers.

This storage takes ideas from the [Message Vault](https://github.com/abdullin/messageVault),
which was based on the ideas of Kafka and append-only storage in [Lokad.CQRS](https://github.com/abdullin/lokad-cqrs)

Analytical pipeline on top of this library was deployed at SkuVault to run reporting and 
data analysis tasks on 1.5B of events (400GB+ of data). You can read more about it in
[Real-time Analytics with Go and LMDB](https://abdullin.com/bitgn/real-time-analytics/).

This .NET project was eventually ported to golang for the reasons outlined in the previous article. 
All further development happens in [cellar](https://github.com/abdullin/cellar), this .NET version is currently unmaintained.

# License

3-clause BSD license.
