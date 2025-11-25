using System.Threading.Tasks.Dataflow;

namespace BatchBlockActionBlock.App
{
    internal class Program
    {
        static async Task Main()
        {
            var batch = new BatchBlock<string>(3);

            var action = new ActionBlock<string[]>(async batch =>
            {
                Console.WriteLine($"Processing in batch ({batch.Length}): {string.Join("|", batch)}");
                
                await Task.Delay(200);

            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 2 });

            batch.LinkTo(action, new DataflowLinkOptions { PropagateCompletion = true });

            string[] items = ["A", "B", "C", "D", "E", "F", "G"];

            foreach (var item in items)
            {
                await Task.Delay(50);
                await batch.SendAsync(item);
                Console.WriteLine($"Send {item}");
            }

            batch.Complete();
            await action.Completion;

            Console.WriteLine("End.");
        }
    }
}
