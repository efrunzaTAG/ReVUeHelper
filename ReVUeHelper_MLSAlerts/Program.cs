// See https://aka.ms/new-console-template for more information
using ReVUeHelper_MLSAlerts;

Console.WriteLine("Hello, World!");


//items to watch MLS for:
//- missed import: check if expected imports (daily and intra-day) did not come in as expected
//- low import: check if expected imports (daily and intra-day) seems lower than same day in prev weeks' averages
//- boards with only Stale imports (or ration between stale and new count is larged than average FOR that board)
//- low volume in a board (have a running avg over the N biz days and see if last day's count is below or above this avg)
//- low volume overall during biz hours (5:30am to 4pm daily) - a 4pm alert to compare today with prev biz hour timeframes (avoid weekends)
//- missing boards: dynamic way to tell if a board existed in the past N days and did not get anything for it by a certain hour of the day
//- best fit trends?


Timer timer = new Timer(ExecuteTasks, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

Console.ReadLine();

static void ExecuteTasks(object state)
{
    // Task 1: Run every minute
    Task.Run(() => Task_MissedImport()).ContinueWith(_ => Console.WriteLine("Task_MissedImport completed"));

    // Task 2: Run every 3 minutes
    Task.Run(() => Task2()).ContinueWith(_ => Console.WriteLine("Task 2 completed"));

    // Task 3: Run every 7 minutes
    Task.Run(() => Task3()).ContinueWith(_ => Console.WriteLine("Task 3 completed"));

    // Add more tasks with different schedules as needed
}

static async Task Task_MissedImport()
{
    Console.WriteLine("Task_MissedImport running...");
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    //await Task.Delay(TimeSpan.FromSeconds(5));
    var res  = await Task.Run(() => {
        var db = new ReVUeDbProxy();
        var recentMlsImports = db.GetRecentMlsImports();
        return recentMlsImports;
    });
    

    


    stopwatch.Stop();
    Console.WriteLine($"Task_MissedImport completed. Elapsed Time: {stopwatch.Elapsed}");
}

static async Task Task2()
{
    Console.WriteLine("Task 2 running...");
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    await Task.Delay(TimeSpan.FromSeconds(13));
    stopwatch.Stop();
    Console.WriteLine($"Task 2 completed. Elapsed Time: {stopwatch.Elapsed}");
}

static async Task Task3()
{
    Console.WriteLine("Task 3 running...");
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    await Task.Delay(TimeSpan.FromSeconds(17));
    stopwatch.Stop();
    Console.WriteLine($"Task 3 completed. Elapsed Time: {stopwatch.Elapsed}");
}