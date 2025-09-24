// See https://aka.ms/new-console-template for more information
using System.Text;
using ComponentAce.Compression.Libs.zlib;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            // Parse CLI arguments
            var inputArg = args.FirstOrDefault(a => a.StartsWith("--input="));
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
            // Initialize zlib stream
            var zStream = new ZStream();
            zStream.inflateInit(-15);

            // Decompress
            var decompressedHex = DecompressBytes(zStream, bytes);

            // Print decompressed result
            Console.WriteLine($"Result: {decompressedHex}");
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
    static byte[] InputToBytes(string byteInput)
    {
        // Remove spaces if user pasted a spaced string
        byteInput = byteInput.Replace(" ", "").Trim();

        if (byteInput.Length % 2 != 0)
            throw new Exception("Invalid hex string length.");

        var bytes = new byte[byteInput.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(byteInput.Substring(i * 2, 2), 16);
        }

        return bytes;
    }

    /// <summary>
    /// Decompresses the byte array using zlib and returns decompressed data as hex string.
    /// </summary>
    static string DecompressBytes(ZStream zStream, byte[] bytes)
    {
        var decompressedBytes = new byte[65536];
        
        zStream.next_in = bytes;
        zStream.next_in_index = 0;
        zStream.next_out = decompressedBytes;
        zStream.next_out_index = 0;
        zStream.avail_in = bytes.Length;
        zStream.avail_out = decompressedBytes.Length;

        var ret = zStream.inflate(zlibConst.Z_SYNC_FLUSH);

        if (ret != zlibConst.Z_OK && ret != zlibConst.Z_NO_FLUSH)
            throw new Exception($"zlib inflate failed: {ret}");

        int decompressedLength = zStream.next_out_index;

        if (decompressedLength <= 0)
            throw new Exception("No data was decompressed.");

        // Slice only the valid bytes
        var resultBytes = decompressedBytes.Take(decompressedLength).ToArray();
        
        return BitConverter.ToString(resultBytes).Replace("-", " ");
    }
}