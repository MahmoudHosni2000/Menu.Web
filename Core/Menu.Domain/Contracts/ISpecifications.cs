using Menu.Domain.Entities;
using System.Linq.Expressions;

namespace Menu.Domain.Contracts
{
    public interface ISpecifications<TKey, TEntity> where TEntity : BaseEntity<TKey>
    {
        public List<Expression<Func<TEntity, object>>> Includes { get; }
        public Expression<Func<TEntity, bool>>? Criteria { get; }
        Expression<Func<TEntity, object>>? OrderBy { get; }
        Expression<Func<TEntity, object>>? OrderByDescending { get; }
        int Skip { get; }
        int Take { get; }
        bool IsPaginationEnabled { get; }
    }
}
