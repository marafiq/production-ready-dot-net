using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

async Task<int> Divide(int delayMilliSeconds)
{
    try
    {
        await Task.Delay(delayMilliSeconds * 1000); //9000 - 9 seconds
        var x = delayMilliSeconds % 2;
        return delayMilliSeconds / x;
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        return 0;
    }
}

var stopwatch = System.Diagnostics.Stopwatch.StartNew();
var tasks = Enumerable.Range(1, 10).Select(Divide).ToList();
await Task.Delay(3000); // Delay 3 seconds
// Tasks are already running, await frees current thread and then results are posted based on context.
foreach (var task in tasks) Console.WriteLine($"Task: {task.Id} with status : {task.Status}");
var whenAllTask = Task.WhenAll(tasks);
try
{
    var values = await whenAllTask.ConfigureAwait(false);

    //This code will never hit as exception will be thrown.
    foreach (var value in values)
    {
        Console.WriteLine(value);
    }
}
// Exceptions will be caught after all tasks are done, but `e` contains only the first exception.
catch (Exception e)
{
    Console.WriteLine($"catch block got hit after {stopwatch.ElapsedMilliseconds / 1000} seconds.");
    Console.WriteLine(e);
    if (whenAllTask.Exception is { } aggregateException) // By not doing this you will not get all exceptions
        aggregateException.Flatten().Handle((ae) =>
        {
            Console.WriteLine(ae);
            return true;
        });
}

//Only get values for completed ones and ignore faulted ones.
foreach (var task in tasks.Where(x => x.Status == TaskStatus.RanToCompletion))
    Console.WriteLine($"Results are : {task.Result}");

/*Task<bool> PrintName(string name)
{
    try
    {
        if (name is null || name=="a")
        {
            throw new AggregateException("Agg");
        }
        Console.WriteLine(name.ToString());
        return Task.FromResult(true);
    }
    catch (AggregateException e)
    {
        Console.WriteLine(e);
        return Task.FromResult(false);
    }
}

static Task ReadFile()
{
    return File.ReadAllTextAsync("c:\\");
    
    
}
static async Task OnlyTheFirstOne()
{

    List<Task> tasks = new List<Task>();
    for (int i = 0; i < 10; i++)
    {
        tasks.Add(ReadFile());
    }  

    try
    {
        Task allTasks = Task.WhenAll(tasks);
        await allTasks;
    }
    catch (AggregateException aggregateException)
    {
        Console.WriteLine(aggregateException);
    }
    /*catch (Exception e)
    {
        if (e is AggregateException aggregateException )
        {
            foreach (var innerException in aggregateException.InnerExceptions)
            {
                Console.WriteLine(innerException);
            }
        }
        Console.WriteLine(e);
    }#1#
    
}

await OnlyTheFirstOne();
string[] names = new [] { "a", "b", null};
var nameTasks=names.Select(PrintName);

var allTasks = new List<Task<bool>>();
allTasks.AddRange(nameTasks);
var t = Task.WhenAll(allTasks);
try
{
    await t;
}
catch (Exception e)
{
    Console.WriteLine(e +"catch");
}*/
/*await Example.MainAsync();
Console.WriteLine("Press any key to exit!");
Console.ReadKey();

public static class Example
{
    public static async Task MainAsync()
    {
        var task1 = Task.Run(async () =>
        {
            await Task.Delay(10000);
            //throw new CustomException("This exception is expected! But not caught.");
            var i = 0;
            Console.WriteLine(100 / i);
        });
        var task2 = Task.Run(() =>
        {
            //await Task.Delay(15000);
            //throw new CustomException("This exception is expected! But not caught.");
            Console.WriteLine("I will throw after 15 seconds");
            var i = 0;
            Console.WriteLine(100 / i);
        });
        var t1 = Task.WhenAll(task1, task2);
        try
        {
            await t1;
        }
        catch (Exception e)
        {
            if (t1.Exception is { } aggregateException)
            {
                aggregateException.Handle((exception => true));

                foreach (var innerException in aggregateException.Flatten().InnerExceptions)
                {
                    //Console.WriteLine(innerException);
                }
            }

            Console.WriteLine(e);
        }
        /*catch (AggregateException ae)
        {
            // Call the Handle method to handle the custom exception,
            // otherwise rethrow the exception.
            ae.Handle(ex => { if (ex is CustomException)
                    Console.WriteLine(ex.Message);
                //return ex is CustomException;
                return true;
            });
        }#1#
    }
}

public class CustomException : Exception
{
    public CustomException(String message) : base(message)
    {
    }
}*/


class ReadThroughMemoryCache
{
    private readonly IDistributedCache _distributedCache;

    public ReadThroughMemoryCache(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public async Task<T?> GetAsync<T>(string key, Func<T> retrieveFromDataStore, TimeSpan expiredAfter,
        CancellationToken cancellationToken = default)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        var item = await _distributedCache.GetStringAsync(key, token: cancellationToken);

        if (item is { })
        {
            return JsonSerializer.Deserialize<T>(item);
        }

        var dbItem = retrieveFromDataStore();
        var dbItemSerialized = JsonSerializer.Serialize(dbItem);
        await _distributedCache.SetStringAsync(key, dbItemSerialized,
            new DistributedCacheEntryOptions() { SlidingExpiration = expiredAfter }, cancellationToken);

        return dbItem;
    }
}

record Course(int Id, string CourseName);

class StudentCoursesQuery
{
    private readonly ReadThroughMemoryCache _readThroughMemoryCache;

    public StudentCoursesQuery(ReadThroughMemoryCache readThroughMemoryCache)
    {
        _readThroughMemoryCache = readThroughMemoryCache;
    }

    async Task<IEnumerable<Course>?> GetEnrolledCourses(int studentId)
    {
        return await _readThroughMemoryCache.GetAsync<IEnumerable<Course>>($"Student_{studentId}_Courses",
            RetrieveFromDataStore, TimeSpan.MaxValue);

        IEnumerable<Course> RetrieveFromDataStore()
        {
            return new List<Course>();
        }
    }
}


class StudentEnrollCommand
{
    private readonly IDistributedCache _distributedCache;

    public StudentEnrollCommand(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    async Task<bool> Enroll(int studentId, int courseId)
    {
        await Task.Delay(1000); // consider this is database query executing

        await _distributedCache.RemoveAsync($"Student_{studentId}_Courses"); //delete the entry from cache

        return true;
    }
}


record CacheKey(char Prefix, string UniqueKey, char Postfix)
{
    public override string ToString()
    {
        return $"{Prefix}_{UniqueKey}_{Postfix}";
    }
}