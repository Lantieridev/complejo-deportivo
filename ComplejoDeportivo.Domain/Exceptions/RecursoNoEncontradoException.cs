namespace ComplejoDeportivo.Domain;

public class RecursoNoEncontradoException : Exception
{
    public RecursoNoEncontradoException() { }

    public RecursoNoEncontradoException(string message) : base(message) { }

    public RecursoNoEncontradoException(string message, Exception innerException) : base(message, innerException) { }
}
