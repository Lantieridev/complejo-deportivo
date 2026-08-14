using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ComplejoDeportivo.Api.Middleware;
using ComplejoDeportivo.Domain;

namespace ComplejoDeportivo.Tests.Api
{
    public class GlobalExceptionHandlerTests
    {
        [Fact]
        public async Task TryHandleAsync_Handles_ReservaSuperpuestaException()
        {
            var logger = new Mock<ILogger<GlobalExceptionHandler>>();
            var handler = new GlobalExceptionHandler(logger.Object);
            
            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            
            var ex = new ReservaSuperpuestaException("Test");
            var res = await handler.TryHandleAsync(ctx, ex, CancellationToken.None);
            
            Assert.True(res);
            Assert.Equal(StatusCodes.Status409Conflict, ctx.Response.StatusCode);
        }

        [Fact]
        public async Task TryHandleAsync_Handles_RecursoNoEncontradoException()
        {
            var logger = new Mock<ILogger<GlobalExceptionHandler>>();
            var handler = new GlobalExceptionHandler(logger.Object);
            
            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            
            var ex = new RecursoNoEncontradoException("Test");
            var res = await handler.TryHandleAsync(ctx, ex, CancellationToken.None);
            
            Assert.True(res);
            Assert.Equal(StatusCodes.Status404NotFound, ctx.Response.StatusCode);
        }

        [Fact]
        public async Task TryHandleAsync_Handles_OtherExceptions()
        {
            var logger = new Mock<ILogger<GlobalExceptionHandler>>();
            var handler = new GlobalExceptionHandler(logger.Object);
            
            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            
            var ex = new Exception("Test");
            var res = await handler.TryHandleAsync(ctx, ex, CancellationToken.None);
            
            Assert.True(res);
            Assert.Equal(StatusCodes.Status500InternalServerError, ctx.Response.StatusCode);
        }
    }
}
