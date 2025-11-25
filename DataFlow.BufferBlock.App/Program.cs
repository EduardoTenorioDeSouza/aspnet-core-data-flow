using System.Threading.Tasks.Dataflow;

namespace BufferBlock.App
{
    public class Program
    {
        public static async Task Main(string[] args)
        {

            var dft = new DataFlowTest();

            var d1 = dft.ConsumerAsync();

            var d2 = dft.ProducerAsync(Enumerable.Range(1,100));

           await Task.WhenAll(d1, d2);

           Console.ReadKey();
        }
    }

    public class DataFlowTest
    {
        private readonly BufferBlock<int> _buffer = new(new DataflowBlockOptions { BoundedCapacity = 10 });

        public async Task ProducerAsync(IEnumerable<int> items)
        {
            foreach (var item in items)
            {
                await _buffer.SendAsync(item); // respeita BoundedCapacity
                Console.WriteLine($"Producer: {item}");
            }
            _buffer.Complete(); // sinaliza fim
        }

        public async Task ConsumerAsync()
        {
            while (await _buffer.OutputAvailableAsync()) // enquanto houver ou até Complete
            {
                var item = await _buffer.ReceiveAsync();
                Console.WriteLine($"Consumer: {item}");
            }

            await _buffer.Completion;

            // ou: await buffer.Completion; se você preferir checar a Task Completion
        }
    }
}
