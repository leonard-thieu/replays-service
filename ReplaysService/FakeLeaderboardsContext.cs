using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    internal sealed class FakeLeaderboardsContext : ILeaderboardsContext
    {
        public FakeLeaderboardsContext()
        {
            var ugcFileDetailsPath = Path.Combine("Data", "SteamWebApi", "UgcFileDetails");
            var ugcFileDetailsFiles = Directory.GetFiles(ugcFileDetailsPath, "*.json");
            var replays = (from f in ugcFileDetailsFiles
                           let n = Path.GetFileNameWithoutExtension(f)
                           select new Replay
                           {
                               ReplayId = long.Parse(n),
                           })
                           .ToList();
            Replays = new FakeDbSet<Replay>(replays);
        }

        public DbSet<Replay> Replays { get; }

        public DbSet<Leaderboard> Leaderboards => throw new NotImplementedException();
        public DbSet<Entry> Entries => throw new NotImplementedException();
        public DbSet<DailyLeaderboard> DailyLeaderboards => throw new NotImplementedException();
        public DbSet<DailyEntry> DailyEntries => throw new NotImplementedException();
        public DbSet<Player> Players => throw new NotImplementedException();
        public DbSet<Product> Products => throw new NotImplementedException();
        public DbSet<Mode> Modes => throw new NotImplementedException();
        public DbSet<Run> Runs => throw new NotImplementedException();
        public DbSet<Character> Characters => throw new NotImplementedException();

        public void Dispose() { }

        private sealed class FakeDbSet<TEntity> : DbSet<TEntity>, IDbAsyncEnumerable<TEntity>, IQueryable<TEntity>
            where TEntity : class
        {
            public FakeDbSet(IEnumerable<TEntity> data)
            {
                queryable = data.AsQueryable();
            }

            private readonly IQueryable<TEntity> queryable;

            IQueryProvider IQueryable.Provider => new TestDbAsyncQueryProvider<TEntity>(queryable.Provider);
            Expression IQueryable.Expression => queryable.Expression;
            Type IQueryable.ElementType => queryable.ElementType;
            IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator() => queryable.GetEnumerator();
            IDbAsyncEnumerator<TEntity> IDbAsyncEnumerable<TEntity>.GetAsyncEnumerator() => new TestDbAsyncEnumerator<TEntity>(queryable.GetEnumerator());
        }

        #region https://msdn.microsoft.com/library/dn314429.aspx

        private sealed class TestDbAsyncEnumerable<T> : EnumerableQuery<T>, IDbAsyncEnumerable<T>, IQueryable<T>
        {
            public TestDbAsyncEnumerable(Expression expression) : base(expression) { }

            public IDbAsyncEnumerator<T> GetAsyncEnumerator() => new TestDbAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
            IDbAsyncEnumerator IDbAsyncEnumerable.GetAsyncEnumerator() => GetAsyncEnumerator();
            IQueryProvider IQueryable.Provider => new TestDbAsyncQueryProvider<T>(this);
        }

        private sealed class TestDbAsyncEnumerator<T> : IDbAsyncEnumerator<T>
        {
            public TestDbAsyncEnumerator(IEnumerator<T> inner)
            {
                this.inner = inner;
            }

            private readonly IEnumerator<T> inner;

            public T Current => inner.Current;
            object IDbAsyncEnumerator.Current => Current;
            public Task<bool> MoveNextAsync(CancellationToken cancellationToken) => Task.FromResult(inner.MoveNext());
            public void Dispose() => inner.Dispose();
        }

        private sealed class TestDbAsyncQueryProvider<TEntity> : IDbAsyncQueryProvider
        {
            public TestDbAsyncQueryProvider(IQueryProvider inner)
            {
                this.inner = inner;
            }

            private readonly IQueryProvider inner;

            public IQueryable CreateQuery(Expression expression) => new TestDbAsyncEnumerable<TEntity>(expression);
            public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestDbAsyncEnumerable<TElement>(expression);
            public object Execute(Expression expression) => inner.Execute(expression);
            public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);
            public Task<object> ExecuteAsync(Expression expression, CancellationToken cancellationToken) => Task.FromResult(Execute(expression));
            public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken) => Task.FromResult(Execute<TResult>(expression));
        }

        #endregion
    }
}
