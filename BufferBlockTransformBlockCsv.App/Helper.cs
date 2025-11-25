using System.Globalization;
using System.Text.RegularExpressions;

namespace BufferBlockTransformBlockCsv.App;

public class Helper
{
    private static DateTime ConvertToDateTimeUtc(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Data inválida ou vazia", nameof(input));

        // Remove espaços extras
        input = input.Trim();

        // Se não tiver "UTC", adiciona
        if (!input.EndsWith("UTC", StringComparison.OrdinalIgnoreCase))
            input += " UTC";

        // Verifica se há fração de segundo
        if (Regex.IsMatch(input, @"\.\d+"))
        {
            // Padroniza a fração para 6 dígitos
            input = Regex.Replace(input, @"\.(\d+)\sUTC", match =>
            {
                string fraction = match.Groups[1].Value.PadRight(6, '0'); // completa até 6 dígitos
                return "." + fraction + " UTC";
            });

            // Converte incluindo fração
            return DateTime.ParseExact(
                input,
                "yyyy-MM-dd HH:mm:ss.ffffff 'UTC'",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
            );
        }
        else
        {
            // Converte sem fração
            return DateTime.ParseExact(
                input,
                "yyyy-MM-dd HH:mm:ss 'UTC'",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
            );
        }
    }
}