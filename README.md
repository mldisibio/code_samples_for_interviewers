### Code Samples from Michael DiSibio

A few extracts from corporate projects demonstrating my coding style but not much else. Namespaces and some class names and comments have been garbled to obscure proprietary concepts. Do not expect the projects to compile as such.

### Points of Interest

### hex_tag_reader

In support of my efforts to return to the field of industrial automation, a small sample of extracting data from the hex representation of a proprietary RF tag. Deomonstrates some bit shifting and endianness handling. Not going to lie that I don't need a hex calculator to remember what's going on here, but I worked through it from a specification.

### sqlite_c_wrapper

I didn't write the actual C wrapper. That's the `SQLitePCLRaw` library. But I did write a C# wrapper that uses Sqlite in a way that reflects its native api, and then made that into a 'fluent' interface for C#. 

Because sqlite was not designed for multi-threaded access, this wrapper includes a locking paradigm to protect from incorrect multi-threaded access while deployed per process in a highly concurrent environment. In hindsight, the issue I was trying to address was more about linux I/O, disk throughput, and file caching than it was about sqlite, so the locking pattern was a bit overkill. 

This library supports coding a Sqlite database operation with fluent and self-guiding syntax:

```csharp
    Demo[] results = null;
    long startingId = 10000;
    using var db = SqliteDatabase.OpenReadOnly(dbPath);
    var readCtx = db.GetOpenedReadContext();
    using (var sync = readCtx.Lock())
    {
    	results = readCtx.GetOrPrepare("SELECT * FROM col_test WHERE uid = @uid;")
                         .MapParameters(pc => pc.Bind("@uid").ToInt64(startingId))
    					 .MapRow<Demo>(ctx => new Demo
    					 {
    						 uid = ctx.Read("uid").AsString(),
    						 myshort = ctx.Read("myshort").AsNullableInt16(),
    						 myepoch = ctx.Read("myepoch").AsNullableInt64(),
    						 tag = ctx.Read("tag").AsByteArray()
    					 })
    					 .ExecuteReader()
    					 .ToArray();
    }
    return results;
```

### fluent_ado

Developed from the days before Entity Framework was a twinkle in Microsoft's eye, this library was designed based on Microsoft Enterprise Library's Data Access Application Block (DAAB) to provide a fluent interface for coding database operations in C#. It was designed to be database agnostic, and supports SQL Server, Postgres, and Sqlite. It's purpose was:
- To provide a **lightweight, high-performance abstraction layer** over raw ADO.NET.
- To **emulate a fluent API** design, allowing self-guided, chainable method calls where Intellisense reveals only valid next operations.
- To offer a developer experience similar to modern libraries like Dapper, with an emphasis on **clarity, maintainability, and performance**.

While I wouldn't force anyone to use it, I still get blazing speed and error free operation from it in production code.

Sample usage:

```csharp
    string selectTable = @"SELECT author, note FROM notes";

    var postgres = PostgresDatabase.CreateFor("127.0.0.1", "alpha", "p@ssw0rd");

    // note that the connection is properly disposed when each execute block is exited
	postgres.CreateCommand()
			.FromSqlString(schemaCmd)
			.ExecuteNonQuery();

	postgres.CreateCommand()
			.FromSqlString(selectTable)
			.WithMap(reader => new { author = reader.GetString("author"), note = reader.GetString("note") })
			.ExecuteReader()
			.Dump();
```

The Postgres implementation adds a fluent interface around the Npgsql binary copy API for high performance bulk inserts:

```csharp
	// copy/dump table to csv
	var bytesRead = await postgres.CreateCommand()
						          .FromSqlString("COPY notes(author, note) TO STDIN WITH (FORMAT csv, DELIMITER ',')")
								  .AsNpgsqlContext()
								  .WithAsyncTextCopyReader(tblBuffer.GetAsText)
								  .ReadBulkExportAsync();

	Console.WriteLine($"Table csv is {bytesRead} bytes.");
	
	// write the csv to a mirror table in postgres
	await postgres.CreateCommand()
	              .FromSqlString("COPY votes(author, note) FROM STDIN WITH (FORMAT csv, DELIMITER ',')")
				  .AsNpgsqlContext()
				  .WithAsyncTextCopyWriter<long>(tblBuffer.SendAsText)
				  .WriteBulkInsertAsync(bytesRead);
```
And again, this api has been battle tested copying hundreds of terrabytes of data in production environments.

### fluent_extensions

You may begin to see a pattern of my penchant for fluent interfaces. This is a small library of extension methods that add fluent capabilities to common .NET types, such as `string`, `DateTime`, `IEnumerable<T>`, and `Stream`. Nothing fancy, just some syntactic sugar to make code more readable and expressive. In fact, it was written before `String.IsNullOrWhiteSpace` existed, but even after it was added I still prefer
```csharp
    if (myString.IsNullOrEmpty()){ ... }
    // over
    if (String.IsNullOrEmpty(myString)){ ... }
    // or the readability of this contrived example:
    myString.EmptyIfNullElseTrimmed()
            .ConvertFromBase64Text()
            .DefaultIfEmpty()
            .TakeBy(8)
            .ToList()
            .ForEach(x => parseBytes(x));
```

Also of note here are some wrappers around `gzip`, `deflate`, and `tar` compression streams. It turns out our datalake of millions of compressed files was saturated with compression errors, malformed chunks, and even `gzip` doubly compressed with `deflate`. So it was necessary to have a library that could open compressed streams, parse or skip headers and footers, skip the CRC if needed, and extract whatever valid data could be salvaged. 

### functional_paradigm

A small library of functional programming constructs for C# based heavily on the concepts from Enrico Buonanno "Functional Programming in C#". At first it included all his concepts, but in day-to-day programming just the basics are needed to make code more readable and expressive. Includes `Option<T>`, `Result<T>`, and `Either<TLeft, TRight>` monads, as well as extension methods for LINQ-like operations on these types. Also includes some functional utilities like `Bind`, `Apply`, and `Tee`. 

Sometimes I regret using the functional style, as it can be harder for some developers to read and understand. But it really does provide tangible benefits in terms of rethinking with immutability and exception handling using the `Result` monad.

```csharp
	await Task.WhenAll(parallelTasks)
			  .Bind(CollectResults)
			  .Map
			  (
				Faulted: HandleFailure,
				Completed: ProcessResults
			  )
			  .ConfigureAwait(false);
```

### file_pipeline_example

As a corporate developer it's difficult to provide full applications that are not proprietary. This is an older example of using the TPL Dataflow library to create a file processing pipeline used to process millions of electrical data files in parallel producing about 64 terabytes of output from their highly compressed storage format.

For production, I now use distributed docker containers which communicate via a message broker (RabbitMQ) and use Redis for caching and state management. But the scatter/gather approach is the same.

### Other projects

I was primary (sole) backend developer on one public facing web application for the National Park Service, although the public facing side does not expose half of the bibliographic resource management functionality the system is designed to provide. See 
- https://irma.nps.gov/DataStore/ and in particular the `Quick Search` and `Advanced Search` interfaces. For example, so a quick search for `gray wolf` returns over 500 scientific related resources. Behind that is also the internal intake and managment of those resources using REST and MS Sql Server. You get a taste of the complexity by clicking through the `Advanced Search` fields.

It was released for public use around 2012 and is still in use today. My particular source of pride is the ability to select an area on a map and have the search return resources related to that area, a fun mix of geospatial and relational queries as supported by Sql Server.
