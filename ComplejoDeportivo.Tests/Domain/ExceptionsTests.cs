using System;
using ComplejoDeportivo.Domain;
using FluentAssertions;
using Xunit;

namespace ComplejoDeportivo.Tests.Domain
{
    public class ExceptionsTests
    {
        [Fact]
        public void RecursoNoEncontradoException_ParameterlessConstructor_CreatesInstance()
        {
            var ex = new RecursoNoEncontradoException();
            ex.Should().BeAssignableTo<Exception>();
        }

        [Fact]
        public void RecursoNoEncontradoException_MessageConstructor_SetsMessage()
        {
            var ex = new RecursoNoEncontradoException("no encontrado");
            ex.Message.Should().Be("no encontrado");
        }

        [Fact]
        public void RecursoNoEncontradoException_MessageAndInnerExceptionConstructor_SetsBoth()
        {
            var inner = new InvalidOperationException("inner");
            var ex = new RecursoNoEncontradoException("no encontrado", inner);
            ex.Message.Should().Be("no encontrado");
            ex.InnerException.Should().Be(inner);
        }

        [Fact]
        public void ReservaSuperpuestaException_ParameterlessConstructor_CreatesInstance()
        {
            var ex = new ReservaSuperpuestaException();
            ex.Should().BeAssignableTo<Exception>();
        }

        [Fact]
        public void ReservaSuperpuestaException_MessageConstructor_SetsMessage()
        {
            var ex = new ReservaSuperpuestaException("superpuesta");
            ex.Message.Should().Be("superpuesta");
        }

        [Fact]
        public void ReservaSuperpuestaException_MessageAndInnerExceptionConstructor_SetsBoth()
        {
            var inner = new InvalidOperationException("inner");
            var ex = new ReservaSuperpuestaException("superpuesta", inner);
            ex.Message.Should().Be("superpuesta");
            ex.InnerException.Should().Be(inner);
        }
    }
}
