using FCG.Catalog.Api.Application;
using FCG.Catalog.Api.Application.Services;
using FCG.Catalog.Api.Domain.Entities;
using FCG.Catalog.Api.Domain.Repositories;
using FCG.Catalog.Api.Infrastructure.Search;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;

namespace FCG.Catalog.Api.Tests;

public class JogoServiceTests
{
    private readonly Mock<IJogoRepository> _repoMock = new();
    private readonly Mock<IDistributedCache> _cacheMock = new();
    private readonly Mock<IJogoSearchIndexer> _searchIndexerMock = new();
    private readonly JogoService _service;

    public JogoServiceTests()
    {
        _service = new JogoService(
            _repoMock.Object,
            _cacheMock.Object,
            _searchIndexerMock.Object,
            Mock.Of<ILogger<JogoService>>());
    }

    [Fact]
    public async Task GetByIdAsync_NaoEncontrado_RetornaFalha404()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Jogo?)null);

        var result = await _service.GetByIdAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(Messages.JogoNaoEncontrado, result.ErrorMessage);
    }
}
