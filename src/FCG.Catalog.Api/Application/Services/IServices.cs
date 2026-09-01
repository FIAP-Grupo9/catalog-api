using FCG.BuildingBlocks.Common;
using FCG.Catalog.Api.Application.DTOs;

namespace FCG.Catalog.Api.Application.Services;

public interface IJogoService
{
    Task<Result<IEnumerable<JogoDTO>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<JogoDTO>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<JogoDTO>> CreateAsync(CreateJogoDTO dto, CancellationToken ct = default);
    Task<Result<bool>> UpdateAsync(Guid id, UpdateJogoDTO dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IPromocaoService
{
    Task<Result<IEnumerable<PromocaoDTO>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<PromocaoDTO>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<PromocaoDTO>> CreateAsync(CreatePromocaoDTO dto, CancellationToken ct = default);
    Task<Result<bool>> UpdateAsync(Guid id, UpdatePromocaoDTO dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IBibliotecaJogoService
{
    Task<Result<IEnumerable<BibliotecaJogoDTO>>> GetByUsuarioIdAsync(Guid usuarioId, CancellationToken ct = default);
    Task<Result<BibliotecaJogoDTO>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<OrderPendingDTO>> IniciarCompraAsync(Guid usuarioId, string userEmail, string userName, CreateBibliotecaJogoDTO dto, CancellationToken ct = default);
}

public interface IAvaliacaoService
{
    Task<Result<AvaliacaoDTO>> CriarAsync(Guid jogoId, Guid usuarioId, string usuarioNome, CreateAvaliacaoDTO dto, CancellationToken ct = default);
    Task<Result<IEnumerable<AvaliacaoDTO>>> ListarPorJogoAsync(Guid jogoId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<ResumoAvaliacoesDTO>> ResumoPorJogoAsync(Guid jogoId, CancellationToken ct = default);
}
