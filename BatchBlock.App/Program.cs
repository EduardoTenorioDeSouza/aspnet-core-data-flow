using System.Threading.Tasks.Dataflow;

namespace BatchBlock.App
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var batch = new BatchBlock<int>(5);

            for (int i = 1; i <= 12; i++)
            {
                await batch.SendAsync(i);
                Console.WriteLine($"Posted {i}");
            }

            batch.Complete();

            //// 4) Consumimos TODOS os lotes usando ReceiveAllAsync()
            // Só libera quando o batch.Complete() é chamado.
            //await foreach (var lote in batch.ReceiveAllAsync())
            //{
            //    Console.WriteLine($"Batch received: {string.Join(", ", lote)}");
            //}

            //Retorna sempre que lote estiver pronto
            while (await batch.OutputAvailableAsync())
            {
                var batchReceive = await batch.ReceiveAsync();
                Console.WriteLine($"Received batch: {string.Join(", ", batchReceive)}");
            }

            await batch.Completion;

            Console.WriteLine("End.");
        }
    }
}
