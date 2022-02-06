using System.Diagnostics;

namespace CloudMigrationWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        /*ThreadPool.SetMinThreads(1, 1);
        ThreadPool.SetMaxThreads(1, 1);*/
        while (!stoppingToken.IsCancellationRequested)
        {
            Stopwatch stopwatch = new();

            var tasks = new List<Task>();
            stopwatch.Start();
            for (int i = 1; i <= 3; i++)
            {
                tasks.Add(PrintTaskInfo(i, stopwatch));
            }


            await Task.WhenAll(tasks);
            _logger.LogInformation("Sleeping");
        }
    }

    private async Task<int> PrintTaskInfo(int number, Stopwatch stopwatch)
    {
        _logger.LogInformation(
            $"Task# {number} started after {stopwatch.ElapsedMilliseconds / 1000} s and thread id {Thread.CurrentThread.ManagedThreadId}");
        await Task.Delay(10000);
        //Thread.Sleep(2000);
        _logger.LogInformation(
            $"Task# {number} completed after {stopwatch.ElapsedMilliseconds / 1000} s and thread id {Thread.CurrentThread.ManagedThreadId}");
        return await Task.FromResult(number);
    }
}

public interface IAwsService
{
};

public interface IFileReader
{
};

public record CloudFileInfo(string SourcePath, string DestinationPath, string CloudTier);

public abstract class CloudFileMigration<T> //Its abstract thus can not be used directly, thus must be inherited.
{
    private readonly IAwsService _awsService;
    private readonly IFileReader _fileReader;

    // All subclasses will have to provide these services or dependencies 
    protected CloudFileMigration(IAwsService awsService, IFileReader fileReader)
    {
        _awsService = awsService;
        _fileReader = fileReader;
    }

    // A Template Method  or Invariant Trait of the Pattern
    public async Task Migrate()
    {
        var items = await GetItems();
        await Task.WhenAll(items.Select(MigrateItem));

        async Task MigrateItem(T item)
        {
            var cloudFileInfo = await GetCloudFileInfo(item);
            await UploadToAws3(cloudFileInfo);
            var hasUpdateItemSucceeded = await UpdateItem(item, cloudFileInfo);
            if (!hasUpdateItemSucceeded)
                //handle failure
                await DeleteFromAws3(cloudFileInfo);
        }
    }

    private Task UploadToAws3(CloudFileInfo cloudFileInfo) => Task.CompletedTask; //Do the work
    private Task DeleteFromAws3(CloudFileInfo cloudFileInfo) => Task.CompletedTask; // Do the work

    /*
     * All abstract methods below will have different behavior in sub classes.
     * This is Variant Trait of the Pattern
     */
    protected abstract Task<IEnumerable<T>> GetItems();
    protected abstract Task<bool> UpdateItem(T item, CloudFileInfo cloudFileInfo);
    protected abstract Task<CloudFileInfo> GetCloudFileInfo(T item);
}

public record AssetFile(int PrimaryKey, string LocalPath, DateOnly CreatedDate);

public class AssetFileMigration : CloudFileMigration<AssetFile>
{
    public AssetFileMigration(IAwsService awsService, IFileReader fileReader) : base(awsService, fileReader)
    {
    }

    protected override Task<IEnumerable<AssetFile>> GetItems() =>
        Task.FromResult(Enumerable.Empty<AssetFile>()); // Fetch it from source, say DB

    protected override Task<bool> UpdateItem(AssetFile item, CloudFileInfo cloudFileInfo) =>
        Task.FromResult(true); //Update DB

    // 1- Store in bucket A, Cloud Tier, And Local Path To Read from
    protected override Task<CloudFileInfo> GetCloudFileInfo(AssetFile item) =>
        Task.FromResult(new CloudFileInfo("", "", ""));
}

public record StatementFile(int PrimaryKey, string LocalPath, DateOnly LastUpdatedDate);

public class StatementFileMigration : CloudFileMigration<StatementFile>
{
    public StatementFileMigration(IAwsService awsService, IFileReader fileReader) : base(awsService, fileReader)
    {
    }

    protected override Task<IEnumerable<StatementFile>> GetItems() =>
        Task.FromResult(Enumerable.Empty<StatementFile>()); // Fetch it DB

    protected override Task<bool> UpdateItem(StatementFile item, CloudFileInfo cloudFileInfo) =>
        Task.FromResult(true); //Update DB

    // 1- Store in bucket B, Cloud Tier based on LastUpdateDate, And Local Path To Read from
    protected override Task<CloudFileInfo> GetCloudFileInfo(StatementFile item) =>
        Task.FromResult(new CloudFileInfo("", "", ""));
}

public class CloudFileMigrationSimple
{
    private readonly IAwsService _awsService;
    private readonly IFileReader _fileReader;

     
    public CloudFileMigrationSimple(IAwsService awsService, IFileReader fileReader)
    {
        _awsService = awsService;
        _fileReader = fileReader;
        Stream x;
    }

    // A Template Method  or Invariant Trait of the Pattern
    public async Task Migrate<T>(Func<Task<IEnumerable<T>>> getItems, CloudFileInfo cloudFileInfo,
        Func<T, Task<bool>> updateItem)
    {
        Func<int, int> xxx;
        var items = await getItems();
        await Task.WhenAll(items.Select(MigrateItem));

        async Task MigrateItem(T item)
        {
            await UploadToAws3(cloudFileInfo);
            var hasUpdateItemSucceeded = await updateItem(item);
            if (!hasUpdateItemSucceeded)
                //handle failure
                await DeleteFromAws3(cloudFileInfo);
        }
    }

    private Task UploadToAws3(CloudFileInfo cloudFileInfo) => Task.CompletedTask; //Do the work
    private Task DeleteFromAws3(CloudFileInfo cloudFileInfo) => Task.CompletedTask; // Do the work
}

public static class Client
{
    public static async Task Run()
    {
        CloudFileMigrationSimple migrationSimple = new(default, default);
        await migrationSimple.Migrate(() => Task.FromResult(Enumerable.Empty<StatementFile>()), default,
            (item) => Task.FromResult(true));
        
        await migrationSimple.Migrate(() => Task.FromResult(Enumerable.Empty<AssetFile>()), default, UpdateAssetFile);

        Task<bool> UpdateAssetFile(AssetFile item)
        {
            return Task.FromResult<bool>(true);
        }
    }
}