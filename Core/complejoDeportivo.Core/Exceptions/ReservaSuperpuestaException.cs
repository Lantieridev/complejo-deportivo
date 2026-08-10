namespace complejoDeportivo.Exceptions;

public class ReservaSuperpuestaException : Exception
{
    public ReservaSuperpuestaException() { }

    public ReservaSuperpuestaException(string message) : base(message) { }

    public ReservaSuperpuestaException(string message, Exception innerException) : base(message, innerException) { }
}
