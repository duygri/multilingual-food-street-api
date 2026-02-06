using System.Net;

namespace FoodStreet.Client.Services
{
    public class ProgressableStreamContent : HttpContent
    {
        private readonly HttpContent _content;
        private readonly int _bufferSize;
        private readonly Action<long, long> _progress;

        public ProgressableStreamContent(HttpContent content, Action<long, long> progress) : this(content, 81920, progress) { }

        public ProgressableStreamContent(HttpContent content, int bufferSize, Action<long, long> progress)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }

            _content = content;
            _bufferSize = bufferSize;
            _progress = progress;

            foreach (var header in content.Headers)
            {
                Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            var buffer = new byte[_bufferSize];
            long totalBytes = _content.Headers.ContentLength ?? -1;
            long uploadedBytes = 0;

            using (var input = await _content.ReadAsStreamAsync())
            {
                while (true)
                {
                    var length = await input.ReadAsync(buffer, 0, buffer.Length);
                    if (length <= 0)
                    {
                        break;
                    }

                    uploadedBytes += length;
                    _progress?.Invoke(uploadedBytes, totalBytes);

                    await stream.WriteAsync(buffer, 0, length);
                }
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _content.Headers.ContentLength ?? -1;
            return length != -1;
        }
    }
}
