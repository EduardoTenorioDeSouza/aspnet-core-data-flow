using System.Threading.Tasks.Dataflow;

namespace BufferBlockAndActionBlock.App
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var buffer = new BufferBlock<int>(new DataflowBlockOptions
            {
                BoundedCapacity = 10
            });
            var action = new ActionBlock<int>(i => Console.WriteLine($"Consumer: {i}"));

            // ligar com propagação de conclusão
            buffer.LinkTo(action, new DataflowLinkOptions { PropagateCompletion = true });

            for (var i = 1; i <= 100; i++)
            {
                await buffer.SendAsync(i);
                Console.WriteLine($"Producer: {i}");
            }
            buffer.Complete();
            await action.Completion;
        }
    }
}
