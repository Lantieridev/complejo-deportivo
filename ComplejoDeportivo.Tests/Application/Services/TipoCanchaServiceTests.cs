using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ComplejoDeportivo.Application.DTOs;
using ComplejoDeportivo.Application.Repositories;
using ComplejoDeportivo.Application.Services.Implementations;
using ComplejoDeportivo.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace ComplejoDeportivo.Tests.Application.Services
{
    public class TipoCanchaServiceTests
    {
        private readonly Mock<ITipoCanchaRepository> _repoMock;
        private readonly TipoCanchaService _service;

        public TipoCanchaServiceTests()
        {
            _repoMock = new Mock<ITipoCanchaRepository>();
            _service = new TipoCanchaService(_repoMock.Object);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnMappedDtos()
        {
            var list = new List<TipoCancha> { new TipoCancha { TipoCanchaId = 1, Nombre = "Futbol 5" } };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(list);

            var result = await _service.GetAllAsync();

            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetByIdAsync_WhenFound_ShouldReturnDto()
        {
            var tc = new TipoCancha { TipoCanchaId = 1, Nombre = "Futbol 5" };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tc);

            var result = await _service.GetByIdAsync(1);

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetByIdAsync_WhenNotFound_ShouldThrowNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((TipoCancha?)null);

            Func<Task> act = async () => await _service.GetByIdAsync(1);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateAndReturnDto()
        {
            var dto = new CreateTipoCanchaDTO { Nombre = "Futbol 7" };
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<TipoCancha>())).ReturnsAsync((TipoCancha tc) => { tc.TipoCanchaId = 1; return tc; });

            var result = await _service.CreateAsync(dto);

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateAsync_WhenNotFound_ShouldThrowNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((TipoCancha?)null);

            Func<Task> act = async () => await _service.UpdateAsync(1, new TipoCanchaDTO { Nombre = "Test" });

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task UpdateAsync_WhenFound_ShouldUpdate()
        {
            var tc = new TipoCancha { TipoCanchaId = 1, Nombre = "Viejo" };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tc);

            var dto = new TipoCanchaDTO { Nombre = "Nuevo" };

            await _service.UpdateAsync(1, dto);

            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<TipoCancha>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenNotFound_ShouldThrowNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((TipoCancha?)null);

            Func<Task> act = async () => await _service.DeleteAsync(1);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task DeleteAsync_WhenFound_ShouldDelete()
        {
            var tc = new TipoCancha { TipoCanchaId = 1 };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tc);

            await _service.DeleteAsync(1);

            _repoMock.Verify(r => r.DeleteAsync(1), Times.Once);
        }
    }
}
