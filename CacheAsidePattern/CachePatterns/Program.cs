using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["RedisConnectionString"];
});

builder.Services.AddScoped<ReadThroughDistributedCache>();
builder.Services.AddScoped<StudentCoursesQuery>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var serviceScope = app.Services.CreateScope();
var studentCoursesQuery = serviceScope.ServiceProvider.GetService<StudentCoursesQuery>();
app.MapGet("/students/{studentId}/enrollments",
    async (int studentId) => await studentCoursesQuery!.GetEnrolledCourses(studentId));
app.Run();

record Course(int Id, string CourseName);

class StudentCoursesQuery
{
    private readonly ReadThroughDistributedCache _readThroughDistributedCache;

    public StudentCoursesQuery(ReadThroughDistributedCache readThroughDistributedCache)
    {
        _readThroughDistributedCache = readThroughDistributedCache;
    }

    public async Task<IEnumerable<Course>> GetEnrolledCourses(int studentId)
    {
        return await _readThroughDistributedCache.GetAsync(new StudentCacheKey(studentId), RetrieveFromDataStore,
            TimeSpan.MaxValue) ?? Array.Empty<Course>();

        IEnumerable<Course> RetrieveFromDataStore()
        {
            var courses = new List<Course> { new(1, "CS") };
            return courses.Where(x => x.Id == studentId);
        }
    }
}

class ReadThroughDistributedCache
{
    private readonly IDistributedCache _distributedCache;

    public ReadThroughDistributedCache(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }
    
    public async Task<T?> GetAsync<T, TUniqueKey>(CacheKey<TUniqueKey> key, Func<T?> retrieveFromDataStore,
        TimeSpan expiredAfter, CancellationToken cancellationToken = default)
    {
        var cachedItem = await _distributedCache.GetAsync(key, cancellationToken);
        if (cachedItem is { })
        {
            return JsonSerializer.Deserialize<T>(new ReadOnlySpan<byte>(cachedItem))!;
        }

        var dbItem = retrieveFromDataStore();
        if (dbItem is null) return default;
        var dbItemSerialized = JsonSerializer.SerializeToUtf8Bytes(dbItem);
        await _distributedCache.SetAsync(key, dbItemSerialized,
            new DistributedCacheEntryOptions { SlidingExpiration = expiredAfter }, cancellationToken);
        return dbItem;
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
        // consider this is database query executing
        await Task.Delay(1000);

        var cacheKey = new StudentCacheKey(studentId);
        await _distributedCache.RemoveAsync(cacheKey); //delete the entry from cache

        return true;
    }
}

// To enforce key naming pattern, feel free to use string only.
abstract record CacheKey<TUniqueKey>(char Prefix, TUniqueKey UniqueKey, char Postfix)
{
    public static implicit operator string(CacheKey<TUniqueKey> studentCacheKey)
    {
        return studentCacheKey.ToString();
    }

    public override string ToString()
    {
        return $"{Prefix}_{UniqueKey}_{Postfix}";
    }
}

record StudentCacheKey(int StudentId) : CacheKey<int>('S', StudentId, 'C');