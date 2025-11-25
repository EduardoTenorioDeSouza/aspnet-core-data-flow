using System.Threading.Tasks.Dataflow;

namespace BufferBlockTransformBlockCsv.App
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var csv = new CsvDataflow();
            await csv.ProcessCsvAsync("", CancellationToken.None);
        }
    }

    public class CsvDataflow
    {
        public async Task ProcessCsvAsync(string path, CancellationToken cancellationToken = default)
        {
            var deadLetter = new BufferBlock<string>(
                new DataflowBlockOptions { BoundedCapacity = 5000 });
            
            var deadLetterWriter = new ActionBlock<string>(linha =>
            {
                Console.WriteLine($"Linha inválida enviada ao dead-letter: {linha}");
            });

            deadLetter.LinkTo(deadLetterWriter, new DataflowLinkOptions { PropagateCompletion = true });
    
            var buffer = new BufferBlock<string>(
                new DataflowBlockOptions
                {
                    BoundedCapacity = 8000,
                    CancellationToken = cancellationToken
                });
     
            var parse = new TransformBlock<string, OutputData?>(linha =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var columns = linha.Split(',');

                if (columns.Length != 3)
                {
                    deadLetter.Post(linha);
                    return null;
                }
                
                if (!int.TryParse(columns[0], out int id) ||
                    !decimal.TryParse(columns[2], out decimal valor))
                {
                    deadLetter.Post(linha);
                    return null;
                }
                
                return new OutputData
                {
                    Id = id,
                    Amount = valor
                };

            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                EnsureOrdered = false,
                BoundedCapacity = Environment.ProcessorCount * 4,
                CancellationToken = cancellationToken
            });
      
            var filters = new TransformManyBlock<OutputData?, OutputData>(line =>
            {
                if (line == null)
                    return [];

                return [line];
            },
            new ExecutionDataflowBlockOptions
            {
                CancellationToken = cancellationToken,
                EnsureOrdered = false,
                BoundedCapacity = 2000
            });

            var batch = new BatchBlock<OutputData>(
                batchSize: 200,
                new GroupingDataflowBlockOptions
                {
                    BoundedCapacity = 2000,
                    CancellationToken = cancellationToken
                });
   
            var bulkInsert = new ActionBlock<OutputData[]>(async lote =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Aqui entra SqlBulkCopy, Dapper (em transação), EF Core, etc.
                // Simulação de latência do DB:
                await Task.Delay(40, cancellationToken);

                Console.WriteLine($"[BulkInsert] inserted {lote.Length} records");
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 4,
                BoundedCapacity = 2000,
                CancellationToken = cancellationToken
            });
 
            buffer.LinkTo(parse, new DataflowLinkOptions { PropagateCompletion = true });
            parse.LinkTo(filters, new DataflowLinkOptions { PropagateCompletion = true });
            filters.LinkTo(batch, new DataflowLinkOptions { PropagateCompletion = true });
            batch.LinkTo(bulkInsert, new DataflowLinkOptions { PropagateCompletion = true });

            using var reader = new StreamReader(path);

            while (await reader.ReadLineAsync(cancellationToken) is { } lineCsv)
            {
                await buffer.SendAsync(lineCsv, cancellationToken);
            }
            
            buffer.Complete();
            deadLetter.Complete();
            
            await bulkInsert.Completion;
            await deadLetterWriter.Completion;
        }
    }
}
