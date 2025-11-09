// See https://aka.ms/new-console-template for more information
using ComponentAce.Compression.Libs.zlib;

class Program
{
    private static readonly ZStream zStream = new();
    static void Main(string[] args)
    {
        try
        {
            zStream.inflateInit(-15);

            // Parse CLI arguments
            var inputArg = args.FirstOrDefault(arg => arg.StartsWith("--input="));

            if (inputArg == null)
            {
                Console.WriteLine("Usage: program --input=HEXSTRING");
                return;
            }

            var hexInput = inputArg.Substring("--input=".Length);

            if (string.IsNullOrWhiteSpace(hexInput))
                throw new Exception("Empty input cannot be decompressed.");

            // Convert input to byte array
            var bytes = InputToBytes(hexInput);

            // Decompress
            var decompressedHex = DecompressBytes(bytes);

            zStream.deflateEnd();

            // Print decompressed result
            Console.WriteLine(decompressedHex);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);

            File.AppendAllText("error.log", $"{DateTime.Now} - {e.Message}\n");
        }
    }

    /// <summary>
    /// Converts the raw hex string into a byte array.
    /// Supports both space-separated and continuous hex strings.
    /// </summary>
    static byte[] InputToBytes(string hexInput)
    {
        // Remove spaces if user pasted a spaced string
        var hexInputSantized = hexInput.Replace(" ", "").Trim();

        if (hexInputSantized.Length % 2 != 0)
            throw new Exception("Invalid hex string length.");

        var bytes = new byte[hexInputSantized.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hexInputSantized.Substring(i * 2, 2), 16);
        }

        return bytes;
    }

    /// <summary>
    /// Decompresses the byte array using zlib and returns decompressed data as hex string.
    /// </summary>
    static string DecompressBytes(byte[] bytes)
    {
        var inBuffer = new byte[bytes.Length];
        var outBuffer = new byte[ushort.MaxValue];

        Array.Copy(bytes, 0, inBuffer, 0, bytes.Length);
        
        zStream.next_in = inBuffer;
        zStream.next_in_index = 0;
        zStream.avail_in = inBuffer.Length;
        zStream.next_out = outBuffer;
        zStream.next_out_index = 0;
        zStream.avail_out = outBuffer.Length;

        var ret = zStream.inflate(zlibConst.Z_SYNC_FLUSH);

        if (ret != zlibConst.Z_OK && ret != zlibConst.Z_NO_FLUSH)
            throw new Exception($"zlib inflate failed: {ret}");

        int decompressedLength = zStream.next_out_index;

        if (decompressedLength <= 0)
            throw new Exception("No data was decompressed.");

        // Slice only the valid bytes
        var resultBytes = outBuffer.Take(decompressedLength).ToArray();
        
        return string.Join(" ", resultBytes.Select(b => b.ToString("X2")));
    }
}