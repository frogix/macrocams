using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace myCamViewer
{
    class VideoDecoder
    {
        // JPEG markers
        const byte picMarker = 0xFF;
        const byte picStart = 0xD8;
        const byte picEnd = 0xD9;

        /// <summary>
        /// Start a MJPEG on a http stream
        /// </summary>
        /// <param name="action">Delegate to run at each frame</param>
        /// <param name="url">url of the http stream (only basic auth is implemented)</param>
        /// <param name="token">cancellation token used to cancel the stream parsing</param>
        /// <param name="chunkMaxSize">Max chunk byte size when reading stream</param>
        /// <param name="frameBufferSize">Maximum frame byte size</param>
        /// <returns></returns>
        public async static Task StartAsync(Action<Image> action, string url, CancellationToken? token = null, int chunkMaxSize = 1024, int frameBufferSize = 1024 * 1024)
        {
            var tok = token ?? CancellationToken.None;

            using (var cli = new HttpClient())
            {
                using (var stream = await cli.GetStreamAsync(url).ConfigureAwait(false))
                {
                    int frameIndex = 0; // index of current frame byte
                    bool inPicture = false; 
                    byte current = 0x00;
                    byte previous = 0x00;

                    var chunk = new byte[chunkMaxSize];

                    // byte array that will contain picture eventually
                    var frameBytes = new byte[frameBufferSize];

                    // Filling the stream until CancellationToken is used
                    while (true)
                    {
                        var streamLength = await stream.ReadAsync(chunk, 0, chunkMaxSize, tok).ConfigureAwait(false);
                        parseStreamBuffer(action, frameBytes, ref frameIndex, streamLength, chunk, ref inPicture, ref previous, ref current);
                    };
                }
            }
        }

        // Depending on the position on buffer decide either to try parse image or search for JPEG start
        static void parseStreamBuffer(Action<Image> action, byte[] frameBytes, ref int frameIndex, int streamLength, byte[] chunk, ref bool inPicture, ref byte previous, ref byte current)
        {
            var idx = 0;
            while (idx < streamLength)
            {
                if (inPicture)
                {
                    parsePicture(action, frameBytes, ref frameIndex, ref streamLength, chunk, ref idx, ref inPicture, ref previous, ref current);
                }
                else
                {
                    searchPicture(frameBytes, ref frameIndex, ref streamLength, chunk, ref idx, ref inPicture, ref previous, ref current);
                }
            }
        }

        // Look for JPEG start byte sequence
        static void searchPicture(byte[] frameBytes, ref int frameIndex, ref int streamLength, byte[] chunk, ref int idx, ref bool inPicture, ref byte previous, ref byte current)
        {
            do
            {
                previous = current;
                current = chunk[idx++];

                // JPEG picture start
                if (previous == picMarker && current == picStart)
                {
                    frameIndex = 2;
                    frameBytes[0] = picMarker;
                    frameBytes[1] = picStart;
                    inPicture = true;
                    return;
                }
            } while (idx < streamLength);
        }

        // Fill image bytes until a JPEG end is reach.
        static void parsePicture(Action<Image> action, byte[] frameBytes, ref int frameIndex, ref int streamLength, byte[] chunk, ref int idx, ref bool inPicture, ref byte previous, ref byte current)
        {
            do
            {
                previous = current;
                current = chunk[idx++];
                frameBytes[frameIndex++] = current;

                // JPEG end
                if (previous == picMarker && current == picEnd)
                {
                    Image img = null;

                    using (var s = new MemoryStream(frameBytes, 0, frameIndex))
                    {
                        try
                        {
                            img = Image.FromStream(s);
                        }
                        catch
                        {
                            // Ignore wrongly parsed images
                        }
                    }

                    Task.Run(() => action(img));
                    inPicture = false;
                    return;
                }
            } while (idx < streamLength);
        }
    }
}
