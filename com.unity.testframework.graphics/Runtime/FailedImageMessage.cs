using System;
using System.IO;
using System.Text;

public class FailedImageMessage
{
    public static Guid MessageId { get; } = new Guid("40c7a8e2-ad5d-475f-8119-af022a13b84c");

    public string PathName { get; set; }

    public string ImageName { get; set; }

    public byte[] ExpectedImage { get; set; }

    public byte[] ActualImage { get; set; }

    public byte[] DiffImage { get; set; }

    public byte[] Serialize()
    {
        int capacity = sizeof(int) * 5 + PathName?.Length ?? 0 + ImageName?.Length ?? 0 + ExpectedImage?.Length ?? 0 + ActualImage?.Length ?? 0 + DiffImage?.Length ?? 0;
        using (var memoryStream = new MemoryStream(capacity))
        {
            using (var writer = new BinaryWriter(memoryStream))
            {
                writer.WriteString(PathName);
                writer.WriteString(ImageName);
                writer.WriteBytes(ExpectedImage);
                writer.WriteBytes(ActualImage);
                writer.WriteBytes(DiffImage);
            }

            return memoryStream.ToArray();
        }
    }

    public static FailedImageMessage Deserialize(byte[] data)
    {
        using (var messageStream = new MemoryStream(data))
        {
            using (var reader = new BinaryReader(messageStream))
            {
                return new FailedImageMessage
                {
                    PathName = reader.GetString(),
                    ImageName = reader.GetString(),
                    ExpectedImage = reader.GetBytes(),
                    ActualImage = reader.GetBytes(),
                    DiffImage = reader.GetBytes(),
                };
            }
        }
    }
}

public static class BinaryWriterExtensions
{
    public static void WriteString(this BinaryWriter writer, string value, Encoding encoding = null)
    {
        if (value == null)
        {
            writer.Write(-1);
            return;
        }

        encoding = encoding ?? Encoding.UTF8;
        var data = encoding.GetBytes(value);
        writer.WriteBytes(data);
    }

    public static void WriteBytes(this BinaryWriter writer, byte[] value)
    {
        if (value == null)
        {
            writer.Write(-1);
            return;
        }

        writer.Write(value.Length);
        writer.Write(value);
    }
}

public static class BinaryReaderExtensions
{
    public static string GetString(this BinaryReader reader, Encoding encoding = null)
    {
        encoding = encoding ?? Encoding.UTF8;
        int length = reader.ReadInt32();
        if (length < 0)
        {
            return null;
        }

        return encoding.GetString(reader.ReadBytes(length));
    }

    public static byte[] GetBytes(this BinaryReader reader)
    {
        int length = reader.ReadInt32();
        if (length < 0)
        {
            return null;
        }

        return reader.ReadBytes(length);
    }
}
