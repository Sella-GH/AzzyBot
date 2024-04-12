using System.IO;
using System.Net.Http;

namespace AzzyBot.Modules.Core;

internal class CoreWebRequestsStream(HttpResponseMessage response) : Stream
{
    private readonly HttpResponseMessage Response = response;
    private readonly Stream Stream = response.Content.ReadAsStreamAsync().Result;

    public override bool CanRead => Stream.CanRead;
    public override bool CanSeek => Stream.CanSeek;
    public override bool CanWrite => Stream.CanWrite;
    public override int Read(byte[] buffer, int offset, int count) => Stream.Read(buffer, offset, count);
    public override long Length => Stream.Length;
    public override long Seek(long offset, SeekOrigin origin) => Stream.Seek(offset, origin);
    public override void Flush() => Stream.Flush();
    public override void SetLength(long value) => Stream.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => Stream.Write(buffer, offset, count);

    public override long Position
    {
        get => Stream.Position;
        set => Stream.Position = value;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Stream.Dispose();
            Response.Dispose();
        }

        base.Dispose(disposing);
    }
}
