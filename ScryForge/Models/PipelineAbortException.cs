namespace ScryForge.Models;

public class PipelineAbortException : Exception
{
    public PipelineAbortException(string message) : base(message)
    {
    }
}
