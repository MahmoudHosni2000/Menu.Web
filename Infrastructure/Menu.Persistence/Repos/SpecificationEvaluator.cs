using Menu.Domain.Contracts;
using Menu.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Menu.Persistence.Repos
{
    public static class SpecificationEvaluator<TKey, TEntity> where TEntity : BaseEntity<TKey>
    {
        public static IQueryable<TEntity> GetQuery(IQueryable<TEntity> inputQuery, 
            ISpecifications<TKey, TEntity> specifications)
        {
            var query = inputQuery;
            if (specifications.Criteria != null)
            {
                query = query.Where(specifications.Criteria);
            }

            foreach (var includeExpression in specifications.Includes)
            {
                query = query.Include(includeExpression);
            }

            if(specifications.OrderBy != null)
            {
                query = query.OrderBy(specifications.OrderBy);
            }
            else if(specifications.OrderByDescending != null)
            {
                query = query.OrderByDescending(specifications.OrderByDescending);
            }

            if(specifications.IsPaginationEnabled)
            {
                query = query.Skip(specifications.Skip).Take(specifications.Take);
            }

            return query;
        }
    }
}
