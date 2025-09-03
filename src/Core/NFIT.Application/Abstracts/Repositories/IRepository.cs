using System.Linq.Expressions;
using NFIT.Domain.Entities;

namespace NFIT.Application.Abstracts.Repositories;

public interface IRepository<T> where T : BaseEntity, new()
{
    Task<T?> GetByIdAsync(Guid id);
    IQueryable<T> GetByFiltered(Expression<Func<T, bool>>? predicate = null,
                                Expression<Func<T, object>>[]? include = null,
                                bool IsTracking = false);
    IQueryable<T> GetAll(bool IsTracking = false);
    IQueryable<T> GetAllFiltered(Expression<Func<T, bool>>? predicate = null,
                         Expression<Func<T, object>>[]? include = null,
                         Expression<Func<T, object>>? orderBy = null,
                         bool IsOrderByAsc = true,
                         bool IsTracking = false);

    Task AddAsync(T entity);

    void Update(T entity);

    void Delete(T entity);

    Task SaveChangeAsync();
}
