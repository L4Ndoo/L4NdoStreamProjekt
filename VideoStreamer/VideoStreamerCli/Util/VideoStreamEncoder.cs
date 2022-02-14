using System;
using System.Collections.Generic;
using System.Drawing;
using FFmpeg.AutoGen;

namespace VideoStreamer.Cli.Util
{
    public sealed unsafe class VideoStreamEncoder : IDisposable
    {
        private readonly AVCodec* _pCodec;
        private readonly AVCodecContext* _pCodecContext;
        private readonly Stream _stream;

        public VideoStreamEncoder(Stream stream, int fps, Size frameSize)
        {
            _stream = stream;

            var codecId = AVCodecID.AV_CODEC_ID_H264;
            _pCodec = ffmpeg.avcodec_find_encoder(codecId);
            if (_pCodec == null) throw new InvalidOperationException("Codec not found.");

            _pCodecContext = ffmpeg.avcodec_alloc_context3(_pCodec);
            _pCodecContext->width = frameSize.Width;
            _pCodecContext->height = frameSize.Height;
            _pCodecContext->time_base = new AVRational { num = 1, den = fps };
            _pCodecContext->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUV420P;
            ffmpeg.av_opt_set(_pCodecContext->priv_data, "preset", "veryfast", 0);

            ffmpeg.avcodec_open2(_pCodecContext, _pCodec, null).ThrowExceptionIfError();
        }

        public void Dispose()
        {
            ffmpeg.avcodec_close(_pCodecContext);
            ffmpeg.av_free(_pCodecContext);
        }

        public void Encode(AVFrame frame)
        {
            var pPacket = ffmpeg.av_packet_alloc();

            try
            {
                int error;

                do
                {
                    ffmpeg.avcodec_send_frame(_pCodecContext, &frame).ThrowExceptionIfError();
                    ffmpeg.av_packet_unref(pPacket);
                    error = ffmpeg.avcodec_receive_packet(_pCodecContext, pPacket);
                } while (error == ffmpeg.AVERROR(ffmpeg.EAGAIN));

                error.ThrowExceptionIfError();

                using var packetStream = new UnmanagedMemoryStream(pPacket->data, pPacket->size);
                packetStream.CopyTo(_stream);
            }
            finally
            {
                ffmpeg.av_packet_free(&pPacket);
            }
        }
    }
}
