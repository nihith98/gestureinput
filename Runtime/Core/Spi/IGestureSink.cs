namespace GestureInput.Core
{
    /// <summary>Where recognizers deliver their events during <see cref="IGestureRecognizer.Process"/>.</summary>
    public interface IGestureSink
    {
        void Emit(in GestureEvent e);
    }
}
