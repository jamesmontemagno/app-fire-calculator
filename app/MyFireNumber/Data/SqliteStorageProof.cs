using SQLite;

namespace MyFireNumber.Data;

public static class SqliteStorageProof
{
	private const string DatabaseFileName = "sqlite-storage-proof.db3";

	public static async Task<string> VerifyAsync()
	{
		var databasePath = Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);
		if (File.Exists(databasePath))
		{
			File.Delete(databasePath);
		}

		var database = new SQLiteAsyncConnection(databasePath);
		await database.CreateTableAsync<StorageProofV1>();
		await database.InsertAsync(new StorageProofV1 { Id = 1, Value = "private proof" });
		await database.CreateTableAsync<StorageProofV2>();

		var migratedRow = await database.Table<StorageProofV2>().FirstAsync();
		migratedRow.SchemaVersion = 2;
		await database.UpdateAsync(migratedRow);
		migratedRow = await database.Table<StorageProofV2>().FirstAsync();
		return migratedRow.Value == "private proof" && migratedRow.SchemaVersion == 2
			? "SQLite check passed: v1 row migrated and read from app-private storage."
			: "SQLite check failed: migrated data did not match the expected values.";
	}

	[Table("storage_proof")]
	private sealed class StorageProofV1
	{
		[PrimaryKey]
		public int Id { get; init; }

		[NotNull]
		public string Value { get; init; } = string.Empty;
	}

	[Table("storage_proof")]
	private sealed class StorageProofV2
	{
		[PrimaryKey]
		public int Id { get; init; }

		[NotNull]
		public string Value { get; init; } = string.Empty;

		public int? SchemaVersion { get; set; }
	}
}