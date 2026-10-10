namespace ScreenToGif.Util.Codification.Gif.Decoder;

[Serializable]
public class GifDecoderException : Exception
{
    public GifDecoderException() { }

    public GifDecoderException(string message) : base(message) { }

    public GifDecoderException(string message, Exception inner) : base(message, inner) { }
}