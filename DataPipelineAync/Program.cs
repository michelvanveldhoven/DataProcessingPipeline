// See https://aka.ms/new-console-template for more information
// See https://deniskyashif.com/2020/01/07/csharp-channels-part-3/
using System.Diagnostics;
using System.Threading.Channels;
using static System.Console;

//
Action<string> WriteLineWithTime =
        (str) => WriteLine($"[{DateTime.UtcNow.ToLongTimeString()}] {str}");

Console.WriteLine("Hello, World!");

var sw = new Stopwatch();
sw.Start();
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

// Try with a large folder e.g. node_modules
var fileSource = GetFilesRecursively(".", cts.Token);
var sourceCodeFiles = FilterByExtension(fileSource, new HashSet<string> { ".cs", ".json", ".xml" });
var (counter, errors) = GetLineCount(sourceCodeFiles);
// Distribute the file reading stage amongst several workers
var (counter2, errors2) = CountLinesAndMerge(Split(sourceCodeFiles, 5));

var totalLines = 0;
await foreach (var item in counter.ReadAllAsync())
{
    WriteLineWithTime($"{item.file.FullName} {item.lines}");
    totalLines += item.lines;
}
WriteLine($"Total lines: {totalLines}");

await foreach (var errMessage in errors.ReadAllAsync())
    WriteLine(errMessage);

sw.Stop();
WriteLine(sw.Elapsed);



//
static ChannelReader<string> GetFilesRecursively(string root, CancellationToken token = default)
{
    var output = Channel.CreateUnbounded<string>();

    async Task WalkDir(string path)
    {
        if (token.IsCancellationRequested)
            throw new OperationCanceledException();

        foreach (var file in Directory.GetFiles(path))
            await output.Writer.WriteAsync(file, token);

        var tasks = Directory.GetDirectories(path).Select(WalkDir);
        await Task.WhenAll(tasks.ToArray());
    }

    Task.Run(async () =>
    {
        try
        {
            await WalkDir(root);
        }
        catch (OperationCanceledException) { WriteLine("Cancelled."); }
        finally { output.Writer.Complete(); }
    });

    return output;
}

static ChannelReader<FileInfo> FilterByExtension(
       ChannelReader<string> input, HashSet<string> exts)
{
    var output = Channel.CreateUnbounded<FileInfo>();
    Task.Run(async () =>
    {
        await foreach (var file in input.ReadAllAsync())
        {
            var fileInfo = new FileInfo(file);
            if (exts.Contains(fileInfo.Extension))
                await output.Writer.WriteAsync(fileInfo);
        }

        output.Writer.Complete();
    });

    return output;
}

static (ChannelReader<(FileInfo file, int lines)> output, ChannelReader<string> errors)
        GetLineCount(ChannelReader<FileInfo> input)
{
    var output = Channel.CreateUnbounded<(FileInfo, int)>();
    var errors = Channel.CreateUnbounded<string>();

    Task.Run(async () =>
    {
        await foreach (var file in input.ReadAllAsync())
        {
            var lines = CountLines(file);
            if (lines == 0)
                await errors.Writer.WriteAsync($"[Error] Empty file {file}");
            else
                await output.Writer.WriteAsync((file, lines));
        }
        output.Writer.Complete();
        errors.Writer.Complete();
    });

    return (output, errors);
}

static int CountLines(FileInfo file)
{
    using var sr = new StreamReader(file.FullName);
    var lines = 0;

    while (sr.ReadLine() != null)
        lines++;

    return lines;
}

static (ChannelReader<(FileInfo file, int lines)> output, ChannelReader<string> errors)
        CountLinesAndMerge(IList<ChannelReader<FileInfo>> inputs)
{
    var output = Channel.CreateUnbounded<(FileInfo file, int lines)>();
    var errors = Channel.CreateUnbounded<string>();

    Task.Run(async () =>
    {
        async Task Redirect(ChannelReader<FileInfo> input)
        {
            await foreach (var file in input.ReadAllAsync())
            {
                var lines = CountLines(file);
                if (lines == 0)
                    await errors.Writer.WriteAsync($"[Error] Empty file {file}");
                else
                    await output.Writer.WriteAsync((file, lines));
            }
        }

        await Task.WhenAll(inputs.Select(Redirect).ToArray());
        output.Writer.Complete();
        errors.Writer.Complete();
    });

    return (output, errors);
}

static ChannelReader<T> Merge<T>(params ChannelReader<T>[] inputs)
{
    var output = Channel.CreateUnbounded<T>();

    Task.Run(async () =>
    {
        async Task Redirect(ChannelReader<T> input)
        {
            await foreach (var item in input.ReadAllAsync())
                await output.Writer.WriteAsync(item);
        }

        await Task.WhenAll(inputs.Select(i => Redirect(i)).ToArray());
        output.Writer.Complete();
    });

    return output;
}

static IList<ChannelReader<T>> Split<T>(ChannelReader<T> ch, int n)
{
    var outputs = new Channel<T>[n];

    for (int i = 0; i < n; i++)
        outputs[i] = Channel.CreateUnbounded<T>();

    Task.Run(async () =>
    {
        var index = 0;
        await foreach (var item in ch.ReadAllAsync())
        {
            await outputs[index].Writer.WriteAsync(item);
            index = (index + 1) % n;
        }

        foreach (var ch in outputs)
            ch.Writer.Complete();
    });

    return outputs.Select(ch => ch.Reader).ToArray();
}