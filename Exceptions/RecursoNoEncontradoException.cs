using System;

namespace complejoDeportivo.Exceptions
{
    public class RecursoNoEncontradoException : Exception
    {
        public RecursoNoEncontradoException(string message) : base(message)
        {
        }
    }
}
