using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using NFIT.Application.Abstracts.Repositories;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity, new()
{
    private NFITDbContext _context {  get;} 
    private readonly DbSet<T> Table;
    public Repository(NFITDbContext context)
    {
        _context = context;
        Table = _context.Set<T>();
    }
    public async Task AddAsync(T entity)
    {
        await Table.AddAsync(entity);
    }
    public void Update(T entity)
    {
        Table.Update(entity);
    }
    public void Delete(T entity)
    {
        Table.Remove(entity);
    }
    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await Table.FindAsync(id);
    }
    public IQueryable<T> GetByFiltered(Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, object>>[]? include = null,
        bool IsTracking = false)
    {
        IQueryable<T> query = Table;
        if (predicate is not null)
            query = query.Where(predicate);

        if (include is not null)
        {
            foreach (var includeExpression in include)
                query = query.Include(includeExpression);
        }

        if (IsTracking)
            query = query.AsNoTracking();

        return query;
    }

    public IQueryable<T> GetAll(bool IsTracking = false)
    {
        if (!IsTracking)
            return Table.AsNoTracking();
        return Table;
    }

    public IQueryable<T> GetAllFiltered(Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, object>>[]? include = null,
        Expression<Func<T, object>>? orderBy = null,
        bool IsOrderByAsc = true,
        bool IsTracking = false)
    {
        IQueryable<T> query = Table;

        if (predicate is not null)
            query = query.Where(predicate);

        if (include is not null)
            foreach (var includeExpression in include)
            {
                query = query.Include(includeExpression);
            }

        if (orderBy is not null)
        {
            if (IsOrderByAsc)
                query = query.OrderBy(orderBy);
            else
                query = query.OrderByDescending(orderBy);
        }

        if (IsTracking)
            query = query.AsNoTracking();

        return query;

    }



    public async Task SaveChangeAsync()
    {
        await _context.SaveChangesAsync();
    }



}
